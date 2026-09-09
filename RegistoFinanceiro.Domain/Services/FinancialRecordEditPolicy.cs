using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RegistoFinanceiro.Domain.Entities;

namespace RegistoFinanceiro.Domain.Services
{
    public class FinancialRecordEditPolicy
    {
        public static bool OnlyDescriptionChanged(
        FinancialRecord current,
        string type, decimal amount, Guid paymentMethodId, string reason,
        DateTimeOffset movementDate, Guid categoryId, Guid? vehicleId, string? internalNotes)
        {
            return type == current.Type
                && amount == current.Amount
                && paymentMethodId == current.PaymentMethodId
                && reason == current.Reason
                && movementDate == current.MovementDate
                && categoryId == current.CategoryId
                && vehicleId == current.VehicleId
                && internalNotes == current.InternalNotes;
        }
    }
}