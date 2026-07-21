using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;

namespace OncallRota.Controllers
{
    public class NotificationsController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notify;
        public NotificationsController(ApplicationDbContext db, INotificationService notify)
        { _db = db; _notify = notify; }

        public async Task<IActionResult> Index()
        {
            var g = RequireLogin(); if (g != null) return g;
            var q = _db.Notifications.Include(n=>n.Employee).AsQueryable();
            if (!IsAdmin) q = q.Where(n=>n.EmployeeId==SessionEmployeeId);
            var list = await q.OrderByDescending(n=>n.SentDate).ToListAsync();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notify.MarkReadAsync(id);
            return Ok();
        }

        // Delete a single notification
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = RequireLogin(); if (g != null) return g;
            var n = await _db.Notifications.FindAsync(id);
            if (n != null)
            {
                // Employees can only delete their own notifications
                if (!IsAdmin && n.EmployeeId != SessionEmployeeId)
                    return Forbid();
                _db.Notifications.Remove(n);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Notification deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Delete ALL notifications visible to the current user
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var g = RequireLogin(); if (g != null) return g;
            IQueryable<OncallRota.Models.Notification> q = _db.Notifications;
            if (!IsAdmin) q = q.Where(n => n.EmployeeId == SessionEmployeeId);
            _db.Notifications.RemoveRange(await q.ToListAsync());
            await _db.SaveChangesAsync();
            TempData["Success"] = "All notifications deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}