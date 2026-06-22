using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(ILogger<NotificationsController> logger)
        {
            _logger = logger;
        }

        [HttpPost("send")]
        public IActionResult SendNotification([FromBody] NotificationRequest request)
        {
            _logger.LogInformation("Notification envoyée - Type: {Type}, Email: {Email}, Phone: {Phone}, Message: {Message}",
                request.Type, request.Email, request.Phone, request.Message);

            return Ok(new
            {
                Message = "Notification envoyée avec succès.",
                Type = request.Type,
                Recipient = request.Email ?? request.Phone,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public class NotificationRequest
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "email";
    }
}
