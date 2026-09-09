using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.DTOs.MovementCategory
{
    public record CreateMovementCategoryRequest(string Name, string Type);
    public record UpdateMovementCategoryRequest(string Name, string Type, bool Active);
    public record MovementCategoryResponse(Guid Id, string Name, string Type, bool Active);
}