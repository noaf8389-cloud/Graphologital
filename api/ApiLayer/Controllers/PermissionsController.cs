using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Data_Extraction;

[ApiController]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly DatabaseService _database;
    private readonly EncryptionService _encryptionService;

    public PermissionsController(DatabaseService database, EncryptionService encryptionService)
    {
        _database = database;
        _encryptionService = encryptionService;
    }

    [Authorize]
    [HttpGet("targets")]
    public async Task<IActionResult> GetTargetsWithPermissions()
    {
        int candidateId = GetCandidateIdFromJwt();

        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT
                t.Id,
                t.FullName,
                t.Email,
                t.RoleDescription,
                CASE WHEN l.Id IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsAllowed
            FROM TargetUsers t
            LEFT JOIN CandidateTargetLinks l
                ON l.TargetUserId = t.Id
                AND l.CandidateId = @CandidateId
            WHERE t.IsActive = 1
            ORDER BY t.FullName;
        ";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CandidateId", candidateId);

        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        var list = new List<object>();

        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                targetUserId = reader.GetInt32(0),
                fullName = reader.IsDBNull(1) ? "" : _encryptionService.SafeDecrypt(reader.GetString(1)),
                email = reader.IsDBNull(2) ? "" : _encryptionService.SafeDecrypt(reader.GetString(2)),
                roleDescription = reader.IsDBNull(3) ? "" : _encryptionService.SafeDecrypt(reader.GetString(3)),
                isAllowed = reader.GetBoolean(4)
            });
        }

        return Ok(list);
    }

    [Authorize]
    [HttpPost("{targetUserId}")]
    public async Task<IActionResult> AllowTargetUser(int targetUserId)
    {
        int candidateId = GetCandidateIdFromJwt();

        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            IF NOT EXISTS (
                SELECT 1
                FROM CandidateTargetLinks
                WHERE CandidateId = @CandidateId
                  AND TargetUserId = @TargetUserId
            )
            BEGIN
                INSERT INTO CandidateTargetLinks (CandidateId, TargetUserId)
                VALUES (@CandidateId, @TargetUserId)
            END
        ";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CandidateId", candidateId);
        cmd.Parameters.AddWithValue("@TargetUserId", targetUserId);

        await cmd.ExecuteNonQueryAsync();

        return Ok(new { message = "הרשאת הצפייה נוספה בהצלחה." });
    }

    [Authorize]
    [HttpDelete("{targetUserId}")]
    public async Task<IActionResult> RemoveTargetUserPermission(int targetUserId)
    {
        int candidateId = GetCandidateIdFromJwt();

        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            DELETE FROM CandidateTargetLinks
            WHERE CandidateId = @CandidateId
              AND TargetUserId = @TargetUserId
        ";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CandidateId", candidateId);
        cmd.Parameters.AddWithValue("@TargetUserId", targetUserId);

        await cmd.ExecuteNonQueryAsync();

        return Ok(new { message = "הרשאת הצפייה הוסרה בהצלחה." });
    }

    private int GetCandidateIdFromJwt()
    {
        string? role =
            User.FindFirst("role")?.Value ??
            User.FindFirst(ClaimTypes.Role)?.Value;

        string? userId =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (role != "candidate" || string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("המשתמש הנוכחי אינו מועמד.");

        return int.Parse(userId);
    }
}