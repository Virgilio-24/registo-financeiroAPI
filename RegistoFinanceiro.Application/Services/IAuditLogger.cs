using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.Services
{
    public interface IAuditLogger
    {
        Task LogAsync(Guid recordId, string action, Guid performedBy, object? beforeData, object? afterData, CancellationToken ct);
    }
}