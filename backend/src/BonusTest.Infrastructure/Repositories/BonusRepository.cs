using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;
using BonusTest.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BonusTest.Infrastructure.Repositories;

public class BonusRepository(AppDbContext context) : IBonusRepository
{
    public Task<Employee?> FindEmployeeAsync(string department, int number, CancellationToken ct) =>
        context.Employees.AsNoTracking().SingleOrDefaultAsync(
            x => x.DepartmentCode == department && x.EmployeeNumber == number, ct);

    public Task<bool> IsReasonActiveAsync(string reason, CancellationToken ct) =>
        context.BonusReasons.AnyAsync(x => x.ReasonCode == reason && x.State, ct);

    public Task<bool> ActiveBonusExistsAsync(string department, int number, DateTime period, CancellationToken ct) =>
        context.Bonuses.AnyAsync(x => x.DepartmentCode == department && x.EmployeeNumber == number
            && x.Period == period && x.StateCode != BonusStates.Cancelled, ct);

    public Task<Bonus?> FindAsync(int id, CancellationToken ct) =>
        context.Bonuses.SingleOrDefaultAsync(x => x.BonusId == id, ct);

    public void Add(Bonus bonus) => context.Bonuses.Add(bonus);

    public async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("BONUS_CONCURRENT_CHANGE", "El bono cambió durante la operación. Actualiza el listado.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessException("BONUS_DUPLICATE", "El empleado ya tiene un bono activo en ese periodo.");
        }
    }

    public async Task<PagedResponse<BonusDto>> ListAsync(BonusFilter filter, DateTime? period, CancellationToken ct)
    {
        var bonuses = context.Bonuses.AsNoTracking();
        if (!string.IsNullOrEmpty(filter.DepartmentCode))
            bonuses = bonuses.Where(x => x.DepartmentCode == filter.DepartmentCode);
        if (filter.EmployeeNumber.HasValue)
            bonuses = bonuses.Where(x => x.EmployeeNumber == filter.EmployeeNumber.Value);
        if (period.HasValue)
            bonuses = bonuses.Where(x => x.Period == period.Value);
        if (!string.IsNullOrEmpty(filter.StateCode))
            bonuses = bonuses.Where(x => x.StateCode == filter.StateCode);

        var total = await bonuses.CountAsync(ct);
        var query = from bonus in bonuses
                    join employee in context.Employees on new { bonus.DepartmentCode, bonus.EmployeeNumber }
                        equals new { employee.DepartmentCode, employee.EmployeeNumber }
                    join department in context.Departments on bonus.DepartmentCode equals department.DepartmentCode
                    join state in context.States on bonus.StateCode equals state.StateCode
                    join type in context.BonusTypes on bonus.BonusTypeId equals type.BonusTypeId
                    join reason in context.BonusReasons on bonus.ReasonCode equals reason.ReasonCode
                    orderby bonus.Period descending, bonus.BonusId descending
                    select new { bonus, employee.FullName, department.DepartmentName, state.StateDescription,
                        type.BonusTypeDescription, reason.ReasonDescription };

        // Skip/Take execute in SQL. Only the requested page is materialized.
        var page = await query.Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        var data = page.Select(x => new BonusDto(x.bonus.BonusId, x.bonus.StateCode, x.StateDescription,
            x.bonus.Period.ToString("yyyy-MM"), x.bonus.DepartmentCode, x.DepartmentName,
            x.bonus.EmployeeNumber, x.FullName, x.bonus.BonusTypeId, x.BonusTypeDescription,
            x.bonus.Value, x.bonus.ReasonCode, x.ReasonDescription,
            x.bonus.GrossAmount, x.bonus.TaxAmount, x.bonus.NetAmount)).ToList();
        return PagedResponse<BonusDto>.Create(data, filter.PageIndex, filter.PageSize, total);
    }
}
