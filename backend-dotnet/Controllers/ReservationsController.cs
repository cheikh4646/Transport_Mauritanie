using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BackendDotnet.Data;
using BackendDotnet.DTOs;
using BackendDotnet.Models;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public ReservationsController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Trip)
                    .ThenInclude(t => t!.Bus)
                        .ThenInclude(b => b!.Company)
                .AsQueryable();

            if (userRole == "TRAVELER")
            {
                query = query.Where(r => r.UserId == userId);
            }
            else if (userRole == "COMPANY")
            {
                // Trouver la compagnie gérée par cet utilisateur
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.ManagerId == userId);
                if (company == null) return new List<ReservationDto>();

                query = query.Where(r => r.Trip!.Bus!.CompanyId == company.Id);
            }

            var reservations = await query.ToListAsync();
            var result = new List<ReservationDto>();

            foreach (var r in reservations)
            {
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.ReservationId == r.Id);
                var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.ReservationId == r.Id);

                result.Add(MapToDto(r, payment, ticket));
            }

            return result;
        }

        // GET: api/reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetReservation(int id)
        {
            var r = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Trip)
                    .ThenInclude(t => t!.Bus)
                        .ThenInclude(b => b!.Company)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (r == null) return NotFound("Réservation non trouvée.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "TRAVELER" && r.UserId != userId)
            {
                return Forbid("Vous n'êtes pas autorisé à voir cette réservation.");
            }

            if (userRole == "COMPANY")
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.ManagerId == userId);
                if (company == null || r.Trip?.Bus?.CompanyId != company.Id)
                {
                    return Forbid("Vous n'êtes pas autorisé à voir cette réservation.");
                }
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.ReservationId == r.Id);
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.ReservationId == r.Id);

            return MapToDto(r, payment, ticket);
        }

        // POST: api/reservations
        [HttpPost]
        public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trip = await _context.Trips.Include(t => t.Bus).ThenInclude(b => b!.Company).FirstOrDefaultAsync(t => t.Id == dto.TripId);
            if (trip == null) return BadRequest("Trajet non trouvé.");

            if (trip.Status == "CANCELLED") return BadRequest("Ce trajet a été annulé.");

            // Valider le numéro de siège
            var capacity = trip.Bus?.Capacity ?? 0;
            if (dto.SeatNumber < 1 || dto.SeatNumber > capacity)
            {
                return BadRequest($"Numéro de siège invalide. Le bus dispose de {capacity} sièges.");
            }

            // Vérifier si le siège est déjà occupé
            var alreadyBooked = await _context.Reservations.AnyAsync(r => r.TripId == dto.TripId && r.SeatNumber == dto.SeatNumber && r.Status != "CANCELLED");
            if (alreadyBooked)
            {
                return BadRequest("Ce siège est déjà réservé pour ce trajet.");
            }

            var reservation = new Reservation
            {
                UserId = userId,
                TripId = dto.TripId,
                SeatNumber = dto.SeatNumber,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Recharger pour inclure les entités liées
            reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Trip)
                    .ThenInclude(t => t!.Bus)
                        .ThenInclude(b => b!.Company)
                .FirstOrDefaultAsync(r => r.Id == reservation.Id);

            return CreatedAtAction(nameof(GetReservation), new { id = reservation!.Id }, MapToDto(reservation, null, null));
        }

        // POST: api/reservations/5/pay
        [HttpPost("{id}/pay")]
        public async Task<IActionResult> PayReservation(int id, CreatePaymentDto dto)
        {
            if (id != dto.ReservationId) return BadRequest("ID incohérent.");

            var reservation = await _context.Reservations
                .Include(r => r.Trip)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound("Réservation non trouvée.");

            if (reservation.Status == "PAID") return BadRequest("Cette réservation est déjà payée.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (reservation.UserId != userId) return Forbid("Vous n'êtes pas le propriétaire de cette réservation.");

            // Création du paiement (simulation)
            var payment = new Payment
            {
                ReservationId = id,
                Amount = dto.Amount,
                Method = dto.Method.ToUpper(),
                TransactionReference = dto.TransactionReference ?? $"SIM-TX-{new Random().Next(100000, 999999)}",
                PaymentDate = DateTime.UtcNow
            };

            // Mettre à jour le statut de la réservation
            reservation.Status = "PAID";
            reservation.UpdatedAt = DateTime.UtcNow;

            _context.Payments.Add(payment);
            _context.Entry(reservation).State = EntityState.Modified;

            // Essayer d'appeler l'API Laravel pour la génération de billet
            var ticket = new Ticket
            {
                ReservationId = id,
                QrCode = $"TICKET-{id}-TRIP{reservation.TripId}-SEAT{reservation.SeatNumber}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                using var client = new HttpClient();
                var laravelUrl = _config["LaravelUrl"] ?? "http://localhost:8000";
                
                // Appel Laravel asynchrone (fictif/simulé ou réel)
                var response = await client.PostAsJsonAsync($"{laravelUrl}/api/tickets/generate", new {
                    reservation_id = id,
                    qr_code = ticket.QrCode
                });

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadFromJsonAsync<LaravelTicketResponse>();
                    ticket.PdfPath = responseData?.PdfPath;
                }
                else
                {
                    ticket.PdfPath = $"/tickets/ticket_{id}.pdf"; // Fallback local path
                }
            }
            catch
            {
                // Fallback local en cas d'indisponibilité du service Laravel
                ticket.PdfPath = $"/tickets/ticket_{id}.pdf";
            }

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Paiement réussi et ticket généré.", Ticket = ticket });
        }

        // GET: api/reservations/scan/{qrCode} (Pour validation par la compagnie)
        [Authorize(Roles = "ADMIN,COMPANY")]
        [HttpGet("scan/{qrCode}")]
        public async Task<ActionResult<ReservationDto>> ScanTicket(string qrCode)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Reservation)
                    .ThenInclude(r => r!.User)
                .Include(t => t.Reservation)
                    .ThenInclude(r => r!.Trip)
                        .ThenInclude(t => t!.Bus)
                            .ThenInclude(b => b!.Company)
                .FirstOrDefaultAsync(t => t.QrCode == qrCode);

            if (ticket == null) return NotFound("Billet invalide ou introuvable.");

            var reservation = ticket.Reservation;
            if (reservation == null) return NotFound("Réservation associée introuvable.");

            // Vérifier que l'agent connecté appartient à la compagnie du trajet
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "COMPANY")
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.ManagerId == userId);
                if (company == null || reservation.Trip?.Bus?.CompanyId != company.Id)
                {
                    return Forbid("Vous n'êtes pas autorisé à valider des billets pour une autre compagnie.");
                }
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.ReservationId == reservation.Id);

            return MapToDto(reservation, payment, ticket);
        }

        private static ReservationDto MapToDto(Reservation r, Payment? p, Ticket? t)
        {
            return new ReservationDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User?.Name ?? string.Empty,
                TripId = r.TripId,
                DepartureCity = r.Trip?.DepartureCity ?? string.Empty,
                ArrivalCity = r.Trip?.ArrivalCity ?? string.Empty,
                DepartureDate = r.Trip?.DepartureDate ?? DateTime.MinValue,
                DepartureTime = r.Trip?.DepartureTime ?? TimeSpan.Zero,
                Price = r.Trip?.Price ?? 0,
                CompanyName = r.Trip?.Bus?.Company?.Name ?? string.Empty,
                SeatNumber = r.SeatNumber,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                Payment = p == null ? null : new PaymentDto
                {
                    Id = p.Id,
                    ReservationId = p.ReservationId,
                    Amount = p.Amount,
                    Method = p.Method,
                    TransactionReference = p.TransactionReference,
                    PaymentDate = p.PaymentDate
                },
                Ticket = t == null ? null : new TicketDto
                {
                    Id = t.Id,
                    ReservationId = t.ReservationId,
                    QrCode = t.QrCode,
                    PdfPath = t.PdfPath,
                    CreatedAt = t.CreatedAt
                }
            };
        }

        private class LaravelTicketResponse
        {
            public string PdfPath { get; set; } = string.Empty;
        }
    }
}
