using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.App.Models
{
    public class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int AccountId { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? TransferToAccountId { get; set; }

        // Navigation properties
        public virtual Account Account { get; set; } = null!;
        public virtual Category? Category { get; set; }
        public virtual Account? TransferToAccount { get; set; }

        // Display helpers (not mapped to database)
        [NotMapped]
        public string AccountDisplay => TransactionType == "Transfer"
            ? $"{Account?.Name ?? ""} → {TransferToAccount?.Name ?? ""}"
            : Account?.Name ?? "";

        [NotMapped]
        public string AmountDisplay => TransactionType == "Expense"
            ? $"-{Amount:C}"
            : $"{Amount:C}";
    }
}
