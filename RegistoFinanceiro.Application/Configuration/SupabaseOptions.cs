using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.Configuration
{
    public class SupabaseOptions
    {
         public const string SectionName = "Supabase";

        public string Url { get; set; } = string.Empty;
        public string ServiceKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = "financial-documents";
    }
}