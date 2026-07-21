using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OncallRota.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key] public int RoleId { get; set; }
        [Required, MaxLength(50)]  public string  RoleName    { get; set; } = "";
        [MaxLength(200)]           public string? Description { get; set; }
        [MaxLength(20)]            public string  Status      { get; set; } = "Active";
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}