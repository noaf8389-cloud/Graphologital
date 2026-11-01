using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Data_Extraction;

[ApiController]
[Route("api/targetusers")]
public class TargetUsersController : ControllerBase
{
    private readonly DatabaseService _database;

    private readonly PasswordService _passwordService;
    private readonly EncryptionService _encryptionService;
    private readonly JwtService _jwtService;

    public TargetUsersController(DatabaseService database, PasswordService passwordService, EncryptionService encryptionService, JwtService jwtService)
    {
        _database = database;
        _passwordService = passwordService;
        _encryptionService = encryptionService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] TargetRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "יש להזין שם משתמש וסיסמה." });
        }

        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            INSERT INTO TargetUsers (Username, PasswordHash, FullName, Phone, Email, IsActive, RoleDescription)
            VALUES (@Username, @PasswordHash, @FullName, @Phone, @Email, 1, @RoleDescription);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

        if (!InputValidationService.IsValidUsername(request.Username))
            return BadRequest(new { error = "שם משתמש לא תקין." });

        if (!InputValidationService.IsValidPassword(request.Password))
            return BadRequest(new { error = "הסיסמה חייבת להכיל לפחות 6 תווים." });

        if (!InputValidationService.IsValidEmail(request.Email))
            return BadRequest(new { error = "כתובת אימייל לא תקינה." });

        if (!InputValidationService.IsValidPhone(request.Phone))
            return BadRequest(new { error = "מספר טלפון לא תקין." });

        if (!InputValidationService.IsValidTextLength(request.FullName, 100))
            return BadRequest(new { error = "שם מלא ארוך מדי." });
        if (!InputValidationService.IsValidTextLength(request.RoleDescription, 200))
            return BadRequest(new { error = "תיאור התפקיד ארוך מדי." });

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", request.Username);
        cmd.Parameters.AddWithValue("@PasswordHash", _passwordService.HashPassword(request.Password));
        cmd.Parameters.AddWithValue("@FullName", string.IsNullOrWhiteSpace(request.FullName) ? DBNull.Value : _encryptionService.Encrypt(request.FullName));
        cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(request.Phone) ? DBNull.Value : _encryptionService.Encrypt(request.Phone));
        cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(request.Email) ? DBNull.Value : _encryptionService.Encrypt(request.Email));
        cmd.Parameters.AddWithValue("@RoleDescription", string.IsNullOrWhiteSpace(request.RoleDescription) ? DBNull.Value : _encryptionService.Encrypt(request.RoleDescription));

        try
        {
            int id = (int)await cmd.ExecuteScalarAsync();

            return Ok(new
            {
                token = _jwtService.GenerateToken(id, "target"),
                user = new
                {
                    id,
                    username = request.Username,
                    fullName = request.FullName,
                    email = request.Email,
                    phone = request.Phone,
                    role = "target"
                }
            });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            return BadRequest(new { error = "שם המשתמש כבר קיים במערכת." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT Id, Username, PasswordHash, FullName, Email, Phone
            FROM TargetUsers
            WHERE Username = @Username
        ";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", request.Username);

        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Unauthorized(new { error = "שם משתמש או סיסמה שגויים." });
        }

        string storedHash = reader.GetString(2);

        if (!_passwordService.VerifyPassword(request.Password, storedHash))
        {
            return Unauthorized(new { error = "שם משתמש או סיסמה שגויים." });
        }

        int id = reader.GetInt32(0);

        return Ok(new
        {
            token = _jwtService.GenerateToken(id, "target"),
            user = new
            {
                id,
                username = reader.GetString(1),
                fullName = reader.IsDBNull(3) ? "" : _encryptionService.SafeDecrypt(reader.GetString(3)),
                email = reader.IsDBNull(4) ? "" : _encryptionService.SafeDecrypt(reader.GetString(4)),
                phone = reader.IsDBNull(5) ? "" : _encryptionService.SafeDecrypt(reader.GetString(5)),
                role = "target"
            }
        });
    }

    [Authorize]
    [HttpGet("candidates")]
    public async Task<IActionResult> GetSharedCandidates()
    {
        int targetId = GetTargetIdFromJwt();

        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT
                c.Id,
                c.FullName,
                c.Email,
                a.Id,
                a.AnalysisDate,
                a.TopPersonalityType,
                a.DiagnosisText,
                a.ScoreTypeD
            FROM CandidateTargetLinks l
            JOIN Candidates c ON c.Id = l.CandidateId
            OUTER APPLY (
                SELECT TOP 1 *
                FROM Analyses a
                WHERE a.CandidateId = c.Id
                ORDER BY a.Id DESC
            ) a
            WHERE l.TargetUserId = @TargetId
            ORDER BY c.FullName
        ";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TargetId", targetId);

        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        var list = new List<object>();

        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                candidateId = reader.GetInt32(0),
                fullName = reader.IsDBNull(1) ? "" : _encryptionService.SafeDecrypt(reader.GetString(1)),
                email = reader.IsDBNull(2) ? "" : _encryptionService.SafeDecrypt(reader.GetString(2)),
                analysisId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                analysisDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                topPersonalityType = reader.IsDBNull(5) ? "" : reader.GetString(5),
                diagnosisText = reader.IsDBNull(6) ? "" : _encryptionService.SafeDecrypt(reader.GetString(6)),
                score = reader.IsDBNull(7) ? 0 : reader.GetDouble(7)
            });
        }

        return Ok(list);
    }

    private int GetTargetIdFromJwt()
    {
        string? role =
            User.FindFirst("role")?.Value ??
            User.FindFirst(ClaimTypes.Role)?.Value;

        string? userId =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (role != "target" || string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("המשתמש הנוכחי אינו משתמש יעד.");

        return int.Parse(userId);
    }
}

public class TargetRegisterRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? RoleDescription { get; set; }
}