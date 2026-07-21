using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;
using OncallRota.Models;

namespace OncallRota.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService        _email;

        public NotificationService(ApplicationDbContext db, IEmailService email)
        {
            _db    = db;
            _email = email;
        }

        /// <summary>
        /// Saves an in-app notification AND sends a real email to the employee.
        /// </summary>
        public async Task SendAsync(int employeeId, string type, string message)
        {
            // 1. Save in-app notification
            _db.Notifications.Add(new Notification
            {
                EmployeeId       = employeeId,
                NotificationType = type,
                Message          = message,
                SentDate         = DateTime.UtcNow,
                Status           = "Unread"
            });
            await _db.SaveChangesAsync();

            // 2. Look up real email address
            var emp = await _db.Employees
                               .AsNoTracking()
                               .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (emp?.Email == null) return;

            // 3. Build subject + HTML from type
            var (subject, html) = BuildEmail(type, message, emp.EmployeeName);

            // 4. Send (failures are logged, never thrown)
            await _email.SendEmailAsync(emp.Email, emp.EmployeeName, subject, html);
        }

        public async Task MarkReadAsync(int notificationId)
        {
            var n = await _db.Notifications.FindAsync(notificationId);
            if (n != null) { n.Status = "Sent"; await _db.SaveChangesAsync(); }
        }

        // ── Email HTML builder ───────────────────────────────────────────
        private static (string subject, string html) BuildEmail(
            string type, string message, string name)
        {
            var (icon, colour, subject) = type switch
            {
                "WeeklyReminder"      => ("&#128197;", "#4f46e5", "On-Call Schedule \u2013 You Are Assigned This Week"),
                "LeaveApproved"       => ("&#9989;",   "#10b981", "Leave Request Approved"),
                "LeaveRejected"       => ("&#10060;",  "#ef4444", "Leave Request Rejected"),
                "LeaveSubmitted"      => ("&#128203;", "#f59e0b", "Leave Request Received"),
                "ShiftSwapApproved"   => ("&#128260;", "#06b6d4", "Shift Swap Request Approved"),
                "ShiftSwapRejected"   => ("&#10060;",  "#ef4444", "Shift Swap Request Rejected"),
                "ShiftSwapSubmitted"  => ("&#128260;", "#f59e0b", "Shift Swap Request Submitted"),
                "ShiftSwapRequested"  => ("&#128276;", "#8b5cf6", "Shift Swap Request \u2013 Action Required"),
                _                     => ("&#128276;", "#4f46e5", $"Notification \u2013 {type}")
            };

            var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""utf-8""/>
<style>
  body{{font-family:-apple-system,'Segoe UI',sans-serif;background:#f1f5f9;margin:0;padding:0}}
  .wrap{{max-width:560px;margin:32px auto;background:#fff;border-radius:10px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,.08)}}
  .hdr{{background:{colour};padding:28px 32px;color:#fff;text-align:center}}
  .hdr .icon{{font-size:2.5rem;display:block;margin-bottom:8px}}
  .hdr h1{{margin:0;font-size:1.25rem;font-weight:700}}
  .body{{padding:28px 32px}}
  .body p{{color:#374151;line-height:1.7;margin:0 0 16px}}
  .msg-box{{background:#f8fafc;border-left:4px solid {colour};border-radius:0 6px 6px 0;padding:14px 18px;margin:20px 0;color:#1e293b;font-weight:600}}
  .footer{{background:#f8fafc;padding:16px 32px;text-align:center;color:#94a3b8;font-size:.78rem;border-top:1px solid #e5e7eb}}
</style>
</head>
<body>
<div class=""wrap"">
  <div class=""hdr"">
    <span class=""icon"">{icon}</span>
    <h1>Smart On-Call Rota</h1>
  </div>
  <div class=""body"">
    <p>Hi <strong>{name}</strong>,</p>
    <div class=""msg-box"">{message}</div>
    <p>Please log in to the <strong>Smart On-Call Rota</strong> system for full details.</p>
    <p style=""color:#94a3b8;font-size:.85rem"">This is an automated notification. Please do not reply to this email.</p>
  </div>
  <div class=""footer"">Smart Weekly On-Call Rota Management System &nbsp;|&nbsp; &copy; {DateTime.UtcNow.Year}</div>
</div>
</body>
</html>";

            return (subject, html);
        }
    }
}
