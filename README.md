# CarStockAPI

## Overview

CarStockAPI is a web API built using ASP.NET Core that provides functionalities related to car stock management. The API includes authentication with JWT tokens, allowing each dealer to manage only their own car data. The project is built with secure authentication and well-structured endpoints for seamless integration with clients.

## Problem-Solving Approach

The primary objective of this API is to enable dealers to perform CRUD (Create, Read, Update, Delete) operations on their car inventory securely. To meet this requirement, the following approaches were adopted:

- Secure Authentication: Implemented using JWT tokens to ensure that only authenticated users can access specific functionalities.
- Dealer-Specific Access Control: Each dealer is restricted to managing only their own car data through token-based identification.

## File Structure

Below is a breakdown of the project structure:
CarStockAPI

- Controllers/
  - AuthController.cs
    - Handles user authentication and JWT generation
  - CarsController.cs
    - Manages car inventory CRUD operations
- Data/
  - DatabaseContext.cs
    - Sets up database connection using SQLite
- Models/
  - Car.cs
    - Represents the Car model
  - User.cs
    - Represents the User model for authentication
- Program.cs
  - Main entry point for configuring services and middleware
- appsettings.json
  - Configuration file for database and JWT settings
- README.md
  - Project documentation

## Running the Project

Follow these steps to set up and run the CarStockAPI project:

### Prerequisites

- .NET SDK
- SQLite (for database)
- Visual Studio Code or any C# IDE

### Setup Instructions

- Clone the Repository:
  - git clone https://github.com/yourusername/CarStockAPI.git
  - `cd CarStockAPI`
- Restore Dependencies:
  - `dotnet restore`
- Build the Project:
  - `dotnet build`
- Run the Project:
  - `dotnet run`
- Access the API:
  - Navigate to https://localhost:5039/swagger to view and test the API documentation.

## Authentication Workflow

- Login:
  - Use the /api/auth/login endpoint to authenticate with DealerId and Password. The server responds with a JWT token in a cookie.
- "Logout":
  - Delete the jwt cookie in the browser.
- Access Car Operations:
  - Use the JWT token to access secured endpoints like adding, updating, deleting, or retrieving cars related to the logged-in dealer.

## API Endpoints and Descriptions

1. AuthController

- Handles authentication operations, allowing users to log in and receive a JWT token for secure access to the API.

- POST /api/auth/login

  - Description: Authenticates a dealer using their DealerId and Password. If the credentials are valid, it returns an HTTP-only JWT cookie for session management.
  - Request Body:

    `{ "DealerId": "1234", "Password": "yourpassword" }`

  - Response:
    - 200 OK:
      `{ "message": "Logged in successfully" }`
    - 401 Unauthorized:
      `{ "message": "Invalid username or password" }`

2. CarsController

- Handles CRUD operations related to car management. These endpoints require the dealer to be authenticated and restrict operations to their specific DealerId.

- GET /api/cars

  - Description: Retrieves a list of all cars associated with the authenticated dealer.
  - Response:

    - 200 OK: Returns an array of cars.
    - 404 Not Found: `{"message": "No cars found in the database."}`

- POST /api/cars

  - Description: Adds a new car to the inventory for the authenticated dealer.
  - Request Body:

    `{ "Make": "Toyota", "Model": "Corolla", "Year": 2020, "StockLevel": 15 }`

  - Response:
    - 200 OK: `{"message": "Car added successfully"}`
    - 500 Internal Server Error: `{"message": "Failed to add car"}`

- DELETE /api/cars

  - Description: Deletes a car from the inventory based on Id, only if it belongs to the authenticated dealer.
  - Request Body:

    `{ "id": 1234 }`

  - Response:
    - 200 OK: `{"message": "Car deleted successfully"}`
    - 404 Not Found: `{"message": "Car not found"}`

- PUT /api/cars/stock

  - Description: Updates the stock level of a car by its Id, only if it belongs to the authenticated dealer.
  - Request Body:

    `{ "id": 1234, "newStockLevel": 20 }`

  - Response:
    - 200 OK: `{"message": "Stock level updated successfully"}`
    - 404 Not Found: `{"message": "Car not found"}`

- POST /api/cars/search

  - Description: Searches for cars by Make and Model, restricted to the authenticated dealer's cars.
  - Request Body:

    `{ "Make": "Toyota", "Model": "Corolla" }`

  - Response:

    - 200 OK: Returns an array of cars matching the search criteria.
    - 404 Not Found: `{"message": "No cars found matching the criteria."}`

## User Accounts

The following are the dealer accounts available for testing the API:

- Dealer 1:
  `{ "DealerId" : "1001", "Password": "password123" }`

- Dealer 2:
  `{ "DealerId" : "1002", "Password": "password456" }`

- Dealer 3:
  `{ "DealerId" : "1003", "Password": "password789" }`

- Dealer 4:
  `{ "DealerId" : "1004", "Password": "password000" }`

These credentials can be used to authenticate with the /api/auth/login endpoint to receive a JWT token.

## Input Validation (DataAnnotations)
Each model in the project has relevant `DataAnnotations` attributes to ensure that data conforms to required formats, ranges, and constraints. Below are some examples of how DataAnnotations have been applied:

- Required Fields: Ensures that necessary data fields are present in the request.
- String Length: Limits the maximum length of string properties to prevent excessively long input.
- Range Checks: Validates numerical properties to ensure they fall within a specified range.
