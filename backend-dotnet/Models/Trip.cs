using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendDotnet.Models
{
    [Table("trips")]
    public class Trip
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("bus_id")]
        public int BusId { get; set; }

        [ForeignKey("BusId")]
        public Bus? Bus { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("departure_city")]
        public string DepartureCity { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("arrival_city")]
        public string ArrivalCity { get; set; } = string.Empty;

        [Required]
        [Column("departure_date")]
        public DateTime DepartureDate { get; set; }

        [Required]
        [Column("departure_time")]
        public TimeSpan DepartureTime { get; set; }

        [Required]
        [Column("price")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "SCHEDULED"; // "SCHEDULED", "ON_GOING", "ARRIVED", "CANCELLED"

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
