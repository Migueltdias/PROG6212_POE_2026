using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EnrolmentConfiguration : IEntityTypeConfiguration<Enrolment>
{
    public void Configure(EntityTypeBuilder<Enrolment> b)
    {
        b.ToTable("Enrolments", t =>
        {
            t.HasCheckConstraint("CK_Enrolments_Status",
                "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Cancelled', 'Withdrawn')");
            t.HasCheckConstraint("CK_Enrolments_AmountPaid", "[AmountPaid] >= 0");
            t.HasCheckConstraint("CK_Enrolments_TShirtSize",
                "[TShirtSize] IS NULL OR [TShirtSize] IN ('XS', 'S', 'M', 'L', 'XL', 'XXL')");
        });

        b.HasKey(e => e.EnrolmentId).HasName("PK_Enrolments");

        b.Property(e => e.RaceNumber).HasMaxLength(10).IsRequired();
        b.Property(e => e.EnrolmentDate)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedNever();
        b.Property(e => e.EnrolmentStatus)
            .HasMaxLength(20)
            .HasDefaultValue("Pending")
            .ValueGeneratedNever();
        b.Property(e => e.AmountPaid)
            .HasPrecision(10, 2)
            .HasDefaultValueSql("0")
            .ValueGeneratedNever();
        b.Property(e => e.PaymentReference).HasMaxLength(50);
        b.Property(e => e.TShirtSize).HasMaxLength(5);
        b.Property(e => e.EmergencyContactName).HasMaxLength(100).IsRequired();
        b.Property(e => e.EmergencyContactPhone).HasMaxLength(20).IsRequired();

        b.HasOne(e => e.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Enrolments_Users");

        b.HasOne(e => e.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Enrolments_Categories");

        b.HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique().HasDatabaseName("UQ_Enrolments_OnePerCat");
        b.HasIndex(e => new { e.CategoryId, e.RaceNumber }).IsUnique().HasDatabaseName("UQ_Enrolments_RaceNumber");
        b.HasIndex(e => e.ParticipantId).HasDatabaseName("IX_Enrolments_ParticipantId");
        b.HasIndex(e => e.CategoryId)
            .HasDatabaseName("IX_Enrolments_CategoryId")
            .IncludeProperties(e => e.EnrolmentStatus);
    }
}
