using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.Services
{
    public interface IEmailSender
    {
        Task SendPasswordResetAsync(string toEmail, string resetToken, CancellationToken ct);
    }
}