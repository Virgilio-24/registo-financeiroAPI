using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.Configuration
{
    public class SeedOptions
    {
        public const string SectionName = "Seed";
        public string AdminEmail { get; set; } = string.Empty;  
        public string AdminPassword { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = "Administrador";
    }
}