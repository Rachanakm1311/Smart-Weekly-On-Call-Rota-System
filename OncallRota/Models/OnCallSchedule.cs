using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("OnCallSchedule")]
    public class OnCallSchedule
    {
        [Key] public int ScheduleId { get; set; }
        public int       TeamId             { get; set; }
        public DateTime? WeekStartDate      { get; set; }
        public DateTime? WeekEndDate        { get; set; }
        public int?      PrimaryEmployeeId  { get; set; }
        public int?      BackupEmployeeId   { get; set; }
        [MaxLength(20)]  public string Status { get; set; } = "Active";

        [ForeignKey(nameof(TeamId))]            public Team?     Team             { get; set; }
        [ForeignKey(nameof(PrimaryEmployeeId))] public Employee? PrimaryEmployee  { get; set; }
        [ForeignKey(nameof(BackupEmployeeId))]  public Employee? BackupEmployee   { get; set; }
        public ICollection<ShiftSwapRequest> ShiftSwapRequests { get; set; } = new List<ShiftSwapRequest>();
    }
}