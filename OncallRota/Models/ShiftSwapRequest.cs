using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("ShiftSwapRequests")]
    public class ShiftSwapRequest
    {
        [Key] public int SwapId { get; set; }
        public int      ScheduleId             { get; set; }
        public int      RequestedByEmployeeId  { get; set; }
        public int      SwapWithEmployeeId     { get; set; }
        [MaxLength(250)] public string?  Reason       { get; set; }
        public DateTime? RequestDate   { get; set; }
        [MaxLength(20)]  public string   Status       { get; set; } = "Pending";
        [MaxLength(100)] public string?  ApprovedBy   { get; set; }   // manager name (varchar)
        public DateTime? ApprovedDate  { get; set; }

        [ForeignKey(nameof(ScheduleId))]            public OnCallSchedule? Schedule              { get; set; }
        [ForeignKey(nameof(RequestedByEmployeeId))] public Employee?       RequestedByEmployee   { get; set; }
        [ForeignKey(nameof(SwapWithEmployeeId))]    public Employee?       SwapWithEmployee      { get; set; }
    }
}