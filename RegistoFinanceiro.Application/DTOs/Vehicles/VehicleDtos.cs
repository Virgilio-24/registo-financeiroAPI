using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.DTOs.Vehicles
{
    public record VehicleResponse(Guid Id, string Plate, string Brand, string Model, int? Year, string Status, string? Notes);
    public record CreateVehicleRequest(string Plate, string Brand, string Model, int? Year, string? Notes);
    public record UpdateVehicleRequest(string Plate, string Brand, string Model, int? Year, string Status, string? Notes);
}