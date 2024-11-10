using System.ComponentModel.DataAnnotations;
namespace CarStockAPI.Models
{
    /// <summary>
    /// Represents a car entity within the CarStockAPI.
    /// </summary>
    public class Car
    {   
        [Required]     
        public int Id { get; set; }
        [Required(ErrorMessage = "Make is required.")]
        [StringLength(50, ErrorMessage = "Make can't be longer than 50 characters.")]
        public required string Make { get; set; }
        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, ErrorMessage = "Model can't be longer than 50 characters.")]
        public required string Model { get; set; }
        [Required]
        [Range(1900, 2024, ErrorMessage = "Year must be between 1900 and 2024.")]
        public int Year { get; set; }
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock level must be a positive number.")]
        public int StockLevel { get; set; }
        [Required(ErrorMessage = "DealerId is required.")]
        [Range(1000, 9999, ErrorMessage = "DealerId must be a four-digit number.")]
        public int DealerId { get; set; }
    }
}