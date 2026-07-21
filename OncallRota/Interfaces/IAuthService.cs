using OncallRota.Models;
namespace OncallRota.Interfaces
{
    public interface IAuthService
    {
        Task<Employee?> ValidateLoginAsync(string email, string password);
    }
}
