using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("RotationQueue")]
    public class RotationQueue
    {
        [Key] public int QueueId       { get; set; }
        public int       TeamId        { get; set; }
        public int       EmployeeId    { get; set; }
        public int       QueuePosition { get; set; }
        public bool?     IsActive      { get; set; } = true;

        [ForeignKey(nameof(TeamId))]     public Team?     Team     { get; set; }
        [ForeignKey(nameof(EmployeeId))] public Employee? Employee { get; set; }
    }
}