using BonusTest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("Bonus").HasKey(x => x.BonusId);
        builder.Property(x => x.DepartmentCode).HasColumnType("char(5)");
        builder.Property(x => x.ReasonCode).HasColumnType("char(6)");
        builder.Property(x => x.StateCode).HasColumnType("char(4)");
        builder.Property(x => x.Period).HasColumnType("date");
        builder.Property(x => x.Value).HasPrecision(20, 4);
        builder.Property(x => x.SalarySnapshot).HasPrecision(16, 2);
        builder.Property(x => x.TaxRateSnapshot).HasPrecision(5, 2);
        builder.Property(x => x.GrossAmount).HasPrecision(16, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(16, 2);
        builder.Property(x => x.NetAmount).HasPrecision(16, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => new { x.DepartmentCode, x.EmployeeNumber })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BonusType>().WithMany().HasForeignKey(x => x.BonusTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BonusReason>().WithMany().HasForeignKey(x => x.ReasonCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<State>().WithMany().HasForeignKey(x => x.StateCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DepartmentCode, x.EmployeeNumber, x.Period })
            .IsUnique().HasFilter("[IsDeleted] = 0 AND [StateCode] <> '0203'")
            .HasDatabaseName("UX_Bonus_ActiveEmployeePeriod");
    }
}
