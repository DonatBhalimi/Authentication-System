using Microsoft.EntityFrameworkCore;
using Model.Models;

namespace Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
        public DbSet<TwoFactorCode> TwoFactorCodes => Set<TwoFactorCode>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<AppUser>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.UserName).IsRequired().HasMaxLength(50);
                e.Property(x => x.Email).IsRequired().HasMaxLength(100);
                e.Property(x => x.PasswordHash).IsRequired();

                e.HasIndex(x => x.UserName).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
            });

            b.Entity<EmailVerification>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).IsRequired().HasMaxLength(20);

                e.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<TwoFactorCode>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).IsRequired().HasMaxLength(10);

                e.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
