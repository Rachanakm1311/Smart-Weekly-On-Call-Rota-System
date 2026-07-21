using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("Teams")]
    public class Team
    {
        [Key] public int TeamId { get; set; }
        [Required, MaxLength(100)] public string  TeamName    { get; set; } = "";
        [MaxLength(100)]           public string? ManagerName { get; set; }
        [MaxLength(150), EmailAddress] public string? TeamEmailId { get; set; }
        [MaxLength(20)]            public string  Status      { get; set; } = "Active";
        public ICollection<Employee>       Employees       { get; set; } = new List<Employee>();
        public ICollection<Application>    Applications    { get; set; } = new List<Application>();
        public ICollection<RotationQueue>  RotationQueues  { get; set; } = new List<RotationQueue>();
        public ICollection<OnCallSchedule> OnCallSchedules { get; set; } = new List<OnCallSchedule>();
    }
}