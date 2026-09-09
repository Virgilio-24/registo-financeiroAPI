using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RegistoFinanceiro.Application.Services;
using RegistoFinanceiro.Domain.Entities;
using RegistoFinanceiro.Infrastructure.Persistence;

namespace RegistoFinanceiro.Infrastructure.FinancialRecords
{
    public class AuditLogger : IAuditLogger
    {
        private readonly RegistoFinanceiroDbContext _dbContext;

        public AuditLogger(RegistoFinanceiroDbContext dbContext) => _dbContext = dbContext;

        public async Task LogAsync(Guid recordId, string action, Guid performedBy,
            object? beforeData, object? afterData, CancellationToken ct)
        {
            var entry = new RecordAuditLog
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Action = action,
                PerformedById = performedBy,
                BeforeData = ToJsonDocument(beforeData),
                AfterData = ToJsonDocument(afterData),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _dbContext.RecordAuditLogs.Add(entry);
            await _dbContext.SaveChangesAsync(ct);
        }
        private static JsonDocument? ToJsonDocument(object? value)
        {
            return value is null
                ? null
                : JsonDocument.Parse(JsonSerializer.Serialize(value));
        }
    }
}