using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    Task<Employee?> FindEmployeeAsync(string department, int number, CancellationToken ct);
    Task<bool> IsReasonActiveAsync(string reason, CancellationToken ct);
    Task<bool> ActiveBonusExistsAsync(string department, int number, DateTime period, CancellationToken ct);
    Task<Bonus?> FindAsync(int id, CancellationToken ct);
    Task<PagedResponse<BonusDto>> ListAsync(BonusFilter filter, DateTime? period, CancellationToken ct);
    void Add(Bonus bonus);
    Task SaveAsync(CancellationToken ct);
}
