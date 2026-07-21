using OncallRota.Models;
namespace OncallRota.ViewModels
{
    public class ShiftSwapViewModel
    {
        public OnCallSchedule?        MySchedule     { get; set; }
        public List<Employee>         TeamColleagues  { get; set; } = new();
        public List<ShiftSwapRequest> MyRequests     { get; set; } = new();
        public string?                Reason         { get; set; }
        public int SwapWithEmployeeId  { get; set; }
    }
}
