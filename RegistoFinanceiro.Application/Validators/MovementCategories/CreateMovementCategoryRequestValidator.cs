using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using RegistoFinanceiro.Application.DTOs.MovementCategory;
namespace RegistoFinanceiro.Application.Validators.MovementCategory
{
    public class CreateMovementCategoryRequestValidator : AbstractValidator<CreateMovementCategoryRequest>
    {
        public CreateMovementCategoryRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode ter mais de 100 caracteres.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("O tipo é obrigatório.")
                .MaximumLength(50).WithMessage("O tipo não pode ter mais de 50 caracteres.");
        }
    }

    public class UpdateMovementCategoryRequestValidator : AbstractValidator<UpdateMovementCategoryRequest>
    {
        public UpdateMovementCategoryRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode ter mais de 100 caracteres.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("O tipo é obrigatório.")
                .MaximumLength(50).WithMessage("O tipo não pode ter mais de 50 caracteres.");
        }
    }
}