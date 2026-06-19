using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendDotnet.Models
{
    [Table("tickets")]
    public class Ticket
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("reservation_id")]
        public int ReservationId { get; set; }

        [ForeignKey("ReservationId")]
        public Reservation? Reservation { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("qr_code")]
        public string QrCode { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("pdf_path")]
        public string? PdfPath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
