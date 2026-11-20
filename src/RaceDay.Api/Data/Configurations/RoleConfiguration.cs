using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles", t =>
            t.HasCheckConstraint("CK_Roles_RoleName", "[RoleName] IN ('Organiser', 'Participant')"));

        b.HasKey(r => r.RoleId).HasName("PK_Roles");

        b.Property(r => r.RoleName).HasMaxLength(20).IsRequired();
        b.Property(r => r.Description).HasMaxLength(150).IsRequired();

        b.HasIndex(r => r.RoleName).IsUnique().HasDatabaseName("UQ_Roles_RoleName");
    }
}
