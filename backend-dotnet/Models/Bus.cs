using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendDotnet.Models
{
    [Table("buses")]
    public class Bus
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("bus_number")]
        public string BusNumber { get; set; } = string.Empty;

        [Required]
        [Range(1, 100)]
        [Column("capacity")]
        public int Capacity { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
