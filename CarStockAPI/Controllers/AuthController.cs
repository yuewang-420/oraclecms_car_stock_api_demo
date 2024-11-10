using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarStockAPI.Data;
using CarStockAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;

namespace CarStockAPI.Controllers
{
    /// <summary>
    /// Controller responsible for handling authentication-related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController: ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="configuration">Configuration instance for accessing application settings.</param>
        /// <param name="context">Database context for data access.</param>
        public AuthController(IConfiguration configuration, DatabaseContext context) 
        {
            this._configuration = configuration;
            this._context = context;
        }

        /// <summary>
        /// Handles user login requests. Validates credentials and returns a JWT token if successful.
        /// </summary>
        /// <param name="loginRequest">The login request containing the dealer ID and password.</param>
        /// <returns>Returns an HTTP 200 status with a success message and JWT token if credentials are valid; otherwise, an HTTP 401 status.</returns>
        [HttpPost("login")]
        [Consumes("application/json")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {   
            using var connection = _context.CreateConnection();
            var user = await connection.QuerySingleOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE DealerId = @DealerId", 
                new { DealerId = loginRequest.DealerId });
            
            // Validate the user and password using BCrypt
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.HashedPassword))
            {
                // Return unauthorized if the user is not found or the password is incorrect
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // Generate a JWT token for the authenticated user
            var token = GenerateJwtToken(user);

            // Set the token in an HttpOnly cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Prevents JavaScript from accessing the cookie for security
                Secure = true, // Ensures the cookie is sent over HTTPS only
                SameSite = SameSiteMode.Strict, // Restricts cookie to same-site requests
                Expires = DateTime.UtcNow.AddHours(1) // Sets the cookie expiration time
            };
            Response.Cookies.Append("jwt", token, cookieOptions);
            
            // Return a success response
            return Ok(new { message = "Logged in successfully" });
        }

        /// <summary>
        /// Generates a JWT token for the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user for whom the token is generated.</param>
        /// <returns>A JWT token string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the JWT key is not configured.</exception>
        private string GenerateJwtToken(User user)
        {
            // Retrieve the JWT signing key from the configuration
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT signing key is not configured. Please check appsettings.json.");
            }

            // Define the claims for the JWT token
            var claims = new[]
            {
                new Claim("dealerId", user.DealerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Create the security key and credentials for signing the token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the JWT token
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Token expiration time
                signingCredentials: creds);

            // Return the serialized token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    /// <summary>
    /// Represents a login request payload.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Gets or sets the dealer ID. Must be a four-digit number.
        /// </summary>
        [Required]
        [Range(1000, 9999, ErrorMessage = "DealerId must be a four-digit number.")]
        public required string DealerId { get; set; }
        
        /// <summary>
        /// Gets or sets the password for login.
        /// </summary>
        [Required]
        public required string Password { get; set; }
    }
}