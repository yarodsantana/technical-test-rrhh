using BonusTest.Domain.Bonuses;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Tests;

public class BonusCalculatorTests
{
    [Fact]
    public void OfficialExampleCalculatesGrossTaxAndNet()
    {
        Assert.Equal(new BonusAmounts(500m, 40m, 460m), BonusCalculator.Calculate(2, 10m, 5000m, 8m));
    }

    [Theory]
    [InlineData("0.125", "0.13")]
    [InlineData("1.005", "1.01")]
    [InlineData("2.675", "2.68")]
    [InlineData("0.1249", "0.12")]
    public void FixedAmountUsesHalfUpRounding(string input, string expected)
    {
        var result = BonusCalculator.Calculate(1, Parse(input), 5000m, 0m);
        Assert.Equal(Parse(expected), result.GrossAmount);
        Assert.Equal(result.GrossAmount, result.NetAmount);
    }

    [Fact]
    public void TaxIsRoundedBeforeNetIsCalculated()
    {
        var result = BonusCalculator.Calculate(1, 1m, 5000m, 12.5m);
        Assert.Equal(new BonusAmounts(1m, 0.13m, 0.87m), result);
    }

    [Fact]
    public void PercentageGrossUsesHalfUpRounding()
    {
        var result = BonusCalculator.Calculate(2, 12.5m, 1m, 0m);
        Assert.Equal(0.13m, result.GrossAmount);
    }

    [Theory]
    [InlineData(1, "0")]
    [InlineData(1, "-1")]
    [InlineData(1, "5000.0001")]
    [InlineData(2, "0")]
    [InlineData(2, "-1")]
    [InlineData(2, "100.0001")]
    [InlineData(3, "10")]
    [InlineData(1, "1.00001")]
    public void RejectsInvalidValues(int type, string value) =>
        Assert.Throws<BusinessException>(() => BonusCalculator.Calculate(type, Parse(value), 5000m, 8m));

    [Theory]
    [InlineData(1, "5000")]
    [InlineData(2, "100")]
    public void AcceptsUpperBoundary(int type, string value) =>
        Assert.Equal(new BonusAmounts(5000m, 400m, 4600m), BonusCalculator.Calculate(type, Parse(value), 5000m, 8m));

    [Fact]
    public void SmallPositiveAmountMayRoundToZero() =>
        Assert.Equal(new BonusAmounts(0m, 0m, 0m), BonusCalculator.Calculate(1, 0.0001m, 5000m, 8m));

    [Fact]
    public void FullRetentionLeavesZeroNet() =>
        Assert.Equal(new BonusAmounts(10m, 10m, 0m), BonusCalculator.Calculate(1, 10m, 5000m, 100m));

    [Fact]
    public void FinancialSnapshotSurvivesEmployeeChanges()
    {
        var employee = Employee();
        var bonus = Create(employee);
        employee.MonthlySalary = 9000m;
        employee.TaxRate = 10m;
        Assert.Equal(5000m, bonus.SalarySnapshot);
        Assert.Equal(8m, bonus.TaxRateSnapshot);
        Assert.Equal(460m, bonus.NetAmount);
    }

    [Fact]
    public void CancellationPreservesCreationAuditAndPreventsDeletion()
    {
        var bonus = Create(Employee());
        var createdAt = bonus.CreatedAt;
        bonus.Cancel("reviewer", createdAt.AddMinutes(1));
        Assert.Equal(BonusStates.Cancelled, bonus.StateCode);
        Assert.Equal("candidate", bonus.CreatedBy);
        Assert.Equal(createdAt, bonus.CreatedAt);
        Assert.Equal("reviewer", bonus.ModifiedBy);
        Assert.Equal(createdAt.AddMinutes(1), bonus.ModifiedAt);
        Assert.Throws<BusinessException>(() => bonus.Delete("reviewer", createdAt.AddMinutes(2)));
        Assert.Throws<BusinessException>(() => bonus.Cancel("reviewer", createdAt.AddMinutes(2)));
    }

    [Fact]
    public void LogicalDeletionPreservesRecordAndPreventsFurtherOperations()
    {
        var bonus = Create(Employee());
        bonus.Delete("reviewer", bonus.CreatedAt.AddMinutes(1));
        Assert.True(bonus.IsDeleted);
        Assert.Equal(460m, bonus.NetAmount);
        Assert.Throws<BusinessException>(() => bonus.Cancel("reviewer", bonus.CreatedAt.AddMinutes(2)));
    }

    [Fact]
    public void TerminatedEmployeeCannotReceiveBonus()
    {
        var employee = Employee();
        employee.EmployeeStateCode = EmployeeStates.Terminated;
        Assert.Throws<BusinessException>(() => Create(employee));
    }

    private static decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    internal static Employee Employee() => new() { DepartmentCode = "TECH1", EmployeeNumber = 101,
        FullName = "Andrea Torres Vega", MonthlySalary = 5000m, TaxRate = 8m, EmployeeStateCode = EmployeeStates.Active };
    private static Bonus Create(Employee employee) => Bonus.Create(employee, new(2026, 10, 1), 2, 10m, "000001", "candidate", new(2026, 10, 30, 18, 0, 0));
}

