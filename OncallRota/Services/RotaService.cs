using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;
using OncallRota.Models;

namespace OncallRota.Services
{
    public class RotaService : IRotaService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notify;
        public RotaService(ApplicationDbContext db, INotificationService notify)
        {
            _db = db; _notify = notify;
        }

        /// <summary>
        /// Generates one week of on-call entries for every team, starting at weekStart.
        /// Primary = queue[0], Backup = queue[1], skipping employees on approved leave.
        /// </summary>
        public async Task<List<OnCallSchedule>> GenerateWeeklyRotaAsync(int teamId, DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(6);

            // Get active queue sorted by position
            var queue = await _db.RotationQueues
                .Where(q => q.TeamId == teamId && q.IsActive == true)
                .OrderBy(q => q.QueuePosition)
                .Include(q => q.Employee)
                .ToListAsync();

            if (queue.Count < 2) return new List<OnCallSchedule>();

            // Get employees on approved leave that week
            var onLeave = await _db.LeaveRequests
                .Where(l => l.Status == "Approved" &&
                            l.StartDate <= weekEnd && l.EndDate >= weekStart)
                .Select(l => l.EmployeeId)
                .ToListAsync();

            // Get regions that have a public holiday falling in this week
            var holidayRegions = await _db.HolidayCalendars
                .Where(h => h.Status == "Active" &&
                            h.HolidayDate >= weekStart &&
                            h.HolidayDate <= weekEnd &&
                            h.Location != null && h.Location != "")
                .Select(h => h.Location!)
                .Distinct()
                .ToListAsync();

            // Employees who opted to work on the holiday (approved WorkOnHoliday request this week)
            var workingOnHoliday = await _db.LeaveRequests
                .Where(l => l.WorkOnHoliday && l.Status == "Approved" &&
                            l.StartDate <= weekEnd && l.EndDate >= weekStart)
                .Select(l => l.EmployeeId)
                .ToListAsync();

            // Employees whose region has a holiday this week — but exclude those who opted to work
            var onHoliday = queue
                .Where(q => q.Employee != null &&
                            !string.IsNullOrEmpty(q.Employee.Region) &&
                            holidayRegions.Contains(q.Employee.Region) &&
                            !workingOnHoliday.Contains(q.EmployeeId))
                .Select(q => q.EmployeeId)
                .ToList();

            // Combined skip list: on leave OR on regional holiday (but NOT if working on holiday)
            var skipIds = onLeave.Concat(onHoliday).Distinct().ToList();

            // How many weeks have we generated so far for this team? Use count to rotate pointer
            var existingCount = await _db.OnCallSchedules
                .Where(s => s.TeamId == teamId)
                .CountAsync();

            int n = queue.Count;
            int primaryIdx  = existingCount % n;

            // Walk forward until we find an available primary (skips leave + regional holidays)
            int found = 0;
            while (found < n && skipIds.Contains(queue[primaryIdx % n].EmployeeId)) { primaryIdx++; found++; }
            int backupIdx = (primaryIdx + 1) % n;
            found = 0;
            while (found < n && (skipIds.Contains(queue[backupIdx].EmployeeId) || backupIdx == primaryIdx % n)) { backupIdx = (backupIdx + 1) % n; found++; }

            var primary = queue[primaryIdx % n].Employee!;
            var backup  = queue[backupIdx].Employee!;

            // Avoid duplicate for same week
            var existing = await _db.OnCallSchedules
                .FirstOrDefaultAsync(s => s.TeamId == teamId &&
                                          s.WeekStartDate == weekStart.Date);
            if (existing != null) return new List<OnCallSchedule>();

            var schedule = new OnCallSchedule
            {
                TeamId             = teamId,
                WeekStartDate      = weekStart.Date,
                WeekEndDate        = weekEnd.Date,
                PrimaryEmployeeId  = primary.EmployeeId,
                BackupEmployeeId   = backup.EmployeeId,
                Status             = "Active"
            };
            _db.OnCallSchedules.Add(schedule);
            await _db.SaveChangesAsync();

            await _notify.SendAsync(primary.EmployeeId, "WeeklyReminder",
                $"You are the PRIMARY on-call engineer for week {weekStart:dd MMM yyyy} – {weekEnd:dd MMM yyyy}.");
            await _notify.SendAsync(backup.EmployeeId, "WeeklyReminder",
                $"You are the BACKUP on-call engineer for week {weekStart:dd MMM yyyy} – {weekEnd:dd MMM yyyy}.");

            return new List<OnCallSchedule> { schedule };
        }

        public async Task<Employee?> GetNextAvailableEmployeeAsync(int teamId, DateTime weekStart, DateTime weekEnd, IEnumerable<int> excludeIds)
        {
            var onLeave = await _db.LeaveRequests
                .Where(l => l.Status == "Approved" &&
                            l.StartDate <= weekEnd && l.EndDate >= weekStart)
                .Select(l => l.EmployeeId).ToListAsync();

            var holidayRegions = await _db.HolidayCalendars
                .Where(h => h.Status == "Active" &&
                            h.HolidayDate >= weekStart &&
                            h.HolidayDate <= weekEnd &&
                            h.Location != null && h.Location != "")
                .Select(h => h.Location!)
                .Distinct()
                .ToListAsync();

            var workingOnHoliday = await _db.LeaveRequests
                .Where(l => l.WorkOnHoliday && l.Status == "Approved" &&
                            l.StartDate <= weekEnd && l.EndDate >= weekStart)
                .Select(l => l.EmployeeId)
                .ToListAsync();

            var onHoliday = await _db.RotationQueues
                .Where(q => q.TeamId == teamId && q.IsActive == true &&
                            q.Employee != null &&
                            q.Employee.Region != null &&
                            holidayRegions.Contains(q.Employee.Region) &&
                            !workingOnHoliday.Contains(q.EmployeeId))
                .Select(q => q.EmployeeId)
                .ToListAsync();

            var all = excludeIds.Concat(onLeave).Concat(onHoliday).Distinct();

            return await _db.RotationQueues
                .Where(q => q.TeamId == teamId && q.IsActive == true && !all.Contains(q.EmployeeId))
                .OrderBy(q => q.QueuePosition)
                .Select(q => q.Employee)
                .FirstOrDefaultAsync();
        }
    }
}
