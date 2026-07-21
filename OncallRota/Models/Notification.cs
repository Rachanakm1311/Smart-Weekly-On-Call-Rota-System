using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key] public int NotificationId { get; set; }
        public int EmployeeId { get; set; }
        [MaxLength(50)]   public string?  NotificationType { get; set; }
        [MaxLength(500)]  public string   Message          { get; set; } = "";
        public DateTime?  SentDate { get; set; }
        [MaxLength(20)]   public string   Status           { get; set; } = "Unread";

        [ForeignKey(nameof(EmployeeId))] public Employee? Employee { get; set; }
    }
}