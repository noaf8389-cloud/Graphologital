using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Data_Extraction;

[ApiController]
[Route("api/candidates")]
public class CandidatesController : ControllerBase
{
    private readonly PasswordService _passwordService;
    private readonly EncryptionService _encryptionService;
    private readonly DatabaseService _database;
    private readonly JwtService _jwtService;

    public CandidatesController(DatabaseService database, PasswordService passwordService, EncryptionService encryptionService, JwtService jwtService)
    {
        _database = database;
        _passwordService = passwordService;
        _encryptionService = encryptionService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CandidateRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "יש להזין שם משתמש וסיסמה." });
        }

        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            INSERT INTO Candidates (Username, PasswordHash, FullName, Phone, Email)
            VALUES (@Username, @PasswordHash, @FullName, @Phone, @Email);

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

        using SqlCommand cmd = new SqlCommand(sql, conn);
        string passwordHash = _passwordService.HashPassword(request.Password);

        cmd.Parameters.AddWithValue("@Username", request.Username);
        
        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        cmd.Parameters.AddWithValue("@FullName", string.IsNullOrWhiteSpace(request.FullName) ? DBNull.Value : _encryptionService.Encrypt(request.FullName));
        cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(request.Phone) ? DBNull.Value : _encryptionService.Encrypt(request.Phone));
        cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(request.Email) ? DBNull.Value : _encryptionService.Encrypt(request.Email));

        try
        {
            int id = (int)await cmd.ExecuteScalarAsync();

            return Ok(new
            {
                token = _jwtService.GenerateToken(id, "candidate"),
                user = new
                {
                    id,
                    username = request.Username,
                    fullName = request.FullName,
                    email = request.Email,
                    phone = request.Phone,
                    role = "candidate"
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
            FROM Candidates
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
            token = _jwtService.GenerateToken(id, "candidate"),
            user = new
            {
                id = reader.GetInt32(0),
                username = reader.GetString(1),
                fullName = reader.IsDBNull(3) ? "" : _encryptionService.SafeDecrypt(reader.GetString(3)),
                email = reader.IsDBNull(4) ? "" : _encryptionService.SafeDecrypt(reader.GetString(4)),
                phone = reader.IsDBNull(5) ? "" : _encryptionService.SafeDecrypt(reader.GetString(5)),
                role = "candidate"
            }
        });
    }

    [Authorize]
    [HttpGet("analyses")]
    public async Task<IActionResult> GetAnalyses()
    {
        int candidateId = GetCandidateIdFromJwt();

        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            SELECT TOP 20
                Id,
                AnalysisDate,
                TopPersonalityType,
                DiagnosisText,
                ScoreTypeA,
                ScoreTypeB,
                ScoreTypeC,
                ScoreTypeD,
                ScoreTypeE,
                ScoreTypeF,
                ScoreTypeG,
                ScoreTypeH,
                ScoreTypeI
            FROM Analyses
            WHERE CandidateId = @CandidateId
            ORDER BY Id DESC
        ";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CandidateId", candidateId);

        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        var list = new List<object>();

        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                id = reader.GetInt32(0),
                analysisDate = reader.GetDateTime(1),
                topPersonalityType = reader.IsDBNull(2) ? "" : reader.GetString(2),
                diagnosisText = reader.IsDBNull(3) ? "" : _encryptionService.SafeDecrypt(reader.GetString(3)),
                scoreTypeA = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                scoreTypeB = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                scoreTypeC = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                scoreTypeD = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                scoreTypeE = reader.IsDBNull(8) ? 0 : reader.GetDouble(8),
                scoreTypeF = reader.IsDBNull(9) ? 0 : reader.GetDouble(9),
                scoreTypeG = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
                scoreTypeH = reader.IsDBNull(11) ? 0 : reader.GetDouble(11),
                scoreTypeI = reader.IsDBNull(12) ? 0 : reader.GetDouble(12)
            });
        }

        return Ok(list);
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

public class CandidateRegisterRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}