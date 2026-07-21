using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Models;

namespace OncallRota.Controllers
{
    public class ApplicationsController : BaseController
    {
        private readonly ApplicationDbContext _db;
        public ApplicationsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var q = _db.Applications.Include(a=>a.Team).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(a => a.ApplicationName.Contains(search));
            return View(await q.OrderBy(a=>a.ApplicationName).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            var g = RequireAdmin(); if (g != null) return g;
            ViewBag.Teams = new SelectList(await _db.Teams.Where(t=>t.Status=="Active").ToListAsync(),"TeamId","TeamName");
            return View(new Application());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Application app)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) { ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(),"TeamId","TeamName"); return View(app); }
            if (await _db.Applications.AnyAsync(a => a.ApplicationName == app.ApplicationName && a.TeamId == app.TeamId))
            { ModelState.AddModelError("","Application already exists for this team."); ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(),"TeamId","TeamName"); return View(app); }
            _db.Applications.Add(app); await _db.SaveChangesAsync();
            TempData["Success"] = "Application created."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var app = await _db.Applications.FindAsync(id);
            if (app == null) return NotFound();
            ViewBag.Teams = new SelectList(await _db.Teams.Where(t=>t.Status=="Active").ToListAsync(),"TeamId","TeamName", app.TeamId);
            return View(app);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Application app)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) { ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(),"TeamId","TeamName"); return View(app); }
            _db.Applications.Update(app); await _db.SaveChangesAsync();
            TempData["Success"] = "Application updated."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var app = await _db.Applications.FindAsync(id);
            if (app != null) { _db.Applications.Remove(app); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Application deleted."; return RedirectToAction(nameof(Index));
        }
    }
}
