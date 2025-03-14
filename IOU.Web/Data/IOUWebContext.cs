﻿using IOU.Web.Models;
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
        
        public DbSet<Lender> Lender { get; set; }

        public DbSet<Notification> Notification { get; set; }
        public DbSet<ScheduledPayment> ScheduledPayment { get; set; }
        
        public DbSet<Debt> Debt { get; set; }

        public DbSet<Dispute> Dispute { get; set; }

        public DbSet<DisputeDetail> DisputeDetail { get; set; }

        public DbSet<SupportingDocument> SupportingDocument { get; set; }

        public DbSet<DebtEvidence> DebtEvidence { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Model.GetEntityTypes()
       .Where(e => e.ClrType == typeof(Debt))
       .ToList()
       .ForEach(e => e.GetProperties()
           .Where(p => p.ClrType == typeof(decimal))
           .ToList()
           .ForEach(p => p.SetPrecision(18)));

            modelBuilder.Entity<Debt>(entity =>
            {
                entity.HasOne(d => d.Student)
                    .WithMany(s => s.Debts)
                    .HasForeignKey(d => d.StudentUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Lender)
                    .WithMany(l => l.IssuedDebts)
                    .HasForeignKey(d => d.LenderUserId)
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
            modelBuilder.Entity<DebtEvidence>(entity =>
            {
                entity.HasOne(d => d.Dispute)
                    .WithMany(d => d.LenderEvidence)
                    .HasForeignKey(d => d.DisputeId)
                    .OnDelete(DeleteBehavior.Restrict);  // Change from Cascade to Restrict
                
                entity.HasOne(d => d.Lender)
                    .WithMany(l => l.SubmittedEvidence)
                    .HasForeignKey(d => d.LenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Add this for Dispute relationships
            modelBuilder.Entity<Dispute>(entity =>
            {
                entity.HasOne(d => d.Debt)
                    .WithMany(d => d.Disputes)
                    .HasForeignKey(d => d.DebtId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            // Configure decimal precision for Debt
            modelBuilder.Entity<Debt>(entity =>
            {
                entity.Property(d => d.PrincipalAmount).HasPrecision(18, 2);
                entity.Property(d => d.CurrentBalance).HasPrecision(18, 2);
                entity.Property(d => d.InterestRate).HasPrecision(5, 2);
                entity.Property(d => d.AccumulatedInterest).HasPrecision(18, 2);
                entity.Property(d => d.LateFeeAmount).HasPrecision(18, 2);
                entity.Property(d => d.AccumulatedLateFees).HasPrecision(18, 2);
            });

            // Configure decimal precision for DisputeDetail
            modelBuilder.Entity<DisputeDetail>(entity =>
            {
                entity.Property(d => d.RequestedReductionAmount).HasPrecision(18, 2);
            });

            // Configure decimal precision for ScheduledPayment (if exists)
            modelBuilder.Entity<ScheduledPayment>(entity =>
            {
                entity.Property(p => p.Amount).HasPrecision(18, 2);
                entity.Property(p => p.InterestPortion).HasPrecision(18, 2);
                entity.Property(p => p.LateFeesPortion).HasPrecision(18, 2);
                entity.Property(p => p.PrincipalPortion).HasPrecision(18, 2);
            });

        }


    }
}
