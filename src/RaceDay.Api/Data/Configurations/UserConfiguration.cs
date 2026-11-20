using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", t =>
        {
            t.HasCheckConstraint("CK_Users_Email", "[Email] LIKE '%_@_%._%'");
            t.HasCheckConstraint("CK_Users_Gender", "[Gender] IN ('M', 'F', 'O')");
            t.HasCheckConstraint("CK_Users_DOB", "[DateOfBirth] < CAST(SYSUTCDATETIME() AS DATE)");
        });

        b.HasKey(u => u.UserId).HasName("PK_Users");

        b.Property(u => u.FirstName).HasMaxLength(60).IsRequired();
        b.Property(u => u.LastName).HasMaxLength(60).IsRequired();
        b.Property(u => u.Email).HasMaxLength(150).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        b.Property(u => u.PhoneNumber).HasMaxLength(20);
        b.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
        b.Property(u => u.DateOfBirth).HasColumnType("date");
        b.Property(u => u.Gender).HasMaxLength(1).IsFixedLength().IsRequired();

        b.Property(u => u.CreatedAt)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedNever();
        b.Property(u => u.IsActive)
            .HasDefaultValueSql("1")
            .ValueGeneratedNever();

        b.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Users_Roles");

        b.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        b.HasIndex(u => u.RoleId).HasDatabaseName("IX_Users_RoleId");
    }
}
