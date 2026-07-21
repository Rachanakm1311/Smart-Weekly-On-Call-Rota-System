using OncallRota.Models;
namespace OncallRota.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalTeams             { get; set; }
        public int TotalEmployees         { get; set; }
        public int TotalApplications      { get; set; }
        public int EmployeesOnLeave       { get; set; }
        public int PendingLeaveRequests   { get; set; }
        public int PendingSwapRequests    { get; set; }
        public List<OnCallSchedule> CurrentWeekSchedules { get; set; } = new();
    }
}
