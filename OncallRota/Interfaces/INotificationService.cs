using OncallRota.Models;
namespace OncallRota.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(int employeeId, string type, string message);
        Task MarkReadAsync(int notificationId);
    }
}
