using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("Applications")]
    public class Application
    {
        [Key] public int ApplicationId { get; set; }
        [Required, MaxLength(100)] public string  ApplicationName { get; set; } = "";
        public int TeamId { get; set; }
        [MaxLength(20)] public string Status { get; set; } = "Active";
        [ForeignKey(nameof(TeamId))] public Team? Team { get; set; }
    }
}