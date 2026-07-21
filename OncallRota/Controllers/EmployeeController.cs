using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.ViewModels;

namespace OncallRota.Controllers
{
    public class EmployeeController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment  _env;
        public EmployeeController(ApplicationDbContext db, IWebHostEnvironment env)
        { _db = db; _env = env; }

        public async Task<IActionResult> Index()
        {
            var g = RequireLogin(); if (g != null) return g;
            var empId = SessionEmployeeId!.Value;
            var today = DateTime.Today;

            var emp = await _db.Employees.Include(e => e.Team).Include(e => e.Role)
                                         .FirstOrDefaultAsync(e => e.EmployeeId == empId);
            if (emp == null) return RedirectToAction("Login", "Auth");

            var sched = await _db.OnCallSchedules
                .Include(s => s.Team)
                .Include(s => s.PrimaryEmployee)
                .Include(s => s.BackupEmployee)
                .FirstOrDefaultAsync(s => s.WeekStartDate <= today && s.WeekEndDate >= today &&
                    (s.PrimaryEmployeeId == empId || s.BackupEmployeeId == empId));

            string role = "Not Scheduled";
            if (sched != null)
                role = sched.PrimaryEmployeeId == empId ? "Primary On-Call" : "Backup On-Call";

            var leave = await _db.LeaveRequests
                .Where(l => l.EmployeeId == empId && l.StartDate <= today && l.EndDate >= today && l.Status == "Approved")
                .FirstOrDefaultAsync();

            var notifs = await _db.Notifications
                .Where(n => n.EmployeeId == empId)
                .OrderByDescending(n => n.SentDate).Take(5).ToListAsync();

            var vm = new EmployeeDashboardViewModel
            {
                CurrentEmployee = emp, CurrentSchedule = sched,
                OnCallRole = role, ActiveLeave = leave, Notifications = notifs
            };
            return View(vm);
        }

        // My Schedule — only the logged-in employee's own weeks
        public async Task<IActionResult> MySchedule()
        {
            var g = RequireLogin(); if (g != null) return g;
            var empId = SessionEmployeeId!.Value;
            var schedules = await _db.OnCallSchedules
                .Include(s => s.Team).Include(s => s.PrimaryEmployee).Include(s => s.BackupEmployee)
                .Where(s => s.PrimaryEmployeeId == empId || s.BackupEmployeeId == empId)
                .OrderByDescending(s => s.WeekStartDate).ToListAsync();
            return View(schedules);
        }

        // Full On-Call Schedule — all teams, all weeks, visible to every employee
        public async Task<IActionResult> FullSchedule()
        {
            var g = RequireLogin(); if (g != null) return g;
            var today = DateTime.Today;
            var schedules = await _db.OnCallSchedules
                .Include(s => s.Team)
                .Include(s => s.PrimaryEmployee)
                .Include(s => s.BackupEmployee)
                .OrderByDescending(s => s.WeekStartDate)
                .ThenBy(s => s.Team!.TeamName)
                .ToListAsync();

            // Group by week so the view can render week blocks
            ViewBag.Today        = today;
            ViewBag.MyEmployeeId = SessionEmployeeId!.Value;
            return View(schedules);
        }

        // My Profile
        public async Task<IActionResult> Profile()
        {
            var g = RequireLogin(); if (g != null) return g;
            var emp = await _db.Employees
                .Include(e => e.Team).Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.EmployeeId == SessionEmployeeId!.Value);
            if (emp == null) return RedirectToAction("Login", "Auth");
            return View(emp);
        }

        // Self-service remove picture
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePicture()
        {
            var g = RequireLogin(); if (g != null) return g;
            var emp = await _db.Employees.FindAsync(SessionEmployeeId!.Value);
            if (emp == null) return RedirectToAction("Login", "Auth");

            if (!string.IsNullOrEmpty(emp.ProfilePicture) && !emp.ProfilePicture.Contains("default"))
            {
                var file = Path.Combine(_env.WebRootPath,
                    emp.ProfilePicture.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
            }

            emp.ProfilePicture = null;
            await _db.SaveChangesAsync();
            HttpContext.Session.SetString("ProfilePicture", "");
            TempData["Success"] = "Profile picture removed.";
            return RedirectToAction(nameof(Profile));
        }

        // Self-service profile picture upload (employee can update own photo)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile photo)
        {
            var g = RequireLogin(); if (g != null) return g;
            var emp = await _db.Employees.FindAsync(SessionEmployeeId!.Value);
            if (emp == null) return RedirectToAction("Login", "Auth");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(photo?.FileName ?? "").ToLowerInvariant();
            if (photo == null || photo.Length == 0 || !allowed.Contains(ext))
            {
                TempData["Error"] = "Please select a valid image file (jpg, png, gif, webp).";
                return RedirectToAction(nameof(Profile));
            }

            // Remove old file if custom
            if (!string.IsNullOrEmpty(emp.ProfilePicture) && !emp.ProfilePicture.Contains("default"))
            {
                var old = Path.Combine(_env.WebRootPath,
                    emp.ProfilePicture.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
            }

            var profilesDir = Path.Combine(_env.WebRootPath, "images", "profiles");
            Directory.CreateDirectory(profilesDir);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(profilesDir, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await photo.CopyToAsync(stream);

            emp.ProfilePicture = $"/images/profiles/{fileName}";
            await _db.SaveChangesAsync();

            // Update session so topbar avatar refreshes immediately
            HttpContext.Session.SetString("ProfilePicture", emp.ProfilePicture);
            TempData["Success"] = "Profile picture updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
