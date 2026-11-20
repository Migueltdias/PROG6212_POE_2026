using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class WeatherSnapshotConfiguration : IEntityTypeConfiguration<WeatherSnapshot>
{
    public void Configure(EntityTypeBuilder<WeatherSnapshot> b)
    {
        b.ToTable("WeatherSnapshots", t =>
        {
            t.HasCheckConstraint("CK_WeatherSnapshots_Temp", "[TemperatureC] BETWEEN -30 AND 60");
            t.HasCheckConstraint("CK_WeatherSnapshots_Humidity", "[HumidityPercent] IS NULL OR [HumidityPercent] <= 100");
            t.HasCheckConstraint("CK_WeatherSnapshots_Rain", "[ChanceOfRainPct] IS NULL OR [ChanceOfRainPct] <= 100");
            t.HasCheckConstraint("CK_WeatherSnapshots_Wind", "[WindSpeedKph] IS NULL OR [WindSpeedKph] >= 0");
        });

        b.HasKey(w => w.SnapshotId).HasName("PK_WeatherSnapshots");

        b.Property(w => w.CapturedAt)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedNever();
        b.Property(w => w.TemperatureC).HasPrecision(4, 1);
        b.Property(w => w.FeelsLikeC).HasPrecision(4, 1);
        b.Property(w => w.Conditions).HasMaxLength(60).IsRequired();
        b.Property(w => w.WindSpeedKph).HasPrecision(5, 1);
        b.Property(w => w.Source)
            .HasMaxLength(60)
            .HasDefaultValue("OpenWeatherMap")
            .ValueGeneratedNever();

        b.HasOne(w => w.Event)
            .WithMany(e => e.WeatherSnapshots)
            .HasForeignKey(w => w.EventId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_WeatherSnapshots_Events");

        b.HasIndex(w => new { w.EventId, w.CapturedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_WeatherSnapshots_EventId");
    }
}
