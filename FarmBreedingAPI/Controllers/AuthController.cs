using Microsoft.AspNetCore.Mvc;
using Npgsql;
using FarmBreedingAPI.Models;

namespace FarmBreedingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string connectionString =
"Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"SELECT 1 FROM ""Users"" 
                               WHERE ""Username""=@u AND ""Password""=@p";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u", model.Username);
                cmd.Parameters.AddWithValue("@p", model.Password);

                var result = await cmd.ExecuteScalarAsync();

                if (result != null)
                {
                    return Ok(new { success = true });
                }

                return Unauthorized(new { success = false, message = "Invalid login" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}