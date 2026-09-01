using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Domain.Entities
{
    public class RecordAttachment
    {
        public Guid Id { get; set; }
        public Guid RecordId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public Guid? UploadedById { get; set; }
        public ApplicationUser? UploadedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public FinancialRecord FinancialRecord { get; set; } = null!;
    }
}