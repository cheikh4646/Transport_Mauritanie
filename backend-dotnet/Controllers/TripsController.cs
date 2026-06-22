using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendDotnet.Data;
using BackendDotnet.DTOs;
using BackendDotnet.Models;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TripsController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetUserCompanyIdAsync()
        {
            if (User.Identity?.IsAuthenticated == false) return null;
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole == "ADMIN") return null;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return null;

            var userId = int.Parse(userIdClaim);
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.ManagerId == userId);
            return company?.Id;
        }

        // GET: api/trips (Public : Recherche et filtrage)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips(
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] DateTime? date = null)
        {
            var query = _context.Trips
                .Include(t => t.Bus)
                .ThenInclude(b => b!.Company)
                .AsQueryable();

            // Filtrage optionnel
            if (!string.IsNullOrEmpty(from))
            {
                query = query.Where(t => t.DepartureCity.ToLower() == from.ToLower());
            }

            if (!string.IsNullOrEmpty(to))
            {
                query = query.Where(t => t.ArrivalCity.ToLower() == to.ToLower());
            }

            if (date.HasValue)
            {
                var searchDate = date.Value.Date;
                query = query.Where(t => t.DepartureDate.Date == searchDate);
            }

            var trips = await query.ToListAsync();

            var result = new List<TripDto>();

            foreach (var trip in trips)
            {
                var bookedSeats = await _context.Reservations
                    .Where(r => r.TripId == trip.Id && r.Status != "CANCELLED")
                    .Select(r => r.SeatNumber)
                    .ToListAsync();

                result.Add(new TripDto
                {
                    Id = trip.Id,
                    BusId = trip.BusId,
                    BusNumber = trip.Bus?.BusNumber ?? string.Empty,
                    BusCapacity = trip.Bus?.Capacity ?? 0,
                    CompanyName = trip.Bus?.Company?.Name ?? string.Empty,
                    DepartureCity = trip.DepartureCity,
                    ArrivalCity = trip.ArrivalCity,
                    DepartureDate = trip.DepartureDate,
                    DepartureTime = trip.DepartureTime,
                    Price = trip.Price,
                    Status = trip.Status,
                    BookedSeats = bookedSeats
                });
            }

            return result;
        }

        // GET: api/trips/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TripDto>> GetTrip(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Bus)
                .ThenInclude(b => b!.Company)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null)
            {
                return NotFound("Trajet non trouvé.");
            }

            var bookedSeats = await _context.Reservations
                .Where(r => r.TripId == trip.Id && r.Status != "CANCELLED")
                .Select(r => r.SeatNumber)
                .ToListAsync();

            return new TripDto
            {
                Id = trip.Id,
                BusId = trip.BusId,
                BusNumber = trip.Bus?.BusNumber ?? string.Empty,
                BusCapacity = trip.Bus?.Capacity ?? 0,
                CompanyName = trip.Bus?.Company?.Name ?? string.Empty,
                DepartureCity = trip.DepartureCity,
                ArrivalCity = trip.ArrivalCity,
                DepartureDate = trip.DepartureDate,
                DepartureTime = trip.DepartureTime,
                Price = trip.Price,
                Status = trip.Status,
                BookedSeats = bookedSeats
            };
        }

        // POST: api/trips (Admin ou Compagnie)
        [Authorize(Roles = "ADMIN,COMPANY")]
        [HttpPost]
        public async Task<ActionResult<Trip>> PostTrip(CreateTripDto dto)
        {
            // Vérifier que le bus appartient bien à la compagnie de l'utilisateur connecté
            var bus = await _context.Buses.FindAsync(dto.BusId);
            if (bus == null)
            {
                return BadRequest("Bus non trouvé.");
            }

            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue && bus.CompanyId != companyId.Value)
            {
                return StatusCode(403, "Vous n'êtes pas autorisé à utiliser ce bus pour un trajet.");
            }

            var trip = new Trip
            {
                BusId = dto.BusId,
                DepartureCity = dto.DepartureCity,
                ArrivalCity = dto.ArrivalCity,
                DepartureDate = dto.DepartureDate,
                DepartureTime = dto.DepartureTime,
                Price = dto.Price,
                Status = "SCHEDULED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, trip);
        }

        // PUT: api/trips/5
        [Authorize(Roles = "ADMIN,COMPANY")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTrip(int id, CreateTripDto dto)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound("Trajet non trouvé.");
            }

            var bus = await _context.Buses.FindAsync(dto.BusId);
            if (bus == null)
            {
                return BadRequest("Bus non trouvé.");
            }

            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue && (bus.CompanyId != companyId.Value || trip.Bus?.CompanyId != companyId.Value))
            {
                return StatusCode(403, "Vous n'êtes pas autorisé à modifier ce trajet.");
            }

            trip.BusId = dto.BusId;
            trip.DepartureCity = dto.DepartureCity;
            trip.ArrivalCity = dto.ArrivalCity;
            trip.DepartureDate = dto.DepartureDate;
            trip.DepartureTime = dto.DepartureTime;
            trip.Price = dto.Price;
            trip.UpdatedAt = DateTime.UtcNow;

            _context.Entry(trip).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/trips/5/status (Changer le statut du trajet : SCHEDULED, ON_GOING, ARRIVED, CANCELLED)
        [Authorize(Roles = "ADMIN,COMPANY")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var trip = await _context.Trips.Include(t => t.Bus).FirstOrDefaultAsync(t => t.Id == id);
            if (trip == null)
            {
                return NotFound("Trajet non trouvé.");
            }

            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue && trip.Bus?.CompanyId != companyId.Value)
            {
                return StatusCode(403, "Vous n'êtes pas autorisé à modifier le statut de ce trajet.");
            }

            var validStatuses = new[] { "SCHEDULED", "ON_GOING", "ARRIVED", "CANCELLED" };
            if (!validStatuses.Contains(status.ToUpper()))
            {
                return BadRequest("Statut invalide.");
            }

            trip.Status = status.ToUpper();
            trip.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/trips/5
        [Authorize(Roles = "ADMIN,COMPANY")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            var trip = await _context.Trips.Include(t => t.Bus).FirstOrDefaultAsync(t => t.Id == id);
            if (trip == null)
            {
                return NotFound("Trajet non trouvé.");
            }

            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue && trip.Bus?.CompanyId != companyId.Value)
            {
                return StatusCode(403, "Vous n'êtes pas autorisé à supprimer ce trajet.");
            }

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
