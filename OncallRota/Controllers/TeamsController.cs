using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Models;

namespace OncallRota.Controllers
{
    public class TeamsController : BaseController
    {
        private readonly ApplicationDbContext _db;
        public TeamsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var q = _db.Teams.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(t => t.TeamName.Contains(search));
            ViewBag.Search = search;
            return View(await q.OrderBy(t => t.TeamName).ToListAsync());
        }

        public IActionResult Create() { var g = RequireAdmin(); if (g != null) return g; return View(new Team()); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) return View(team);
            if (await _db.Teams.AnyAsync(t => t.TeamName == team.TeamName))
            { ModelState.AddModelError("TeamName","Team name already exists."); return View(team); }
            _db.Teams.Add(team); await _db.SaveChangesAsync();
            TempData["Success"] = "Team created."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var t = await _db.Teams.FindAsync(id);
            if (t == null) return NotFound(); return View(t);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Team team)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) return View(team);
            if (await _db.Teams.AnyAsync(t => t.TeamName == team.TeamName && t.TeamId != team.TeamId))
            { ModelState.AddModelError("TeamName","Team name already exists."); return View(team); }
            _db.Teams.Update(team); await _db.SaveChangesAsync();
            TempData["Success"] = "Team updated."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var t = await _db.Teams.FindAsync(id);
            if (t != null) { _db.Teams.Remove(t); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Team deleted."; return RedirectToAction(nameof(Index));
        }
    }
}
