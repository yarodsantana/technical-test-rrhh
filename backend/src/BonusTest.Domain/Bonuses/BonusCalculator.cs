using BonusTest.Domain.Constants;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Domain.Bonuses;

public record BonusAmounts(decimal GrossAmount, decimal TaxAmount, decimal NetAmount);

public static class BonusCalculator
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static BonusAmounts Calculate(int bonusTypeId, decimal value, decimal salary, decimal taxRate)
    {
        if (salary <= 0 || taxRate < 0 || taxRate > 100)
            throw new BusinessException("EMPLOYEE_FINANCIAL_DATA", "El sueldo o la tasa de retención del empleado no son válidos.");
        if (bonusTypeId != BonusTypes.FixedAmount && bonusTypeId != BonusTypes.SalaryPercentage)
            throw new BusinessException("BONUS_TYPE", "Selecciona un tipo de bono válido.");
        if (value <= 0 || (bonusTypeId == BonusTypes.FixedAmount && value > salary)
            || (bonusTypeId == BonusTypes.SalaryPercentage && value > 100))
            throw new BusinessException("BONUS_VALUE", bonusTypeId == BonusTypes.FixedAmount
                ? "El monto debe ser mayor que cero y no superar el sueldo mensual."
                : "El porcentaje debe ser mayor que cero y no superar 100.");
        if (decimal.Round(value, 4) != value)
            throw new BusinessException("BONUS_PRECISION", "El valor admite hasta cuatro decimales.");

        var gross = Round(bonusTypeId == BonusTypes.FixedAmount ? value : salary * value / 100m);
        var tax = Round(gross * taxRate / 100m);
        return new(gross, tax, Round(gross - tax));
    }
}
