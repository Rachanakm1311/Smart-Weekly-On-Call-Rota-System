using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("HolidayCalendar")]
    public class HolidayCalendar
    {
        [Key] public int HolidayId { get; set; }
        [Required, MaxLength(100)] public string  HolidayName { get; set; } = "";
        [Required]                 public DateTime HolidayDate { get; set; }
        [MaxLength(50)]            public string? Location    { get; set; }
        [MaxLength(200)]           public string? Description { get; set; }
        [MaxLength(20)]            public string  Status      { get; set; } = "Active";
    }
}