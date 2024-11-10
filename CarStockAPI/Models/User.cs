using System.ComponentModel.DataAnnotations;

namespace CarStockAPI.Models
{
    /// <summary>
    /// Represents a user entity associated with a dealer in the CarStockAPI.
    /// </summary>
    public class User
    {
        [Required]
        [Range(1000, 9999, ErrorMessage = "DealerId must be a four-digit number.")]
        public int DealerId { get; set; }
        [Required]
        public required string HashedPassword { get; set; }
    }
}