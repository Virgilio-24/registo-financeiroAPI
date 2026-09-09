using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.DTOs.FinancialRecords
{
    public record FinancialRecordResponse(
        Guid Id, string Reference, string Type, decimal Amount,
        Guid PaymentMethodId, string PaymentMethodName,
        string Reason, string? Description, DateTimeOffset MovementDate,
        Guid CategoryId, string CategoryName,
        Guid? VehicleId, string? VehiclePlate,
        Guid CreatedById, string CreatedByName,
        Guid? ApprovedById, DateTimeOffset? ApprovedAt,
        string Status, Guid? ParentRecordId, string? InternalNotes,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

    public record CreateFinancialRecordRequest(
        string Type, decimal Amount, Guid PaymentMethodId, string Reason,
        string? Description, DateTimeOffset MovementDate, Guid CategoryId,
        Guid? VehicleId, string? InternalNotes);

    public record UpdateFinancialRecordRequest(
        string Type, decimal Amount, Guid PaymentMethodId, string Reason,
        string? Description, DateTimeOffset MovementDate, Guid CategoryId,
        Guid? VehicleId, string? InternalNotes);

    public record FinancialRecordFilter(
        string? Type, string? Status, Guid? CategoryId, Guid? VehicleId,
        Guid? CreatedById, DateTimeOffset? FromDate, DateTimeOffset? ToDate);
}