using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("Employees")]
    public class Employee
    {
        [Key] public int EmployeeId { get; set; }
        [Required, MaxLength(50)]  public string  FirstName       { get; set; } = "";
        [Required, MaxLength(50)]  public string  LastName        { get; set; } = "";
        [Required, MaxLength(100)] public string  EmployeeName    { get; set; } = "";
        [MaxLength(150)]           public string? Email           { get; set; }
        [MaxLength(20)]            public string? Phone           { get; set; }
        [MaxLength(50)]            public string? Region          { get; set; }
        [MaxLength(200)]           public string  Password        { get; set; } = "Password123!";
        /// <summary>Relative path stored as /images/profiles/filename.jpg — null = use generated avatar.</summary>
        [MaxLength(200)]           public string? ProfilePicture  { get; set; }
        public int TeamId { get; set; }
        public int RoleId { get; set; }
        [MaxLength(20)] public string Status { get; set; } = "Active";
        [ForeignKey(nameof(TeamId))] public Team? Team { get; set; }
        [ForeignKey(nameof(RoleId))] public Role? Role { get; set; }
        public ICollection<LeaveRequest>  LeaveRequests  { get; set; } = new List<LeaveRequest>();
        public ICollection<Notification>  Notifications  { get; set; } = new List<Notification>();
        public ICollection<RotationQueue> RotationQueues { get; set; } = new List<RotationQueue>();
    }
}
