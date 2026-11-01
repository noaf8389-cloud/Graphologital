using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Data_Extraction;

[ApiController]
[Route("api/dbtest")]
public class DbTestController : ControllerBase
{
    private readonly DatabaseService _database;

    public DbTestController(DatabaseService database)
    {
        _database = database;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        using SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Candidates", conn);
        int count = (int)await cmd.ExecuteScalarAsync();

        return Ok(new
        {
            status = "ok",
            candidatesCount = count
        });
    }
}