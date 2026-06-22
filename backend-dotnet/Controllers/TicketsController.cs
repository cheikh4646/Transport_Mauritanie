using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendDotnet.Data;
using BackendDotnet.Services;
using System.Text;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly QrCodeService _qrCodeService;
        private readonly AppDbContext _context;

        public TicketsController(QrCodeService qrCodeService, AppDbContext context)
        {
            _qrCodeService = qrCodeService;
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet("qr/{reservationId}")]
        public async Task<IActionResult> GetQrCode(int reservationId)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.ReservationId == reservationId);
            if (ticket == null) return NotFound("Ticket non trouvé.");

            var qrData = ticket.QrCode;
            using var generator = new QRCoder.QRCodeGenerator();
            var qr = generator.CreateQrCode(qrData, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var svg = new QRCoder.SvgQRCode(qr);
            var svgContent = svg.GetGraphic(4);

            return Content(svgContent, "image/svg+xml", Encoding.UTF8);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateTicket([FromBody] GenerateTicketRequest request)
        {
            var qrPngPath = await _qrCodeService.SaveQrCodePngAsync(request.QrCode, $"qr_{request.ReservationId}.png");
            var qrBase64 = _qrCodeService.GenerateQrCodeBase64(request.QrCode);

            var ticketHtml = GenerateTicketHtml(request, qrBase64);
            var pdfPath = await _qrCodeService.SaveTicketPdfAsync(ticketHtml, $"ticket_{request.ReservationId}.html");

            return Ok(new
            {
                qrCodePath = qrPngPath,
                pdfPath = pdfPath,
                qrCodeBase64 = qrBase64
            });
        }

        private static string GenerateTicketHtml(GenerateTicketRequest request, string qrBase64)
        {
            return $@"<html><head><meta charset='utf-8'><title>Billet de Transport</title>
<style>
body {{ font-family: 'Segoe UI', sans-serif; margin: 0; padding: 20px; background: #0b0f19; color: #f3f4f6; }}
.ticket {{ max-width: 500px; margin: auto; background: #131a2e; border-radius: 16px; overflow: hidden; border: 1px solid rgba(255,255,255,0.08); }}
.header {{ background: linear-gradient(135deg, #059669, #047857); padding: 24px; text-align: center; }}
.header h1 {{ margin: 0; font-size: 20px; letter-spacing: 2px; }}
.header p {{ margin: 4px 0 0; font-size: 11px; opacity: 0.8; }}
.body {{ padding: 24px; }}
.qr {{ text-align: center; margin: 16px 0; }}
.qr img {{ width: 150px; height: 150px; background: white; padding: 8px; border-radius: 8px; }}
.info {{ display: grid; grid-template-columns: 1fr 1fr; gap: 12px; font-size: 14px; margin: 16px 0; }}
.label {{ color: #9ca3af; font-size: 12px; }}
.value {{ color: white; font-weight: bold; }}
.route {{ text-align: center; padding: 16px 0; border-top: 1px solid rgba(255,255,255,0.08); }}
.route h2 {{ margin: 0; font-size: 24px; color: white; }}
.route span {{ color: #9ca3af; font-size: 12px; }}
.footer {{ text-align: center; padding: 16px; font-size: 10px; color: #6b7280; border-top: 1px solid rgba(255,255,255,0.08); }}
</style></head><body>
<div class='ticket'>
<div class='header'><h1>BILLET DE TRANSPORT</h1><p>{request.CompanyName}</p></div>
<div class='body'>
<div class='info'>
<div><div class='label'>Passager</div><div class='value'>{request.PassengerName}</div></div>
<div><div class='label'>Référence</div><div class='value'>#{request.ReservationId}</div></div>
<div><div class='label'>Siège</div><div class='value'>N° {request.SeatNumber}</div></div>
<div><div class='label'>Prix</div><div class='value'>{request.Price} MRU</div></div>
</div>
<div class='route'>
<h2>{request.DepartureCity} → {request.ArrivalCity}</h2>
<span>{request.DepartureDate:yyyy-MM-dd} à {request.DepartureTime}</span>
</div>
<div class='qr'><img src='data:image/svg+xml;base64,{qrBase64}' alt='QR Code' /></div>
</div>
<div class='footer'>RIM Transport - Plateforme de Gestion du Transport Interurbain en Mauritanie</div>
</div></body></html>";
        }
    }

    public class GenerateTicketRequest
    {
        public int ReservationId { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string DepartureCity { get; set; } = string.Empty;
        public string ArrivalCity { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public string DepartureTime { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public decimal Price { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }
}
