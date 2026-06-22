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
using BackendDotnet.Services;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly QrCodeService _qrCodeService;

        public ReservationsController(AppDbContext context, QrCodeService qrCodeService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
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
                return StatusCode(403, "Vous n'êtes pas autorisé à voir cette réservation.");
            }

            if (userRole == "COMPANY")
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.ManagerId == userId);
                if (company == null || r.Trip?.Bus?.CompanyId != company.Id)
                {
                    return StatusCode(403, "Vous n'êtes pas autorisé à voir cette réservation.");
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
            if (reservation.UserId != userId) return StatusCode(403, "Vous n'êtes pas le propriétaire de cette réservation.");

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

            // Génération locale du billet avec QR code
            var qrCode = $"TICKET-{id}-TRIP{reservation.TripId}-SEAT{reservation.SeatNumber}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            var ticketHtml = GenerateTicketHtml(
                id,
                qrCode,
                reservation.User?.Name ?? "Passager",
                reservation.Trip?.DepartureCity ?? "",
                reservation.Trip?.ArrivalCity ?? "",
                reservation.Trip?.DepartureDate,
                reservation.Trip?.DepartureTime.ToString() ?? "",
                reservation.SeatNumber,
                reservation.Trip?.Price ?? 0,
                reservation.Trip?.Bus?.Company?.Name ?? ""
            );

            var pdfPath = await _qrCodeService.SaveTicketPdfAsync(ticketHtml, $"ticket_{id}.html");

            var ticket = new Ticket
            {
                ReservationId = id,
                QrCode = qrCode,
                PdfPath = pdfPath,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Paiement réussi et ticket généré.", Ticket = ticket });
        }

        // DELETE: api/reservations/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null) return NotFound("Réservation non trouvée.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "TRAVELER" && reservation.UserId != userId)
                return StatusCode(403, "Vous n'êtes pas autorisé à annuler cette réservation.");

            if (reservation.Status == "PAID" || reservation.Status == "PENDING")
            {
                reservation.Status = "CANCELLED";
                reservation.UpdatedAt = DateTime.UtcNow;
                _context.Entry(reservation).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Réservation annulée avec succès." });
            }

            return BadRequest("Impossible d'annuler une réservation avec ce statut.");
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
                    return StatusCode(403, "Vous n'êtes pas autorisé à valider des billets pour une autre compagnie.");
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

        private static string GenerateTicketHtml(int reservationId, string qrCode, string passengerName,
            string departureCity, string arrivalCity, DateTime? departureDate, string departureTime,
            int seatNumber, decimal price, string companyName)
        {
            var date = departureDate?.ToString("yyyy-MM-dd") ?? "";
            return $@"<html><head><meta charset='utf-8'><title>Billet de Transport</title>
<style>
body {{ font-family: 'Segoe UI', sans-serif; margin: 0; padding: 20px; background: #0b0f19; color: #f3f4f6; }}
.ticket {{ max-width: 500px; margin: auto; background: #131a2e; border-radius: 16px; overflow: hidden; border: 1px solid rgba(255,255,255,0.08); }}
.header {{ background: linear-gradient(135deg, #059669, #047857); padding: 24px; text-align: center; }}
.header h1 {{ margin: 0; font-size: 20px; letter-spacing: 2px; }}
.header p {{ margin: 4px 0 0; font-size: 11px; opacity: 0.8; }}
.body {{ padding: 24px; }}
.info {{ display: grid; grid-template-columns: 1fr 1fr; gap: 12px; font-size: 14px; margin: 16px 0; }}
.label {{ color: #9ca3af; font-size: 12px; }}
.value {{ color: white; font-weight: bold; }}
.route {{ text-align: center; padding: 16px 0; border-top: 1px solid rgba(255,255,255,0.08); }}
.route h2 {{ margin: 0; font-size: 24px; color: white; }}
.route span {{ color: #9ca3af; font-size: 12px; }}
.footer {{ text-align: center; padding: 16px; font-size: 10px; color: #6b7280; border-top: 1px solid rgba(255,255,255,0.08); }}
.qr {{ text-align: center; font-family: monospace; font-size: 10px; color: #6b7280; word-break: break-all; }}
</style></head><body>
<div class='ticket'>
<div class='header'><h1>BILLET DE TRANSPORT</h1><p>{companyName}</p></div>
<div class='body'>
<div class='info'>
<div><div class='label'>Passager</div><div class='value'>{passengerName}</div></div>
<div><div class='label'>Référence</div><div class='value'>#{reservationId}</div></div>
<div><div class='label'>Siège</div><div class='value'>N° {seatNumber}</div></div>
<div><div class='label'>Prix</div><div class='value'>{price} MRU</div></div>
</div>
<div class='route'>
<h2>{departureCity} → {arrivalCity}</h2>
<span>{date} à {departureTime}</span>
</div>
<div class='qr'><p>Code: {qrCode}</p></div>
</div>
<div class='footer'>RIM Transport - Plateforme de Gestion du Transport Interurbain en Mauritanie</div>
</div></body></html>";
        }
    }
}
