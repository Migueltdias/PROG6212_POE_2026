using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> b)
    {
        b.ToTable("Results", t =>
        {
            t.HasCheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished', 'DNF', 'DNS', 'DQ')");
            t.HasCheckConstraint("CK_Results_ChipTime", "[ChipTimeSeconds] IS NULL OR [ChipTimeSeconds] > 0");
            t.HasCheckConstraint("CK_Results_Positions",
                "([OverallPosition] IS NULL OR [OverallPosition] > 0) " +
                "AND ([GenderPosition] IS NULL OR [GenderPosition] > 0) " +
                "AND ([AgeGroupPosition] IS NULL OR [AgeGroupPosition] > 0)");
            t.HasCheckConstraint("CK_Results_Consistency",
                "([ResultStatus] = 'Finished' AND [FinishTime] IS NOT NULL AND [ChipTimeSeconds] IS NOT NULL " +
                "AND [OverallPosition] IS NOT NULL) " +
                "OR ([ResultStatus] <> 'Finished' AND [FinishTime] IS NULL AND [ChipTimeSeconds] IS NULL " +
                "AND [OverallPosition] IS NULL)");
        });

        b.HasKey(r => r.ResultId).HasName("PK_Results");

        b.Property(r => r.StartTime).HasColumnType("time(0)");
        b.Property(r => r.FinishTime).HasColumnType("time(0)");
        b.Property(r => r.ResultStatus).HasMaxLength(10).IsRequired();
        b.Property(r => r.CapturedAt)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedNever();

        b.HasOne(r => r.Enrolment)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Results_Enrolments");

        b.HasOne(r => r.CapturedBy)
            .WithMany(u => u.CapturedResults)
            .HasForeignKey(r => r.CapturedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Results_CapturedBy");

        b.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
        b.HasIndex(r => r.CapturedByUserId).HasDatabaseName("IX_Results_CapturedByUserId");
    }
}
