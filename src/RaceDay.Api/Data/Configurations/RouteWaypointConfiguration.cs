using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class RouteWaypointConfiguration : IEntityTypeConfiguration<RouteWaypoint>
{
    public void Configure(EntityTypeBuilder<RouteWaypoint> b)
    {
        b.ToTable("RouteWaypoints", t =>
        {
            t.HasCheckConstraint("CK_RouteWaypoints_Type",
                "[WaypointType] IN ('Start', 'WaterPoint', 'Medical', 'Timing', 'Finish')");
            t.HasCheckConstraint("CK_RouteWaypoints_Sequence", "[SequenceNumber] > 0");
            t.HasCheckConstraint("CK_RouteWaypoints_Latitude", "[Latitude] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_RouteWaypoints_Longitude", "[Longitude] BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_RouteWaypoints_Distance", "[DistanceFromStartKm] >= 0");
        });

        b.HasKey(w => w.WaypointId).HasName("PK_RouteWaypoints");

        b.Property(w => w.WaypointName).HasMaxLength(80).IsRequired();
        b.Property(w => w.WaypointType).HasMaxLength(20).IsRequired();
        b.Property(w => w.Latitude).HasPrecision(9, 6);
        b.Property(w => w.Longitude).HasPrecision(9, 6);
        b.Property(w => w.DistanceFromStartKm).HasPrecision(6, 2);

        b.HasOne(w => w.Category)
            .WithMany(c => c.Waypoints)
            .HasForeignKey(w => w.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RouteWaypoints_Categories");

        b.HasIndex(w => new { w.CategoryId, w.SequenceNumber }).IsUnique().HasDatabaseName("UQ_RouteWaypoints_Sequence");
        b.HasIndex(w => w.CategoryId).HasDatabaseName("IX_RouteWaypoints_CategoryId");
    }
}
