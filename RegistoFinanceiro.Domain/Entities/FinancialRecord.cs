using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Domain.Entities
{
    public class FinancialRecord
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid PaymentMethodId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = null!; 
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset MovementDate { get; set; }
        public Guid CategoryId { get; set; }
        public MovementCategory Category { get; set; } = null!;
        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public Guid CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }
        public Guid? ApprovedById { get; set; }
        public ApplicationUser? ApprovedBy { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public string Status { get; set; } = "draft";
        public Guid? ParentRecordId { get; set; }
        public FinancialRecord? ParentRecord { get; set; }
        public string InternalNotes { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public ICollection<RecordAttachment> Attachments { get; set; } = new List<RecordAttachment>();
        public ICollection<RecordAuditLog> AuditLogs { get; set; } = new List<RecordAuditLog>();
    }
}