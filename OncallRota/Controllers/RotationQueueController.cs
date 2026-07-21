using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Models;
using OncallRota.ViewModels;

namespace OncallRota.Controllers
{
    public class RotationQueueController : BaseController
    {
        private readonly ApplicationDbContext _db;
        public RotationQueueController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? teamId)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var teams = await _db.Teams.Where(t=>t.Status=="Active").OrderBy(t=>t.TeamName).ToListAsync();
            int selectedTeam = teamId ?? (teams.Any() ? teams[0].TeamId : 0);
            var queue = await _db.RotationQueues.Include(q=>q.Employee)
                .Where(q=>q.TeamId == selectedTeam).OrderBy(q=>q.QueuePosition).ToListAsync();
            var teamEmps = await _db.Employees.Where(e=>e.TeamId==selectedTeam && e.Status=="Active").ToListAsync();
            var vm = new RotationQueueViewModel
            {
                TeamId = selectedTeam,
                TeamName = teams.FirstOrDefault(t=>t.TeamId==selectedTeam)?.TeamName ?? "",
                Queue = queue, TeamEmployees = teamEmps, AllTeams = teams
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToQueue(int teamId, int employeeId)
        {
            var g = RequireAdmin(); if (g != null) return g;

            // Guard: employeeId must be a real employee
            if (employeeId <= 0)
            {
                TempData["Error"] = "Please select an employee before adding to the queue.";
                return RedirectToAction(nameof(Index), new { teamId });
            }

            // Guard: employee must actually exist in the Employees table
            var empExists = await _db.Employees.AnyAsync(e => e.EmployeeId == employeeId);
            if (!empExists)
            {
                TempData["Error"] = "Selected employee does not exist.";
                return RedirectToAction(nameof(Index), new { teamId });
            }

            // Guard: not already in queue for this team
            if (await _db.RotationQueues.AnyAsync(q=>q.TeamId==teamId && q.EmployeeId==employeeId))
            {
                TempData["Error"] = "Employee is already in the queue.";
                return RedirectToAction(nameof(Index), new { teamId });
            }

            int nextPos = (await _db.RotationQueues.Where(q=>q.TeamId==teamId).MaxAsync(q=>(int?)q.QueuePosition) ?? 0) + 1;
            _db.RotationQueues.Add(new RotationQueue { TeamId=teamId, EmployeeId=employeeId, QueuePosition=nextPos, IsActive=true });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Employee added to queue.";
            return RedirectToAction(nameof(Index), new { teamId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveUp(int queueId, int teamId)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var current = await _db.RotationQueues.FindAsync(queueId);
            if (current == null || current.QueuePosition <= 1) return RedirectToAction(nameof(Index), new{teamId});
            var above = await _db.RotationQueues.FirstOrDefaultAsync(q=>q.TeamId==teamId && q.QueuePosition==current.QueuePosition-1);
            if (above != null) { above.QueuePosition++; current.QueuePosition--; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index), new{teamId});
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveDown(int queueId, int teamId)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var current = await _db.RotationQueues.FindAsync(queueId);
            if (current == null) return RedirectToAction(nameof(Index), new{teamId});
            var below = await _db.RotationQueues.FirstOrDefaultAsync(q=>q.TeamId==teamId && q.QueuePosition==current.QueuePosition+1);
            if (below != null) { below.QueuePosition--; current.QueuePosition++; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index), new{teamId});
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int queueId, int teamId)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var q = await _db.RotationQueues.FindAsync(queueId);
            if (q != null) { _db.RotationQueues.Remove(q); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Removed from queue.";
            return RedirectToAction(nameof(Index), new{teamId});
        }
    }
}