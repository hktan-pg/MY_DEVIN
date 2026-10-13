using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.App.Models
{
    public class Reminder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime ReminderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string ReminderType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? RelatedEntity { get; set; }

        public int? RelatedId { get; set; }

        public bool IsCompleted { get; set; } = false;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}