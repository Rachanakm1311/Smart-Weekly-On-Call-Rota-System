using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;
using OncallRota.Models;
using OncallRota.ViewModels;

namespace OncallRota.Controllers
{
    public class ShiftSwapController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notify;
        public ShiftSwapController(ApplicationDbContext db, INotificationService notify)
        { _db = db; _notify = notify; }

        public async Task<IActionResult> Index()
        {
            var g = RequireLogin(); if (g != null) return g;
            if (IsAdmin)
            {
                var all = await _db.ShiftSwapRequests
                    .Include(s=>s.RequestedByEmployee).Include(s=>s.SwapWithEmployee)
                    .Include(s=>s.Schedule).ThenInclude(sc=>sc!.Team)
                    .OrderByDescending(s=>s.SwapId).ToListAsync();
                return View("AdminIndex", all);
            }
            var today = DateTime.Today;
            var empId = SessionEmployeeId!.Value;
            var sched = await _db.OnCallSchedules
                .Include(s=>s.Team)
                .FirstOrDefaultAsync(s => s.WeekStartDate <= today && s.WeekEndDate >= today &&
                    (s.PrimaryEmployeeId==empId || s.BackupEmployeeId==empId));

            var colleagues = sched != null
                ? await _db.Employees.Where(e=>e.TeamId==sched.TeamId && e.EmployeeId!=empId && e.Status=="Active").ToListAsync()
                : new List<Employee>();

            var myReqs = await _db.ShiftSwapRequests
                .Include(s=>s.SwapWithEmployee).Include(s=>s.Schedule)
                .Where(s=>s.RequestedByEmployeeId==empId).OrderByDescending(s=>s.SwapId).ToListAsync();

            return View("EmployeeIndex", new ShiftSwapViewModel
            {
                MySchedule = sched, TeamColleagues = colleagues, MyRequests = myReqs
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int scheduleId, int swapWithEmployeeId, string reason)
        {
            var g = RequireLogin(); if (g != null) return g;
            if (await _db.ShiftSwapRequests.AnyAsync(s=>s.ScheduleId==scheduleId && s.RequestedByEmployeeId==SessionEmployeeId && s.Status=="Pending"))
            { TempData["Error"] = "You already have a pending swap request for this week."; return RedirectToAction(nameof(Index)); }
            _db.ShiftSwapRequests.Add(new ShiftSwapRequest
            {
                ScheduleId=scheduleId, RequestedByEmployeeId=SessionEmployeeId!.Value,
                SwapWithEmployeeId=swapWithEmployeeId, Reason=reason,
                RequestDate=DateTime.UtcNow, Status="Pending"
            });
            await _db.SaveChangesAsync();
            // Confirm to the requester
            await _notify.SendAsync(SessionEmployeeId!.Value, "ShiftSwapSubmitted",
                "Your shift swap request has been submitted and is pending admin approval.");
            // Alert the colleague being asked to swap
            await _notify.SendAsync(swapWithEmployeeId, "ShiftSwapRequested",
                $"{SessionEmployeeName} has requested to swap their on-call shift with you. Please await admin review.");
            TempData["Success"] = "Swap request submitted."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var req = await _db.ShiftSwapRequests.Include(s=>s.Schedule).FirstOrDefaultAsync(s=>s.SwapId==id);
            if (req != null)
            {
                req.Status = "Approved"; req.ApprovedBy = SessionEmployeeName; req.ApprovedDate = DateTime.UtcNow;
                // Perform the swap on the schedule
                if (req.Schedule != null)
                {
                    if (req.Schedule.PrimaryEmployeeId == req.RequestedByEmployeeId)
                        req.Schedule.PrimaryEmployeeId = req.SwapWithEmployeeId;
                    else req.Schedule.BackupEmployeeId = req.SwapWithEmployeeId;
                }
                await _db.SaveChangesAsync();
                await _notify.SendAsync(req.RequestedByEmployeeId, "ShiftSwapApproved","Your shift swap request has been approved.");
                await _notify.SendAsync(req.SwapWithEmployeeId,   "ShiftSwapApproved","You have been assigned an on-call shift via swap.");
            }
            TempData["Success"] = "Swap approved."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var req = await _db.ShiftSwapRequests.FindAsync(id);
            if (req != null)
            {
                req.Status = "Rejected"; req.ApprovedBy = SessionEmployeeName; req.ApprovedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _notify.SendAsync(req.RequestedByEmployeeId, "ShiftSwapRejected",
                    "Your shift swap request has been rejected by the admin.");
            }
            TempData["Error"] = "Swap rejected."; return RedirectToAction(nameof(Index));
        }
    }
}
