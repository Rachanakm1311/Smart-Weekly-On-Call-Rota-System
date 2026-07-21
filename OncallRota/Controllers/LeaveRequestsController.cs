using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;
using OncallRota.Models;

namespace OncallRota.Controllers
{
    public class LeaveRequestsController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notify;
        public LeaveRequestsController(ApplicationDbContext db, INotificationService notify)
        { _db = db; _notify = notify; }

        // Admin: all requests
        public async Task<IActionResult> Index()
        {
            var g = RequireLogin(); if (g != null) return g;
            if (IsAdmin)
            {
                var all = await _db.LeaveRequests.Include(l=>l.Employee)
                    .OrderByDescending(l=>l.LeaveId).ToListAsync();
                return View("AdminIndex", all);
            }
            var mine = await _db.LeaveRequests.Where(l=>l.EmployeeId==SessionEmployeeId)
                .OrderByDescending(l=>l.LeaveId).ToListAsync();
            return View("EmployeeIndex", mine);
        }

        // Employee: Apply leave
        public IActionResult Apply() { var g = RequireLogin(); if (g != null) return g; return View(new LeaveRequest()); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequest req)
        {
            var g = RequireLogin(); if (g != null) return g;
            if (req.StartDate.HasValue && req.EndDate.HasValue && req.StartDate >= req.EndDate)
                ModelState.AddModelError("", "End date must be after start date.");

            // Holiday Work validation
            if (req.WorkOnHoliday)
            {
                if (!req.CompOffDate.HasValue)
                    ModelState.AddModelError("CompOffDate", "Please select a comp-off date when opting to work on a holiday.");
                else if (req.CompOffDate <= req.EndDate)
                    ModelState.AddModelError("CompOffDate", "Comp-off date must be after the holiday end date.");
            }
            else
            {
                // Clear these fields if the user did not tick the checkbox
                req.CompOffDate = null;
            }

            if (!ModelState.IsValid) return View(req);

            req.EmployeeId = SessionEmployeeId!.Value;
            req.Status     = "Pending";
            _db.LeaveRequests.Add(req);
            await _db.SaveChangesAsync();

            // Tailored notification
            var notifMsg = req.WorkOnHoliday
                ? $"Your Holiday Work request for {req.StartDate?.ToString("dd MMM yyyy")} has been submitted. " +
                  $"Comp-off date: {req.CompOffDate?.ToString("dd MMM yyyy")}. Pending approval."
                : $"Your {req.LeaveType} leave request from {req.StartDate?.ToString("dd MMM yyyy")} to " +
                  $"{req.EndDate?.ToString("dd MMM yyyy")} has been submitted and is pending approval.";
            await _notify.SendAsync(req.EmployeeId, "LeaveSubmitted", notifMsg);

            TempData["Success"] = req.WorkOnHoliday
                ? "Holiday Work request submitted. You will remain eligible for on-call once approved."
                : "Leave request submitted.";
            return RedirectToAction(nameof(Index));
        }

        // Admin: Approve
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var req = await _db.LeaveRequests.FindAsync(id);
            if (req != null)
            {
                req.Status = "Approved"; req.ApprovedBy = SessionEmployeeName; req.ApprovedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _notify.SendAsync(req.EmployeeId, "LeaveApproved", "Your leave request has been approved.");
            }
            TempData["Success"] = "Leave approved."; return RedirectToAction(nameof(Index));
        }

        // Admin: Reject
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var req = await _db.LeaveRequests.FindAsync(id);
            if (req != null)
            {
                req.Status = "Rejected"; req.ApprovedBy = SessionEmployeeName; req.ApprovedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _notify.SendAsync(req.EmployeeId, "LeaveRejected", "Your leave request has been rejected.");
            }
            TempData["Error"] = "Leave rejected."; return RedirectToAction(nameof(Index));
        }
    }
}
