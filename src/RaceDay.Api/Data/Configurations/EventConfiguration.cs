using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.ToTable("Events", t =>
        {
            t.HasCheckConstraint("CK_Events_Status",
                "[EventStatus] IN ('Draft', 'Published', 'Cancelled', 'Completed')");
            t.HasCheckConstraint("CK_Events_Province",
                "[Province] IN ('Eastern Cape', 'Free State', 'Gauteng', 'KwaZulu-Natal', 'Limpopo', " +
                "'Mpumalanga', 'North West', 'Northern Cape', 'Western Cape')");
            t.HasCheckConstraint("CK_Events_RegWindow", "[RegistrationCloses] >= [RegistrationOpens]");
            t.HasCheckConstraint("CK_Events_RegBeforeRun", "[EventDate] >= [RegistrationCloses]");
            t.HasCheckConstraint("CK_Events_Latitude", "[Latitude] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_Events_Longitude", "[Longitude] BETWEEN -180 AND 180");
        });

        b.HasKey(e => e.EventId).HasName("PK_Events");

        b.Property(e => e.EventName).HasMaxLength(120).IsRequired();
        b.Property(e => e.Description).HasMaxLength(1000);
        b.Property(e => e.BannerImageUrl).HasMaxLength(500);
        b.Property(e => e.EventDate).HasColumnType("date");
        b.Property(e => e.StartTime).HasColumnType("time(0)");
        b.Property(e => e.Province).HasMaxLength(40).IsRequired();
        b.Property(e => e.City).HasMaxLength(80).IsRequired();
        b.Property(e => e.VenueName).HasMaxLength(120).IsRequired();
        b.Property(e => e.Latitude).HasPrecision(9, 6);
        b.Property(e => e.Longitude).HasPrecision(9, 6);
        b.Property(e => e.RegistrationOpens).HasColumnType("date");
        b.Property(e => e.RegistrationCloses).HasColumnType("date");
        b.Property(e => e.EventStatus)
            .HasMaxLength(20)
            .HasDefaultValue("Draft")
            .ValueGeneratedNever();
        b.Property(e => e.CreatedAt)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedNever();

        b.HasOne(e => e.Organiser)
            .WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Events_Organiser");

        b.HasIndex(e => new { e.EventName, e.EventDate }).IsUnique().HasDatabaseName("UQ_Events_NameDate");
        b.HasIndex(e => e.OrganiserId).HasDatabaseName("IX_Events_OrganiserId");
        b.HasIndex(e => e.EventDate)
            .HasDatabaseName("IX_Events_EventDate")
            .IncludeProperties(e => new { e.EventStatus, e.Province });
    }
}
