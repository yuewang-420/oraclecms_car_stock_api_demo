using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CarStockAPI.Data;
using CarStockAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CarStockAPI.Controllers
{
    /// <summary>
    /// Controller responsible for handling operations related to cars.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CarsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CarsController"/> class.
        /// </summary>
        /// <param name="configuration">Configuration instance for accessing application settings.</param>
        /// <param name="context">Database context for data access.</param>
        public CarsController (IConfiguration configuration, DatabaseContext context) 
        {
            this._configuration = configuration;
            this._context = context;
        }

        /// <summary>
        /// Extracts and validates the JWT from the request cookies and retrieves the dealer ID claim.
        /// </summary>
        /// <returns>The dealer ID extracted from the token.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the JWT is missing, invalid, or the dealer ID claim is not found.</exception>
        private int GetDealerIdFromToken()
        {
            // Attempt to retrieve the JWT from the cookies
            if (!Request.Cookies.TryGetValue("jwt", out var token) || string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("JWT not found in cookies.");
            }

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT signing key is not configured. Please check appsettings.json.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            try
            {
                // Validate the JWT and retrieve the claims principal
                var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero  // Ensures no leeway for token expiration
                }, out var validatedToken);

                // Extract the dealer ID claim
                var dealerIdClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "dealerId");
                if (dealerIdClaim == null)
                {
                    throw new UnauthorizedAccessException("DealerId not found in token.");
                }

                return int.Parse(dealerIdClaim.Value);
            }
            catch (SecurityTokenException)
            {
                throw new UnauthorizedAccessException("Invalid JWT token.");
            }
        }

        /// <summary>
        /// Adds a new car associated with the authenticated dealer.
        /// </summary>
        /// <param name="carDto">The car data to be added.</param>
        /// <returns>HTTP 200 status with a success message if the car is added; otherwise, HTTP 500 status.</returns>
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> AddCar([FromBody] CarDto carDto)
        {
            using var connection = _context.CreateConnection();
            var dealerId = GetDealerIdFromToken(); // Set the dealer ID from the token
            var result = await connection.ExecuteAsync(
                "INSERT INTO Cars (Make, Model, Year, StockLevel, DealerId) VALUES (@Make, @Model, @Year, @StockLevel, @DealerId)", 
                new
                {
                    carDto.Make,
                    carDto.Model,
                    carDto.Year,
                    carDto.StockLevel,
                    DealerId = dealerId
                }
            );
            return result > 0 ? Ok(new { message = "Car added successfully" }) : StatusCode(500, new { message = "Failed to add car" });
        }

        /// <summary>
        /// Deletes a car entry if it belongs to the authenticated dealer.
        /// </summary>
        /// <param name="request">The request containing the car ID to delete.</param>
        /// <returns>HTTP 200 status with a success message if deleted; otherwise, HTTP 404 status.</returns>
        [HttpDelete]
        [Consumes("application/json")]
        public async Task<IActionResult> DeleteCar([FromBody] DeleteCarRequest request)
        {   
            using var connection = _context.CreateConnection();
            var dealerId = GetDealerIdFromToken(); // Extract dealer ID from token
            var result = await connection.ExecuteAsync(
                "DELETE FROM Cars WHERE Id = @Id AND DealerId = @DealerId", 
                new { Id = request.Id, DealerId = dealerId});
            return result > 0 ? Ok(new { message = "Car deleted successfully" }) : NotFound(new { message = "Car not found" });
        }

        /// <summary>
        /// Retrieves all cars associated with the authenticated dealer.
        /// </summary>
        /// <returns>A list of cars or HTTP 404 status if no cars are found.</returns>
        [HttpGet]
        public async Task<IActionResult> GetCars()
        {
            using var connection = _context.CreateConnection();
            var dealerId = GetDealerIdFromToken();
            var cars = await connection.QueryAsync<Car>(
                "SELECT * FROM Cars WHERE DealerId = @DealerId",
                new { DealerId = dealerId }
            );

            if (!cars.Any())
            {
                return NotFound("No cars found in the database.");
            }
            return Ok(cars);
        }

        /// <summary>
        /// Updates the stock level of a car if it belongs to the authenticated dealer.
        /// </summary>
        /// <param name="request">The request containing the car ID and new stock level.</param>
        /// <returns>HTTP 200 status if updated successfully; otherwise, HTTP 404 status.</returns>
        [HttpPut("stock")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateCarStockLevel([FromBody] UpdateStockRequest request)
        {     
            using var connection = _context.CreateConnection();
            var dealerId = GetDealerIdFromToken(); // Extract dealer ID from token
            var result = await connection.ExecuteAsync(
                "UPDATE Cars SET StockLevel = @StockLevel WHERE Id = @Id AND DealerId = @DealerId",
                new { StockLevel = request.NewStockLevel, Id = request.Id,  DealerId = dealerId }
            );

            return result > 0 ? Ok(new { message = "Stock level updated successfully" }) : NotFound(new { message = "Car not found" });
        }

        /// <summary>
        /// Searches for cars based on make and model, only for the authenticated dealer.
        /// </summary>
        /// <param name="request">The search criteria containing make and model.</param>
        /// <returns>A list of cars matching the criteria or HTTP 404 status if no match is found.</returns>
        [HttpPost("search")]
        [Consumes("application/json")]
        public async Task<IActionResult> SearchCar([FromBody] SearchCarRequest request)
        {   
            using var connection = _context.CreateConnection();
            var dealerId = GetDealerIdFromToken(); // Extract dealer ID from token
            
            var query = "SELECT * FROM Cars WHERE DealerId = @DealerId";
            var parameters = new DynamicParameters();

            parameters.Add("DealerId", dealerId);

            if (!string.IsNullOrEmpty(request.Make))
            {
                query += " AND LOWER(Make) = LOWER(@Make)";
                parameters.Add("Make", request.Make);
            }

            if (!string.IsNullOrEmpty(request.Model))
            {
                query += " AND LOWER(Model) = LOWER(@Model)";
                parameters.Add("Model", request.Model);
            }

            var cars = await connection.QueryAsync<Car>(query, parameters);

            if (cars == null || !cars.Any())
                return NotFound(new { message = "No cars found matching the criteria." });

            return Ok(cars);
        }
    }

    /// <summary>
    /// Represents a object to add a car.
    /// </summary>
    public class CarDto
    {        
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
    }

    /// <summary>
    /// Represents a request to delete a car.
    /// </summary>
    public class DeleteCarRequest
    {   
        /// <summary>
        /// Gets or sets the ID of the car to be deleted.
        /// </summary>
        [Required]
        public int Id { get; set; }
    }

    /// <summary>
    /// Represents a request to update the stock level of a car.
    /// </summary>
    public class UpdateStockRequest
    {
        /// <summary>
        /// Gets or sets the ID of the car.
        /// </summary>
        [Required]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the new stock level for the car.
        /// </summary>
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock level must be a positive number.")]
        public int NewStockLevel { get; set; }
    }

    /// <summary>
    /// Represents a request to search for cars.
    /// </summary>
    public class SearchCarRequest
    {
        /// <summary>
        /// Gets or sets the make of the car to search for.
        /// </summary>
        [Required(ErrorMessage = "Make is required.")]
        [StringLength(50, ErrorMessage = "Make can't be longer than 50 characters.")]
        public required string Make { get; set; }

        /// <summary>
        /// Gets or sets the model of the car to search for.
        /// </summary>
        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, ErrorMessage = "Model can't be longer than 50 characters.")]
        public required string Model { get; set; }
    }
}