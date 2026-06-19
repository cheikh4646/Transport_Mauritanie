using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendDotnet.Models
{
    [Table("payments")]
    public class Payment
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
        [Column("amount")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("method")]
        public string Method { get; set; } = "BANKILY"; // "BANKILY", "MASRIFY", "BIMIE", "CREDIT_CARD"

        [MaxLength(100)]
        [Column("transaction_reference")]
        public string? TransactionReference { get; set; }

        [Column("payment_date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }
}
