using System.Globalization;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

public class BonusService(IBonusRepository repository, TimeProvider clock)
{
    public async Task<EmployeeDto> FindEmployeeAsync(string department, int number, CancellationToken ct)
    {
        var employee = await RequireActiveEmployeeAsync(department, number, ct);
        return new(employee.DepartmentCode, employee.EmployeeNumber, employee.FullName,
            employee.HireDate, employee.MonthlySalary, employee.TaxRate);
    }

    public Task<PagedResponse<BonusDto>> ListAsync(BonusFilter filter, CancellationToken ct)
    {
        filter.DepartmentCode = filter.DepartmentCode?.Trim();
        filter.StateCode = filter.StateCode?.Trim();
        if (!string.IsNullOrEmpty(filter.DepartmentCode) && filter.DepartmentCode.Length > 5)
            throw new BusinessException("DEPARTMENT", "El código de área no es válido.");
        if (filter.EmployeeNumber.HasValue && filter.EmployeeNumber <= 0)
            throw new BusinessException("EMPLOYEE_NUMBER", "El número de empleado debe ser positivo.");
        if (!string.IsNullOrEmpty(filter.StateCode) && filter.StateCode != BonusStates.Registered
            && filter.StateCode != BonusStates.Paid && filter.StateCode != BonusStates.Cancelled)
            throw new BusinessException("BONUS_STATE", "Selecciona un estado de bono válido.");
        var period = string.IsNullOrWhiteSpace(filter.Period) ? (DateTime?)null : ParsePeriod(filter.Period);
        // Avoid overflow in Skip for pathological page indexes.
        if ((long)(filter.PageIndex - 1) * filter.PageSize > int.MaxValue)
            throw new BusinessException("PAGINATION", "La página solicitada no es válida.");
        return repository.ListAsync(filter, period, ct);
    }

    public async Task<BonusCreatedDto> CreateAsync(CreateBonusRequest request, string? userCode, CancellationToken ct)
    {
        var user = RequireUser(userCode);
        var period = ParsePeriod(request.Period);
        var now = clock.GetUtcNow();
        var limaToday = now.ToOffset(TimeSpan.FromHours(-5)).Date;
        if (period > new DateTime(limaToday.Year, limaToday.Month, 1))
            throw new BusinessException("FUTURE_PERIOD", "El periodo no puede ser posterior al mes actual (Lima).");
        var employee = await RequireActiveEmployeeAsync(request.DepartmentCode, request.EmployeeNumber, ct);
        var reason = request.ReasonCode?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 6 || !await repository.IsReasonActiveAsync(reason, ct))
            throw new BusinessException("BONUS_REASON", "Selecciona un motivo activo.");
        if (await repository.ActiveBonusExistsAsync(employee.DepartmentCode, employee.EmployeeNumber, period, ct))
            throw new BusinessException("BONUS_DUPLICATE", "El empleado ya tiene un bono activo en ese periodo.");
        var bonus = Bonus.Create(employee, period, request.BonusTypeId, request.Value, reason, user, now.UtcDateTime);
        repository.Add(bonus);
        await repository.SaveAsync(ct);
        return new(bonus.BonusId, bonus.GrossAmount, bonus.TaxAmount, bonus.NetAmount);
    }

    public async Task CancelAsync(int id, string? userCode, CancellationToken ct)
    {
        var user = RequireUser(userCode);
        var bonus = await RequireBonusAsync(id, ct);
        bonus.Cancel(user, clock.GetUtcNow().UtcDateTime);
        await repository.SaveAsync(ct);
    }

    public async Task DeleteAsync(int id, string? userCode, CancellationToken ct)
    {
        var user = RequireUser(userCode);
        var bonus = await RequireBonusAsync(id, ct);
        bonus.Delete(user, clock.GetUtcNow().UtcDateTime);
        await repository.SaveAsync(ct);
    }

    private async Task<Employee> RequireActiveEmployeeAsync(string? department, int number, CancellationToken ct)
    {
        department = department?.Trim();
        if (string.IsNullOrEmpty(department) || department.Length > 5 || number <= 0)
            throw new BusinessException("EMPLOYEE_KEY", "Ingresa un área y un número de empleado válidos.");
        var employee = await repository.FindEmployeeAsync(department, number, ct)
            ?? throw new BusinessException("EMPLOYEE_NOT_FOUND", "No se encontró al empleado en el área indicada.");
        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_INACTIVE", "El empleado está cesado. Solo se aceptan empleados activos.");
        return employee;
    }

    private async Task<Bonus> RequireBonusAsync(int id, CancellationToken ct) =>
        await repository.FindAsync(id, ct)
        ?? throw new BusinessException("BONUS_NOT_FOUND", "El bono no existe o fue eliminado.");

    private static string RequireUser(string? user)
    {
        user = user?.Trim();
        if (string.IsNullOrEmpty(user) || user.Length > 100)
            throw new BusinessException("USER_CODE", "El header UserCode es obligatorio y admite hasta 100 caracteres.");
        return user;
    }

    private static DateTime ParsePeriod(string? period)
    {
        if (period is null || period.Length != 7 || !DateTime.TryParseExact(period + "-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            throw new BusinessException("PERIOD_FORMAT", "El periodo debe tener el formato YYYY-MM.");
        return result;
    }
}
