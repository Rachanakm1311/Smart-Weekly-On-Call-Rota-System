using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;

namespace OncallRota.Controllers
{
    public class ScheduleController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IRotaService _rota;
        public ScheduleController(ApplicationDbContext db, IRotaService rota) { _db = db; _rota = rota; }

        public async Task<IActionResult> Index()
        {
            var g = RequireAdmin(); if (g != null) return g;
            ViewBag.Teams = await _db.Teams.Where(t => t.Status == "Active")
                                           .OrderBy(t => t.TeamName).ToListAsync();
            var list = await _db.OnCallSchedules
                .Include(s => s.Team).Include(s => s.PrimaryEmployee).Include(s => s.BackupEmployee)
                .OrderByDescending(s => s.WeekStartDate).ToListAsync();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var s = await _db.OnCallSchedules.FindAsync(id);
            if (s != null)
            {
                // Remove any linked shift-swap requests first (FK cascade safety)
                var swaps = _db.ShiftSwapRequests.Where(r => r.ScheduleId == id);
                _db.ShiftSwapRequests.RemoveRange(swaps);
                _db.OnCallSchedules.Remove(s);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Schedule entry deleted.";
            }
            else
            {
                TempData["Error"] = "Schedule entry not found.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRota(int teamId, string weekStart)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!DateTime.TryParse(weekStart, out var ws))
            { TempData["Error"] = "Invalid date."; return RedirectToAction(nameof(Index)); }
            var result = await _rota.GenerateWeeklyRotaAsync(teamId, ws);
            TempData[result.Any() ? "Success" : "Error"] = result.Any()
                ? $"Rota generated for week of {ws:dd MMM yyyy}."
                : "Rota already exists for this week, or there are fewer than 2 active queue members.";
            return RedirectToAction(nameof(Index));
        }
    }
}