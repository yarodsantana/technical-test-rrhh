using BonusTest.Domain.Bonuses;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Domain.Entities;

public class Bonus
{
    public int BonusId { get; private set; }
    public string DepartmentCode { get; private set; } = null!;
    public int EmployeeNumber { get; private set; }
    public DateTime Period { get; private set; }
    public int BonusTypeId { get; private set; }
    public decimal Value { get; private set; }
    public string ReasonCode { get; private set; } = null!;
    public decimal SalarySnapshot { get; private set; }
    public decimal TaxRateSnapshot { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public string StateCode { get; private set; } = BonusStates.Registered;
    public bool IsDeleted { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public string ModifiedBy { get; private set; } = null!;
    public DateTime ModifiedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Bonus() { }

    public static Bonus Create(Employee employee, DateTime period, int type, decimal value,
        string reason, string user, DateTime utcNow)
    {
        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_INACTIVE", "El empleado debe estar activo para registrar un bono.");
        var amounts = BonusCalculator.Calculate(type, value, employee.MonthlySalary, employee.TaxRate);
        return new Bonus
        {
            DepartmentCode = employee.DepartmentCode, EmployeeNumber = employee.EmployeeNumber,
            Period = period, BonusTypeId = type, Value = value, ReasonCode = reason,
            SalarySnapshot = employee.MonthlySalary, TaxRateSnapshot = employee.TaxRate,
            GrossAmount = amounts.GrossAmount, TaxAmount = amounts.TaxAmount, NetAmount = amounts.NetAmount,
            CreatedBy = user, ModifiedBy = user, CreatedAt = utcNow, ModifiedAt = utcNow
        };
    }

    public void Cancel(string user, DateTime utcNow)
    {
        EnsureRegistered();
        StateCode = BonusStates.Cancelled;
        Touch(user, utcNow);
    }

    public void Delete(string user, DateTime utcNow)
    {
        EnsureRegistered();
        IsDeleted = true;
        Touch(user, utcNow);
    }

    private void EnsureRegistered()
    {
        if (IsDeleted || StateCode != BonusStates.Registered)
            throw new BusinessException("BONUS_STATE", "Solo se pueden operar bonos en estado Registrado y no eliminados.");
    }

    private void Touch(string user, DateTime utcNow)
    {
        ModifiedBy = user;
        ModifiedAt = utcNow;
    }
}
