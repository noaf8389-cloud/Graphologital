using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Data_Extraction;

[ApiController]
[Route("api/handwriting")]
public class HandwritingController : ControllerBase
{
    private readonly GraphologyAnalysisService _analysisService;
    private readonly DatabaseService _database;
    private readonly IWebHostEnvironment _env;
    private readonly EncryptionService _encryptionService;

    public HandwritingController(GraphologyAnalysisService analysisService, DatabaseService database, IWebHostEnvironment env, EncryptionService encryptionService)
    {
        _analysisService = analysisService;
        _database = database;
        _env = env;
        _encryptionService = encryptionService;
    }

    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "לא נשלח קובץ." });
            
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".pdf" };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "סוג קובץ לא נתמך. יש להעלות תמונת כתב יד תקינה." });
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { error = "הקובץ גדול מדי. ניתן להעלות קובץ עד 10MB." });
            }

            int candidateId = GetCandidateIdFromJwt();

            string uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads");

            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            string fileName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            AnalysisResult result = await _analysisService.AnalyzeAsync(filePath, candidateId);

            int analysisId = await SaveAnalysisToDatabase(result);

            return Ok(new
            {
                message = "האבחון בוצע בהצלחה.",
                analysisId,
                analysis = result
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("===== ERROR IN HANDWRITING UPLOAD =====");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("=======================================");
            return StatusCode(500, new
            {
                error = "שגיאה פנימית בשרת"
            });
        }
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

    private async Task<int> SaveAnalysisToDatabase(AnalysisResult result)
    {
        using SqlConnection conn = _database.CreateConnection();
        await conn.OpenAsync();

        string sql = @"
            INSERT INTO Analyses (
                CandidateId,
                HandwritingImage,
                TopPersonalityType,
                PersonalityName,
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
            )
            VALUES (
                @CandidateId,
                @HandwritingImage,
                @TopPersonalityType,
                @PersonalityName,
                @DiagnosisText,
                @ScoreTypeA,
                @ScoreTypeB,
                @ScoreTypeC,
                @ScoreTypeD,
                @ScoreTypeE,
                @ScoreTypeF,
                @ScoreTypeG,
                @ScoreTypeH,
                @ScoreTypeI
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

        using SqlCommand cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@CandidateId", result.CandidateId);
        cmd.Parameters.AddWithValue("@HandwritingImage", (object?)result.HandwritingImage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TopPersonalityType", (object?)result.TopPersonalityType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PersonalityName", (object?)result.TopPersonalityType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DiagnosisText", string.IsNullOrWhiteSpace(result.DiagnosisText) ? DBNull.Value : _encryptionService.Encrypt(result.DiagnosisText));

        cmd.Parameters.AddWithValue("@ScoreTypeA", result.ScoreTypeA);
        cmd.Parameters.AddWithValue("@ScoreTypeB", result.ScoreTypeB);
        cmd.Parameters.AddWithValue("@ScoreTypeC", result.ScoreTypeC);
        cmd.Parameters.AddWithValue("@ScoreTypeD", result.ScoreTypeD);
        cmd.Parameters.AddWithValue("@ScoreTypeE", result.ScoreTypeE);
        cmd.Parameters.AddWithValue("@ScoreTypeF", result.ScoreTypeF);
        cmd.Parameters.AddWithValue("@ScoreTypeG", result.ScoreTypeG);
        cmd.Parameters.AddWithValue("@ScoreTypeH", result.ScoreTypeH);
        cmd.Parameters.AddWithValue("@ScoreTypeI", result.ScoreTypeI);

        object? id = await cmd.ExecuteScalarAsync();

        return id == null ? 0 : Convert.ToInt32(id);
    }
}