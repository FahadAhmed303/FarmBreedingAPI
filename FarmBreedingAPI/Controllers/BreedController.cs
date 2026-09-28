using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace FarmBreedingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BreedController : ControllerBase
    {
        private readonly string connectionString =
    "Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";
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