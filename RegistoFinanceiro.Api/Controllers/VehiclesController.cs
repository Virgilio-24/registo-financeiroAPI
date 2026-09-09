using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistoFinanceiro.Application.DTOs.Common;
using RegistoFinanceiro.Application.DTOs.Vehicles;
using RegistoFinanceiro.Domain.Entities;
using RegistoFinanceiro.Infrastructure.Persistence;

namespace RegistoFinanceiro.Api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class VehiclesController : ControllerBase
    {
        private readonly RegistoFinanceiroDbContext _dbContext;
        public VehiclesController(RegistoFinanceiroDbContext dbContext) => _dbContext = dbContext;
        
        [HttpGet]
        public async Task<ActionResult<PagedResult<VehicleResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);

            var query = _dbContext.Vehicles.AsNoTracking().OrderBy(v => v.Plate);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VehicleResponse(v.Id, v.Plate, v.Brand, v.Model, v.Year, v.Status, v.Notes))
                .ToListAsync(ct);

            return Ok(new PagedResult<VehicleResponse>(items, page, pageSize, totalCount));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<VehicleResponse>> GetById(Guid id, CancellationToken ct)
        {
            var vehicle = await _dbContext.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);
            if (vehicle is null)
                return NotFound();

            return Ok(new VehicleResponse(
                vehicle.Id, vehicle.Plate, vehicle.Brand, vehicle.Model,
                vehicle.Year, vehicle.Status, vehicle.Notes));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VehicleResponse>> Create(CreateVehicleRequest request, CancellationToken ct)
        {
            var plateExists = await _dbContext.Vehicles.AnyAsync(v => v.Plate == request.Plate, ct);
            if (plateExists)
                return Conflict(new ProblemDetails { Title = "Já existe uma viatura com esta matrícula." });

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Plate = request.Plate,
                Brand = request.Brand,
                Model = request.Model,
                Year = request.Year,
                Status = "active",
                Notes = request.Notes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            _dbContext.Vehicles.Add(vehicle);
            await _dbContext.SaveChangesAsync(ct);

            var response = new VehicleResponse(
                vehicle.Id, vehicle.Plate, vehicle.Brand, vehicle.Model,
                vehicle.Year, vehicle.Status, vehicle.Notes);

            return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, response);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateVehicleRequest request, CancellationToken ct)
        {
            var vehicle = await _dbContext.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (vehicle is null)
                return NotFound();

            var plateTakenByOther = await _dbContext.Vehicles
                .AnyAsync(v => v.Plate == request.Plate && v.Id != id, ct);
            if (plateTakenByOther)
                return Conflict(new ProblemDetails { Title = "Já existe outra viatura com esta matrícula." });

            vehicle.Plate = request.Plate;
            vehicle.Brand = request.Brand;
            vehicle.Model = request.Model;
            vehicle.Year = request.Year;
            vehicle.Status = request.Status;
            vehicle.Notes = request.Notes;
            vehicle.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}