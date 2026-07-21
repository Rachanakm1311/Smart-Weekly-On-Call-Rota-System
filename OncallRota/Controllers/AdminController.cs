using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.ViewModels;

namespace OncallRota.Controllers
{
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _db;
        public AdminController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var g = RequireAdmin(); if (g != null) return g;

            var today = DateTime.Today;
            var vm = new AdminDashboardViewModel
            {
                TotalTeams           = await _db.Teams.CountAsync(t => t.Status == "Active"),
                TotalEmployees       = await _db.Employees.CountAsync(e => e.Status == "Active"),
                TotalApplications    = await _db.Applications.CountAsync(a => a.Status == "Active"),
                EmployeesOnLeave     = await _db.LeaveRequests
                                           .CountAsync(l => l.Status == "Approved" &&
                                                            l.StartDate <= today && l.EndDate >= today),
                PendingLeaveRequests = await _db.LeaveRequests.CountAsync(l => l.Status == "Pending"),
                PendingSwapRequests  = await _db.ShiftSwapRequests.CountAsync(s => s.Status == "Pending"),
                CurrentWeekSchedules = await _db.OnCallSchedules
                                           .Include(s => s.Team)
                                           .Include(s => s.PrimaryEmployee)
                                           .Include(s => s.BackupEmployee)
                                           .Where(s => s.WeekStartDate <= today && s.WeekEndDate >= today && s.Status == "Active")
                                           .ToListAsync()
            };
            return View(vm);
        }
    }
}
