using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Tests;

public class BonusServiceTests
{
    private readonly FakeRepository repository = new();
    // It is November in UTC, but still October in Lima.
    private readonly TimeProvider clock = new FixedClock(new(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private BonusService Service => new(repository, clock);
    private static CreateBonusRequest Request(string period = "2026-10") => new("TECH1", 101, period, 2, 10m, "000001");

    [Fact]
    public async Task ValidRequestStoresAuditAndCalculatedAmounts()
    {
        var result = await Service.CreateAsync(Request(), " candidate ", default);
        Assert.Equal(460m, result.NetAmount);
        Assert.Equal("candidate", repository.Added!.CreatedBy);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, repository.Added.CreatedAt);
        Assert.True(repository.Saved);
    }

    [Theory]
    [InlineData("2026-11", "FUTURE_PERIOD")]
    [InlineData("2027-01", "FUTURE_PERIOD")]
    [InlineData("2026-13", "PERIOD_FORMAT")]
    [InlineData("2026-00", "PERIOD_FORMAT")]
    [InlineData("2026-1", "PERIOD_FORMAT")]
    [InlineData("2026-10-01", "PERIOD_FORMAT")]
    [InlineData("", "PERIOD_FORMAT")]
    public async Task RejectsInvalidOrFuturePeriod(string period, string code)
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Service.CreateAsync(Request(period), "candidate", default));
        Assert.Equal(code, ex.Code);
        Assert.Null(repository.Added);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RequiresAuditUser(string? user)
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Service.CreateAsync(Request(), user, default));
        Assert.Equal("USER_CODE", ex.Code);
    }

    [Fact]
    public async Task RejectsInactiveReason()
    {
        repository.ActiveReason = false;
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Service.CreateAsync(Request(), "candidate", default));
        Assert.Equal("BONUS_REASON", ex.Code);
    }

    [Fact]
    public async Task RejectsDuplicateActiveBonus()
    {
        repository.Duplicate = true;
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Service.CreateAsync(Request(), "candidate", default));
        Assert.Equal("BONUS_DUPLICATE", ex.Code);
        Assert.False(repository.Saved);
    }

    [Fact]
    public async Task RejectsTerminatedEmployeeOnLookupAndCreate()
    {
        repository.Employee!.EmployeeStateCode = "0102";
        Assert.Equal("EMPLOYEE_INACTIVE", (await Assert.ThrowsAsync<BusinessException>(() => Service.FindEmployeeAsync("VENTA", 202, default))).Code);
        Assert.Equal("EMPLOYEE_INACTIVE", (await Assert.ThrowsAsync<BusinessException>(() => Service.CreateAsync(Request(), "candidate", default))).Code);
    }

    [Fact]
    public async Task UnknownEmployeeReturnsBusinessError()
    {
        repository.Employee = null;
        Assert.Equal("EMPLOYEE_NOT_FOUND", (await Assert.ThrowsAsync<BusinessException>(() => Service.FindEmployeeAsync("TECH1", 999, default))).Code);
    }

    [Fact]
    public async Task MissingBonusCannotBeCancelledOrDeleted()
    {
        Assert.Equal("BONUS_NOT_FOUND", (await Assert.ThrowsAsync<BusinessException>(() => Service.CancelAsync(999, "candidate", default))).Code);
        Assert.Equal("BONUS_NOT_FOUND", (await Assert.ThrowsAsync<BusinessException>(() => Service.DeleteAsync(999, "candidate", default))).Code);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeRepository : IBonusRepository
    {
        public Employee? Employee { get; set; } = BonusCalculatorTests.Employee();
        public bool ActiveReason { get; set; } = true;
        public bool Duplicate { get; set; }
        public Bonus? Added { get; private set; }
        public bool Saved { get; private set; }
        public void Add(Bonus bonus) => Added = bonus;
        public Task SaveAsync(CancellationToken ct) { Saved = true; return Task.CompletedTask; }
        public Task<Employee?> FindEmployeeAsync(string department, int number, CancellationToken ct) => Task.FromResult(Employee);
        public Task<bool> IsReasonActiveAsync(string reason, CancellationToken ct) => Task.FromResult(ActiveReason);
        public Task<bool> ActiveBonusExistsAsync(string department, int number, DateTime period, CancellationToken ct) => Task.FromResult(Duplicate);
        public Task<Bonus?> FindAsync(int id, CancellationToken ct) => Task.FromResult<Bonus?>(null);
        public Task<PagedResponse<BonusDto>> ListAsync(BonusFilter filter, DateTime? period, CancellationToken ct) =>
            Task.FromResult(PagedResponse<BonusDto>.Create([], filter.PageIndex, filter.PageSize, 0));
    }
}
