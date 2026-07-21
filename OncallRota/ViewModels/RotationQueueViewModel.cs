using OncallRota.Models;
namespace OncallRota.ViewModels
{
    public class RotationQueueViewModel
    {
        public int TeamId                      { get; set; }
        public string TeamName                 { get; set; } = "";
        public List<RotationQueue> Queue       { get; set; } = new();
        public List<Employee> TeamEmployees    { get; set; } = new();
        public List<Team> AllTeams             { get; set; } = new();
    }
}
