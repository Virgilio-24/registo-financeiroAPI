using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.Configuration
{
    public class CorsOptions
    {
        public const string SectionName = "Cors";

        public string AllowedOrigin { get; set; } = string.Empty;
    }
}