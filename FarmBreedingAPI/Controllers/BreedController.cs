using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace FarmBreedingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BreedController : ControllerBase
    {
        private readonly string connectionString;

        public BreedController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is not configured.");
        }

        [HttpGet]
        public async Task<IActionResult> GetBreeds()
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT ""BreedCode"", ""BreedName""
                    FROM ""Breed""
                    ORDER BY ""BreedName""";

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var breeds = new List<object>();

                while (await reader.ReadAsync())
                {
                    breeds.Add(new
                    {
                        breedCode = reader.GetInt32(0),
                        breedName = reader.GetString(1)
                    });
                }

                return Ok(breeds);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}