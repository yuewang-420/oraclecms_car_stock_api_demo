using CarStockAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Retrieve the JWT signing key from configuration and validate its presence.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT signing key is not configured. Please check appsettings.json.");
}

// Configure CORS (Cross-Origin Resource Sharing) to allow requests from a specific origin.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.WithOrigins("http://localhost:5039")  // Specify the allowed origin.
               .AllowAnyMethod()  // Allow all HTTP methods (GET, POST, PUT, DELETE, etc.).
               .AllowAnyHeader() // Allow all headers.
               .AllowCredentials(); // Allow cookies and credentials to be sent with the requests.
    }); 
});

// Register services to the dependency injection container.
// Add controllers to the container to enable MVC functionality.
builder.Services.AddControllers();

// Add Swagger/OpenAPI services for generating API documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Retrieve the database connection string from the configuration and validate its presence.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Register the DatabaseContext as a singleton service.
builder.Services.AddSingleton(new DatabaseContext(connectionString));

// Build the application.
var app = builder.Build();


// Configure the HTTP request pipeline for development environments.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enable Swagger middleware for generating API documentation.
    app.UseSwaggerUI(); // Enable Swagger UI for API exploration.
}

// Enable HTTPS redirection to ensure secure communication.
app.UseHttpsRedirection();
// Apply the CORS policy configured earlier.
app.UseCors("AllowAllOrigins");

// Map controller routes for handling API requests.
app.MapControllers();

// Run the application and start processing requests.
app.Run();
