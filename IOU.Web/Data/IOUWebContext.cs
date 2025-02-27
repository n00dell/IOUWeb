using IOU.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Data
{
    public class IOUWebContext : IdentityDbContext<ApplicationUser>
    {
        public IOUWebContext(DbContextOptions<IOUWebContext> options)
                : base(options)
            {
            }
        public DbSet<Student> Student { get; set; }
        public DbSet<Guardian> Guardian { get; set; }
        public DbSet<Lender> Lender { get; set; }

        public DbSet<Notification> Notification { get; set; }
        public DbSet<ScheduledPayment> ScheduledPayment { get; set; }
        public DbSet<StudentGuardian> StudentGuardian { get; set; }
        public DbSet<Debt> Debt { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Debt>(entity =>
            {
                entity.HasOne(d => d.Student)
                    .WithMany(s => s.Debts)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Lender)
                    .WithMany(l => l.IssuedDebts)
                    .HasForeignKey(d => d.LenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Student>()
        .HasOne(s => s.User)
        .WithOne(u => u.Student)
        .HasForeignKey<Student>(s => s.UserId)
        .OnDelete(DeleteBehavior.NoAction);

            // Configure one-to-one relationship between ApplicationUser and Lender
            modelBuilder.Entity<Lender>()
                .HasOne(l => l.User)
                .WithOne(u => u.Lender)
                .HasForeignKey<Lender>(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure one-to-one relationship between ApplicationUser and Guardian
            modelBuilder.Entity<Guardian>()
                .HasOne(g => g.User)
                .WithOne(u => u.Guardian)
                .HasForeignKey<Guardian>(g => g.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<StudentGuardian>()
                .HasKey(sg => new { sg.StudentUserId, sg.GuardianUserId });
            modelBuilder.Entity<StudentGuardian>()
                .HasOne(sg => sg.Student)
                .WithMany(s => s.StudentGuardians)
                .HasForeignKey(sg => sg.StudentUserId);
            modelBuilder.Entity<StudentGuardian>()
                .HasOne(sg => sg.Guardian)
                .WithMany(g => g.StudentGuardians)
                .HasForeignKey(sg => sg.GuardianUserId);
        }


    }
}
