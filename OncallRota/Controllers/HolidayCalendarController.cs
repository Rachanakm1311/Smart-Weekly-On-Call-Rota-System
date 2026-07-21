using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Models;

namespace OncallRota.Controllers
{
    public class HolidayCalendarController : BaseController
    {
        private readonly ApplicationDbContext _db;
        public HolidayCalendarController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var g = RequireLogin(); if (g != null) return g;
            var list = await _db.HolidayCalendars.Where(h=>h.Status=="Active")
                .OrderBy(h=>h.HolidayDate).ToListAsync();
            return View(list);
        }

        public IActionResult Create() { var g = RequireAdmin(); if (g != null) return g; return View(new HolidayCalendar()); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HolidayCalendar h)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) return View(h);
            _db.HolidayCalendars.Add(h); await _db.SaveChangesAsync();
            TempData["Success"] = "Holiday added."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var h = await _db.HolidayCalendars.FindAsync(id);
            if (h == null) return NotFound(); return View(h);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HolidayCalendar h)
        {
            var g = RequireAdmin(); if (g != null) return g;
            if (!ModelState.IsValid) return View(h);
            _db.HolidayCalendars.Update(h); await _db.SaveChangesAsync();
            TempData["Success"] = "Holiday updated."; return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var g = RequireAdmin(); if (g != null) return g;
            var h = await _db.HolidayCalendars.FindAsync(id);
            if (h != null) { h.Status = "Inactive"; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Holiday removed."; return RedirectToAction(nameof(Index));
        }
    }
}
