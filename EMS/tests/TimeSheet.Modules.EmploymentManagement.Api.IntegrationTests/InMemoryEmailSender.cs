using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Email;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class InMemoryEmailSender : IEmailSender
{
    private readonly List<EmailMessage> _messages = [];

    public IReadOnlyList<EmailMessage> Messages => _messages;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _messages.Clear();
    }
}
