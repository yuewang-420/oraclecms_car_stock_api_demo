using Microsoft.Data.Sqlite;
using System.Data;

namespace CarStockAPI.Data
{
    /// <summary>
    /// Represents the database context for creating and managing database connections.
    /// </summary>
    public class DatabaseContext
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseContext"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        public DatabaseContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        /// <summary>
        /// Creates and returns a new database connection.
        /// </summary>
        /// <returns>An <see cref="IDbConnection"/> instance representing a connection to the database.</returns>
        /// <remarks>
        /// The connection returned is an instance of <see cref="SqliteConnection"/>. Ensure to properly dispose of the connection after use.
        /// </remarks>
        public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
    }
}