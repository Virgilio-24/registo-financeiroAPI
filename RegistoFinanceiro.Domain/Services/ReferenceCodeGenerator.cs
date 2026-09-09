using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Domain.Services
{
    public static partial class ReferenceCodeGenerator
    {
        public static string NewCandidate(int year, int randomNumber) => $"MOV-{year}-{randomNumber:D6}";

        public static string NewCandidate() =>
            NewCandidate(DateTime.UtcNow.Year, RandomNumberGenerator.GetInt32(0, 1_000_000));

        [GeneratedRegex(@"^MOV-\d{4}-\d{6}$")]
        public static partial Regex Pattern();

        public static bool IsValidFormat(string reference) => Pattern().IsMatch(reference);
    }
}