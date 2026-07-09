namespace Runtira.Application.Abstractions
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default);
    }
}
