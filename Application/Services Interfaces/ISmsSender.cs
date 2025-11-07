using System.Threading.Tasks;

namespace Application.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
