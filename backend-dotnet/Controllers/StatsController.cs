using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendDotnet.Data;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/stats")]
    [Authorize]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            var totalRevenue = await _context.Payments.SumAsync(p => p.Amount);
            var totalReservations = await _context.Reservations.CountAsync();
            var activeCompanies = await _context.Companies.CountAsync(c => c.IsActive);
            var totalBuses = await _context.Buses.CountAsync();

            var allPayments = await _context.Payments.ToListAsync();
            var revenueByMonth = allPayments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Month)
                .ToList();

            var paidReservations = await _context.Reservations
                .Where(r => r.Status == "PAID")
                .Include(r => r.Trip)
                .ToListAsync();
            var popularRoutes = paidReservations
                .Where(r => r.Trip != null)
                .GroupBy(r => new { r.Trip!.DepartureCity, r.Trip.ArrivalCity })
                .Select(g => new
                {
                    Route = $"{g.Key.DepartureCity} → {g.Key.ArrivalCity}",
                    Bookings = g.Count()
                })
                .OrderByDescending(x => x.Bookings)
                .Take(4)
                .ToList();

            var allReservations = await _context.Reservations.ToListAsync();
            var reservationsByStatus = allReservations
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            return Ok(new
            {
                totalRevenue,
                totalReservations,
                activeCompanies,
                totalBuses,
                revenueByMonth,
                popularRoutes,
                reservationsByStatus
            });
        }

        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<object>> GetCompanyStats(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) return NotFound("Compagnie non trouvée.");

            var busIds = await _context.Buses
                .Where(b => b.CompanyId == companyId)
                .Select(b => b.Id)
                .ToListAsync();

            var tripIds = await _context.Trips
                .Where(t => busIds.Contains(t.BusId))
                .Select(t => t.Id)
                .ToListAsync();

            var totalSales = await _context.Payments
                .Where(p => tripIds.Contains(p.Reservation!.TripId))
                .SumAsync(p => p.Amount);

            var ticketsSold = await _context.Tickets
                .Where(t => tripIds.Contains(t.Reservation!.TripId))
                .CountAsync();

            var busCount = busIds.Count;

            var recentReservations = await _context.Reservations
                .Where(r => tripIds.Contains(r.TripId))
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();
            var recentTrips = await _context.Trips
                .Where(t => tripIds.Contains(t.Id))
                .ToListAsync();
            var recentActivity = recentReservations.Select(r =>
            {
                var trip = recentTrips.FirstOrDefault(t => t.Id == r.TripId);
                return new
                {
                    r.Id,
                    r.Status,
                    r.SeatNumber,
                    r.CreatedAt,
                    Trip = trip != null ? trip.DepartureCity + " → " + trip.ArrivalCity : ""
                };
            }).ToList();

            return Ok(new
            {
                companyName = company.Name,
                totalSales,
                ticketsSold,
                busCount,
                recentActivity
            });
        }
    }
}
