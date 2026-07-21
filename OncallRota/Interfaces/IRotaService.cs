using OncallRota.Models;
namespace OncallRota.Interfaces
{
    public interface IRotaService
    {
        Task<List<OnCallSchedule>> GenerateWeeklyRotaAsync(int teamId, DateTime weekStart);
        Task<Employee?> GetNextAvailableEmployeeAsync(int teamId, DateTime weekStart, DateTime weekEnd, IEnumerable<int> excludeIds);
    }
}
