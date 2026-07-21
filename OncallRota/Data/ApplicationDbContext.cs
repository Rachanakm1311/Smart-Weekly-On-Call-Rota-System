using Microsoft.EntityFrameworkCore;
using OncallRota.Models;

namespace OncallRota.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Role>             Roles             => Set<Role>();
        public DbSet<Team>             Teams             => Set<Team>();
        public DbSet<Employee>         Employees         => Set<Employee>();
        public DbSet<Application>      Applications      => Set<Application>();
        public DbSet<RotationQueue>    RotationQueues    => Set<RotationQueue>();
        public DbSet<HolidayCalendar>  HolidayCalendars  => Set<HolidayCalendar>();
        public DbSet<LeaveRequest>     LeaveRequests     => Set<LeaveRequest>();
        public DbSet<OnCallSchedule>   OnCallSchedules   => Set<OnCallSchedule>();
        public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
        public DbSet<Notification>     Notifications     => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder m)
        {
            base.OnModelCreating(m);

            // Employee → Team / Role
            m.Entity<Employee>()
             .HasOne(e => e.Team).WithMany(t => t.Employees)
             .HasForeignKey(e => e.TeamId).OnDelete(DeleteBehavior.Restrict);
            m.Entity<Employee>()
             .HasOne(e => e.Role).WithMany(r => r.Employees)
             .HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);

            // Application → Team
            m.Entity<Application>()
             .HasOne(a => a.Team).WithMany(t => t.Applications)
             .HasForeignKey(a => a.TeamId).OnDelete(DeleteBehavior.Restrict);

            // RotationQueue → Team / Employee
            m.Entity<RotationQueue>()
             .HasOne(rq => rq.Team).WithMany(t => t.RotationQueues)
             .HasForeignKey(rq => rq.TeamId).OnDelete(DeleteBehavior.Restrict);
            m.Entity<RotationQueue>()
             .HasOne(rq => rq.Employee).WithMany(e => e.RotationQueues)
             .HasForeignKey(rq => rq.EmployeeId).OnDelete(DeleteBehavior.Restrict);

            // LeaveRequest → Employee only (ApprovedBy is varchar, not FK)
            m.Entity<LeaveRequest>()
             .HasOne(lr => lr.Employee).WithMany(e => e.LeaveRequests)
             .HasForeignKey(lr => lr.EmployeeId).OnDelete(DeleteBehavior.Restrict);

            // OnCallSchedule → Team / Primary / Backup
            m.Entity<OnCallSchedule>()
             .HasOne(s => s.Team).WithMany(t => t.OnCallSchedules)
             .HasForeignKey(s => s.TeamId).OnDelete(DeleteBehavior.Restrict);
            m.Entity<OnCallSchedule>()
             .HasOne(s => s.PrimaryEmployee).WithMany()
             .HasForeignKey(s => s.PrimaryEmployeeId).OnDelete(DeleteBehavior.Restrict);
            m.Entity<OnCallSchedule>()
             .HasOne(s => s.BackupEmployee).WithMany()
             .HasForeignKey(s => s.BackupEmployeeId).OnDelete(DeleteBehavior.SetNull);

            // ShiftSwapRequest → Schedule / Employees (ApprovedBy is varchar, not FK)
            m.Entity<ShiftSwapRequest>()
             .HasOne(s => s.Schedule).WithMany(sc => sc.ShiftSwapRequests)
             .HasForeignKey(s => s.ScheduleId).OnDelete(DeleteBehavior.Restrict);
            m.Entity<ShiftSwapRequest>()
             .HasOne(s => s.RequestedByEmployee).WithMany()
             .HasForeignKey(s => s.RequestedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            m.Entity<ShiftSwapRequest>()
             .HasOne(s => s.SwapWithEmployee).WithMany()
             .HasForeignKey(s => s.SwapWithEmployeeId).OnDelete(DeleteBehavior.Restrict);

            // Notification → Employee
            m.Entity<Notification>()
             .HasOne(n => n.Employee).WithMany(e => e.Notifications)
             .HasForeignKey(n => n.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}