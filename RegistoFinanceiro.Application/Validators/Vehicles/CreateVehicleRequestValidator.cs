using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using RegistoFinanceiro.Application.DTOs.Vehicles;

namespace RegistoFinanceiro.Application.Validators.Vehicles
{
    public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
    {
            public CreateVehicleRequestValidator()
        {
            RuleFor(x => x.Plate).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Brand).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Model).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Year).InclusiveBetween(1950, DateTime.UtcNow.Year + 1).When(x => x.Year.HasValue);
        }
    }

    public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
    {
        public UpdateVehicleRequestValidator()
        {
            RuleFor(x => x.Plate).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Brand).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Model).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Status).Must(s => new[] { "active", "inactive", "maintenance" }.Contains(s))
                .WithMessage("Status deve ser 'active', 'inactive' ou 'maintenance'.");
        }
    }                                                                               
}