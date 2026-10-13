using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.App.Models
{
    public class MaintenanceRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }

        public DateTime ServiceDate { get; set; } = DateTime.UtcNow;

        public int Mileage { get; set; }

        [Required]
        [MaxLength(100)]
        public string MaintenanceType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Workshop { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public int? NextServiceMileage { get; set; }

        public DateTime? NextServiceDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation property
        public virtual Vehicle Vehicle { get; set; } = null!;
    }
}