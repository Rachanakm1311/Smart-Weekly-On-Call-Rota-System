using Microsoft.EntityFrameworkCore;
using OncallRota.Data;
using OncallRota.Interfaces;

namespace OncallRota.Services
{
    /// <summary>
    /// Hosted background service that automatically generates the NEXT week on-call rota
    /// every Friday at the configured time (default 08:00 local), so employees can plan ahead.
    ///
    /// Config section in appsettings.json:
    ///   "RotaScheduler": { "RunOnDayOfWeek": "Friday", "RunAtHour": 8, "RunAtMinute": 0 }
    /// </summary>
    public class WeeklyRotaBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration       _config;
        private readonly ILogger<WeeklyRotaBackgroundService> _logger;

        public WeeklyRotaBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<WeeklyRotaBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _config       = config;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[RotaScheduler] Background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = ComputeDelayUntilNextRun();
                _logger.LogInformation(
                    "[RotaScheduler] Next auto-generation in {delay:hh\\:mm\\:ss} (at {time:yyyy-MM-dd HH:mm} — publishing NEXT week rota).",
                    delay, DateTime.Now.Add(delay));

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                await RunGenerationAsync(stoppingToken);
            }

            _logger.LogInformation("[RotaScheduler] Background service stopping.");
        }

        // ── Core generation logic ─────────────────────────────────────────────────
        private async Task RunGenerationAsync(CancellationToken ct)
        {
            _logger.LogInformation("[RotaScheduler] Running weekly rota auto-generation at {now}.", DateTime.Now);

            try
            {
                using var scope  = _scopeFactory.CreateScope();
                var db           = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var rotaService  = scope.ServiceProvider.GetRequiredService<IRotaService>();

                // We run on Friday — the rota being generated is for NEXT week (Monday → Sunday).
                // Find how many days until the coming Monday (always 3 days from Friday).
                var today   = DateTime.Today;
                int daysToNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
                if (daysToNextMonday == 0) daysToNextMonday = 7;   // if somehow already Monday, go +7
                var weekStart = today.AddDays(daysToNextMonday);

                _logger.LogInformation(
                    "[RotaScheduler] Publishing rota for week starting {weekStart:dd MMM yyyy} (next Monday).",
                    weekStart);

                var activeTeams = await db.Teams
                    .Where(t => t.Status == "Active")
                    .Select(t => t.TeamId)
                    .ToListAsync(ct);

                int generated = 0, skipped = 0;

                foreach (var teamId in activeTeams)
                {
                    var result = await rotaService.GenerateWeeklyRotaAsync(teamId, weekStart);
                    if (result.Count > 0)
                    {
                        generated++;
                        _logger.LogInformation(
                            "[RotaScheduler] Generated rota for TeamId={teamId}, week {w}.",
                            teamId, weekStart.ToString("dd MMM yyyy"));
                    }
                    else
                    {
                        skipped++;
                        _logger.LogInformation(
                            "[RotaScheduler] Skipped TeamId={teamId} (already exists or queue too small).",
                            teamId);
                    }
                }

                _logger.LogInformation("[RotaScheduler] Done. Generated={g}, Skipped={s}.", generated, skipped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RotaScheduler] Error during auto-generation.");
            }
        }

        // ── Delay calculation ─────────────────────────────────────────────────────
        private TimeSpan ComputeDelayUntilNextRun()
        {
            var section = _config.GetSection("RotaScheduler");
            var dayName = section["RunOnDayOfWeek"] ?? "Friday";
            int hour    = section.GetValue<int?>("RunAtHour")   ?? 8;
            int minute  = section.GetValue<int?>("RunAtMinute") ?? 0;

            if (!Enum.TryParse<DayOfWeek>(dayName, ignoreCase: true, out var targetDay))
                targetDay = DayOfWeek.Friday;

            var now  = DateTime.Now;
            var next = now.Date.AddHours(hour).AddMinutes(minute);

            // Advance day-by-day until we land on the target weekday in the future
            int safety = 0;
            while (next.DayOfWeek != targetDay || next <= now)
            {
                next = next.AddDays(1);
                if (++safety > 14) break;   // guard against infinite loop
            }

            return next - now;
        }
    }
}