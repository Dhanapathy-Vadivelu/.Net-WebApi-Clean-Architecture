using DotNetLibrary.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetLibrary.Data;

public class ApplicationDbContext : DbContext
{
    private static readonly DateTime SeedCreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleMap> UserRoleMaps => Set<UserRoleMap>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermissionMap> RolePermissionMaps => Set<RolePermissionMap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.EmailVerifiedAt);
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasMany(e => e.UserRoleMaps)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UserOtps)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasMany(e => e.UserRoleMaps)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.RolePermissionMaps)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new Role { Id = 1, Name = "Admin", CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new Role { Id = 2, Name = "User", CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false }
            );
        });

        modelBuilder.Entity<UserRoleMap>(entity =>
        {
            entity.ToTable("UserRoleMaps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RoleId).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
        });

        modelBuilder.Entity<UserOtp>(entity =>
        {
            entity.ToTable("UserOtps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Purpose).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CodeHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.UsedAt);
            entity.Property(e => e.IsUsed).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasIndex(e => new { e.UserId, e.Purpose, e.IsUsed, e.ExpiresAt });
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasData(
                new Permission { Id = 1, Name = "User Read", Code = "USER_READ", Description = "Read user records", CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new Permission { Id = 2, Name = "User Write", Code = "USER_WRITE", Description = "Create or update user records", CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new Permission { Id = 3, Name = "Role Read", Code = "ROLE_READ", Description = "Read role records", CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new Permission { Id = 4, Name = "Role Write", Code = "ROLE_WRITE", Description = "Create or update role records", CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false }
            );
        });

        modelBuilder.Entity<RolePermissionMap>(entity =>
        {
            entity.ToTable("RolePermissionMaps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.RoleId).IsRequired();
            entity.Property(e => e.PermissionId).IsRequired();
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissionMaps)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new RolePermissionMap { Id = 1, RoleId = 1, PermissionId = 1, CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new RolePermissionMap { Id = 2, RoleId = 1, PermissionId = 2, CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new RolePermissionMap { Id = 3, RoleId = 1, PermissionId = 3, CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new RolePermissionMap { Id = 4, RoleId = 1, PermissionId = 4, CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false },
                new RolePermissionMap { Id = 5, RoleId = 2, PermissionId = 1, CreatedAt = SeedCreatedAtUtc, CreatedBy = 0, IsDeleted = false }
            );
        });
    }
}
