using System;
using System.Linq;
using DAL.Data.Models;
using DAL.Data.Models.AIPlanning;
using DAL.Data.Models.IdentityModels;
using DAL.Data.Models.NotificationsLogs;
using DAL.Data.Models.Subscription;
using DAL.Data.Models.TasksAndReports;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ====================== DbSets ======================
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<TaskReport> TaskReports { get; set; }
        public DbSet<AnnualTarget> AnnualTargets { get; set; }
        public DbSet<MonthlyPlan> MonthlyPlans { get; set; }
        public DbSet<WeeklyPlan> WeeklyPlans { get; set; }
        public DbSet<PerformanceReport> PerformanceReports { get; set; }
        public DbSet<MonthlyPerformanceReport> MonthlyPerformanceReports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<WeeklyCost> WeeklyCosts { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<ImportantNote> ImportantNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ==================== Fluent API Configurations ====================

            // ApplicationUser
            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
                e.HasIndex(u => u.FullName);
                e.HasIndex(u => u.IsActive);
            });

            // Admin
            builder.Entity<Admin>(e =>
            {
                e.HasOne(a => a.User)
                    .WithOne(u => u.Admin)
                    .HasForeignKey<Admin>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(a => a.UserId).IsUnique();
            });

            // Manager
            builder.Entity<Manager>(e =>
            {
                e.HasOne(m => m.User)
                    .WithOne(u => u.Manager)
                    .HasForeignKey<Manager>(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Subscription)
                    .WithOne()
                    .HasForeignKey<Manager>(m => m.SubscriptionId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                e.HasIndex(m => m.UserId).IsUnique();
                e.HasIndex(m => m.SubscriptionEndsAt);
                // CurrentEmployeeCount index removed - property deleted
            });

            // Employee
            builder.Entity<Employee>(e =>
            {
                e.HasKey(emp => emp.Id);

                e.HasOne(emp => emp.User)
                    .WithOne(u => u.Employee)
                    .HasForeignKey<Employee>(emp => emp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(emp => emp.Manager)
                    .WithMany()
                    .HasForeignKey(emp => emp.ManagerUserId)
                    .OnDelete(DeleteBehavior.NoAction) 
                    .IsRequired(false);

                e.HasIndex(emp => emp.UserId).IsUnique();
                e.HasIndex(emp => emp.ManagerUserId);
            });

            // Subscription & Payment
            builder.Entity<SubscriptionPlan>(e =>
            {
                e.HasIndex(p => p.Name).IsUnique();
            });

            builder.Entity<Subscription>(e =>
            {
                e.HasOne(s => s.Manager)
                        .WithOne(m => m.Subscription)
                        .HasForeignKey<Subscription>(s => s.ManagerId)
                        .HasPrincipalKey<Manager>(m => m.Id)
                        .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.Plan)
                    .WithMany(p => p.Subscriptions)
                    .HasForeignKey(s => s.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(s => s.ManagerId);
                e.HasIndex(s => s.IsActive);
            });

            builder.Entity<Payment>(e =>
            {
                e.HasOne(p => p.User)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                    
                e.HasOne(p => p.PaymentMethod)
                    .WithMany()
                    .HasForeignKey(p => p.PaymentMethodId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                    
                e.HasOne(p => p.Invoice)
                    .WithMany()
                    .HasForeignKey(p => p.InvoiceId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                    
                e.HasIndex(p => p.TransactionId).IsUnique();
                e.HasIndex(p => p.UserId);
                e.HasIndex(p => p.SubscriptionId);
                e.Property(p => p.Amount).HasPrecision(18, 2);
            });
            
            builder.Entity<PaymentMethod>(e =>
            {
                e.HasOne(pm => pm.User)
                    .WithMany()
                    .HasForeignKey(pm => pm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                e.HasIndex(pm => pm.UserId);
                e.HasIndex(pm => new { pm.UserId, pm.IsDefault });
            });
            
            builder.Entity<Invoice>(e =>
            {
                e.HasOne(i => i.Subscription)
                    .WithMany()
                    .HasForeignKey(i => i.SubscriptionId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                e.HasOne(i => i.PaymentMethod)
                    .WithMany()
                    .HasForeignKey(i => i.PaymentMethodId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                    
                e.HasOne(i => i.Payment)
                    .WithOne(p => p.Invoice)
                    .HasForeignKey<Invoice>(i => i.PaymentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                    
                e.HasIndex(i => i.InvoiceNumber).IsUnique();
                e.HasIndex(i => i.SubscriptionId);
                e.Property(i => i.Amount).HasPrecision(18, 2);
            });
            
            builder.Entity<SubscriptionPlan>(e =>
            {
                e.Property(p => p.PricePerMonth).HasPrecision(18, 2);
            });

            // Tasks
            builder.Entity<TaskItem>(e =>
            {
                e.HasOne(t => t.CreatedBy)
                    .WithMany(u => u.TasksCreated)
                    .HasForeignKey(t => t.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                // REMOVED: Single employee assignment
                // e.HasOne(t => t.AssignedTo)
                //     .WithMany(emp => emp.AssignedTasks)
                //     .HasForeignKey(t => t.AssignedToEmployeeId)
                //     .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(t => new { t.Year, t.Month, t.WeekNumber });
                e.HasIndex(t => t.Deadline);
            });

            // TaskAssignments (Many-to-Many Join Table)
            builder.Entity<TaskAssignment>(e =>
            {
                e.HasKey(ta => ta.Id);

                e.HasOne(ta => ta.TaskItem)
                    .WithMany(t => t.Assignments)
                    .HasForeignKey(ta => ta.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ta => ta.Employee)
                    .WithMany(emp => emp.TaskAssignments)
                    .HasForeignKey(ta => ta.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ta => ta.AssignedBy)
                    .WithMany()
                    .HasForeignKey(ta => ta.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Prevent duplicate assignments (same task + same employee)
                e.HasIndex(ta => new { ta.TaskItemId, ta.EmployeeId }).IsUnique();

                // Performance indexes
                e.HasIndex(ta => ta.TaskItemId);
                e.HasIndex(ta => ta.EmployeeId);
                e.HasIndex(ta => ta.AssignedAt);
            });

            // AI Plans
            builder.Entity<AnnualTarget>(e =>
            {
                e.HasOne(at => at.Manager)
                    .WithMany()
                    .HasForeignKey(at => at.ManagerUserId)
                    .HasPrincipalKey(u => u.Id)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                e.HasIndex(at => new { at.ManagerUserId, at.Year }).IsUnique();
            });

            builder.Entity<MonthlyPlan>(e =>
            {
                e.HasIndex(mp => new { mp.AnnualTargetId, mp.Month }).IsUnique();
                e.HasOne(mp => mp.MonthlyPerformanceReport)
                    .WithOne(mpr => mpr.MonthlyPlan)
                    .HasForeignKey<MonthlyPerformanceReport>(mpr => mpr.MonthlyPlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<WeeklyPlan>(e =>
            {
                e.HasIndex(wp => new { wp.Year, wp.Month, wp.WeekNumber });
                e.HasIndex(wp => wp.AchievementPercentage);
            });

            // WeeklyCost
            builder.Entity<WeeklyCost>(e =>
            {
                e.HasOne(c => c.Employee)
                    .WithMany()
                    .HasForeignKey(c => c.EmployeeId)
                    .HasPrincipalKey(emp => emp.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(c => c.Amount).HasPrecision(18, 2);
                e.HasIndex(c => new { c.Year, c.Month, c.WeekNumber });
                e.HasIndex(c => c.Status);
            });

            // Suggestion
            builder.Entity<Suggestion>(e =>
            {
                e.HasOne(s => s.Employee)
                    .WithMany()
                    .HasForeignKey(s => s.EmployeeId)
                    .HasPrincipalKey(emp => emp.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(s => s.Status);
                e.HasIndex(s => s.CreatedAt);
            });

            // UserSettings
            builder.Entity<UserSettings>(e =>
            {
                e.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(s => s.UserId).IsUnique();
            });

            // Notification
            builder.Entity<Notification>(e =>
            {
                e.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(n => new { n.UserId, n.IsRead });
                e.HasIndex(n => n.Type);
            });

            // ImportantNote
            builder.Entity<ImportantNote>(e =>
            {
                e.HasOne(i => i.Employee)
                    .WithMany()
                    .HasForeignKey(i => i.EmployeeId)
                    .HasPrincipalKey(emp => emp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(i => i.EmployeeId);
                e.HasIndex(i => new { i.Year, i.Month, i.WeekNumber });
                e.HasIndex(i => i.CreatedAt);
            });

            // Global Query Filters (Soft Delete)
            builder.Entity<ApplicationUser>().HasQueryFilter(u => u.IsActive);
            builder.Entity<Manager>().HasQueryFilter(m => m.IsActive);
            builder.Entity<Employee>().HasQueryFilter(e => e.IsActive);

            // ===================== Seed Data =====================
            SeedData(builder);
        }

        private static void SeedData(ModelBuilder builder)
        {
            // Roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "admin-001", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "manager-001", Name = "Manager", NormalizedName = "MANAGER" },
                new IdentityRole { Id = "employee-001", Name = "Employee", NormalizedName = "EMPLOYEE" }
            );

            // Subscription Plans
            builder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Basic",
                    PricePerMonth = 50m,
                    MaxEmployees = 30,
                    Description = "حتى 30 موظف - جميع المميزات الأساسية",
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Pro",
                    PricePerMonth = 100m,
                    MaxEmployees = 50,
                    Description = "حتى 100 موظف - تقارير متقدمة ودعم أولوية",
                    IsActive = true
                }
            );
        }
    }
}