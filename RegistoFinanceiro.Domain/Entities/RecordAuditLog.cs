using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace RegistoFinanceiro.Domain.Entities
{
    public class RecordAuditLog
    {
        public Guid Id { get; set; }
        public Guid RecordId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid? PerformedById { get; set; }
        public ApplicationUser? PerformedBy { get; set; }
        public JsonDocument? BeforeData { get; set; }
        public JsonDocument? AfterData { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public FinancialRecord FinancialRecord { get; set; } = null!;
    }
}