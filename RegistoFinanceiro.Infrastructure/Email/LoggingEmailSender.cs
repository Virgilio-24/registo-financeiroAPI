using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RegistoFinanceiro.Application.Services;
using System.Threading;
namespace RegistoFinanceiro.Infrastructure.Email
{
    public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendPasswordResetAsync(string toEmail, string resetToken, CancellationToken ct)
    {
        _logger.LogWarning(
            "EMAIL NÃO ENVIADO (sem provedor configurado). Destinatário: {Email}, Token: {Token}",
            toEmail, resetToken);
        return Task.CompletedTask;
    }
}
}