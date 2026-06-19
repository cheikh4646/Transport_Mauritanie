using System;

namespace BackendDotnet.DTOs
{
    public class CreateReservationDto
    {
        public int TripId { get; set; }
        public int SeatNumber { get; set; }
    }

    public class ReservationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string DepartureCity { get; set; } = string.Empty;
        public string ArrivalCity { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public decimal Price { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        public PaymentDto? Payment { get; set; }
        public TicketDto? Ticket { get; set; }
    }

    public class CreatePaymentDto
    {
        public int ReservationId { get; set; }
        public string Method { get; set; } = "BANKILY"; // BANKILY, MASRIFY, BIMIE, CREDIT_CARD
        public decimal Amount { get; set; }
        public string? TransactionReference { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class TicketDto
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public string? PdfPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
