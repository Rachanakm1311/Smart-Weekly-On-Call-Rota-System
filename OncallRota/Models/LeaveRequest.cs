using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("LeaveRequests")]
    public class LeaveRequest
    {
        [Key] public int LeaveId { get; set; }
        public int EmployeeId { get; set; }
        [MaxLength(30)]  public string?  LeaveType    { get; set; } = "Annual";
        public DateTime? StartDate   { get; set; }
        public DateTime? EndDate     { get; set; }
        [MaxLength(250)] public string?  Reason       { get; set; }
        [MaxLength(20)]  public string   Status       { get; set; } = "Pending";
        [MaxLength(100)] public string?  ApprovedBy   { get; set; }   // manager name (varchar)
        public DateTime? ApprovedDate { get; set; }
        /// <summary>True when the employee has opted to work through their regional holiday.</summary>
        public bool      WorkOnHoliday { get; set; } = false;
        /// <summary>The date the employee will take as comp-off in lieu of the worked holiday.</summary>
        public DateTime? CompOffDate   { get; set; }

        [ForeignKey(nameof(EmployeeId))] public Employee? Employee { get; set; }

        [NotMapped]
        public int DurationDays => (StartDate.HasValue && EndDate.HasValue)
            ? (int)(EndDate.Value - StartDate.Value).TotalDays + 1 : 0;
    }
}