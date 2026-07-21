using OncallRota.Models;
namespace OncallRota.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public Employee         CurrentEmployee   { get; set; } = null!;
        public OnCallSchedule?  CurrentSchedule   { get; set; }
        public string           OnCallRole        { get; set; } = "Not Scheduled";
        public LeaveRequest?    ActiveLeave       { get; set; }
        public List<Notification> Notifications   { get; set; } = new();
    }
}
