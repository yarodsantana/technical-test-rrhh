using BonusTest.Application.Common;

namespace BonusTest.Application.Bonuses;

public class BonusFilter : PaginationRequest
{
    public string? DepartmentCode { get; set; }
    public int? EmployeeNumber { get; set; }
    public string? Period { get; set; }
    public string? StateCode { get; set; }
}

public record CreateBonusRequest(string DepartmentCode, int EmployeeNumber, string Period,
    int BonusTypeId, decimal Value, string ReasonCode);
public record EmployeeDto(string DepartmentCode, int EmployeeNumber, string FullName,
    DateTime HireDate, decimal MonthlySalary, decimal TaxRate);
public record BonusCreatedDto(int BonusId, decimal GrossAmount, decimal TaxAmount, decimal NetAmount);
public record BonusDto(int BonusId, string StateCode, string StateDescription, string Period,
    string DepartmentCode, string DepartmentName, int EmployeeNumber, string FullName,
    int BonusTypeId, string BonusTypeDescription, decimal Value, string ReasonCode,
    string ReasonDescription, decimal GrossAmount, decimal TaxAmount, decimal NetAmount);
