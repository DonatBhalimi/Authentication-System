using System;
using System.Threading.Tasks;

namespace Services
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }

    public class ConsoleEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("========== EMAIL SIMULATION ==========");
            Console.ResetColor();
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body: {body}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=====================================");
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
