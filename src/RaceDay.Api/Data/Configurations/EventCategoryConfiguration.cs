using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EventCategoryConfiguration : IEntityTypeConfiguration<EventCategory>
{
    public void Configure(EntityTypeBuilder<EventCategory> b)
    {
        b.ToTable("EventCategories", t =>
        {
            t.HasCheckConstraint("CK_EventCategories_Disc", "[Discipline] IN ('Run', 'Walk', 'Cycle')");
            t.HasCheckConstraint("CK_EventCategories_Distance", "[DistanceKm] > 0");
            t.HasCheckConstraint("CK_EventCategories_Fee", "[EntryFee] >= 0");
            t.HasCheckConstraint("CK_EventCategories_Max", "[MaxParticipants] > 0");
            t.HasCheckConstraint("CK_EventCategories_MinAge", "[MinimumAge] BETWEEN 0 AND 100");
        });

        b.HasKey(c => c.CategoryId).HasName("PK_EventCategories");

        b.Property(c => c.CategoryName).HasMaxLength(80).IsRequired();
        b.Property(c => c.Discipline).HasMaxLength(10).IsRequired();
        b.Property(c => c.DistanceKm).HasPrecision(6, 2);
        b.Property(c => c.EntryFee).HasPrecision(10, 2);
        b.Property(c => c.StartTime).HasColumnType("time(0)");
        b.Property(c => c.MinimumAge)
            .HasDefaultValue(12)
            .ValueGeneratedNever();

        b.HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_EventCategories_Events");

        b.HasIndex(c => new { c.EventId, c.CategoryName }).IsUnique().HasDatabaseName("UQ_EventCategories_Name");
        b.HasIndex(c => c.EventId).HasDatabaseName("IX_EventCategories_EventId");
    }
}
