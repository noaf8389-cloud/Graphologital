using Microsoft.Extensions.Options;
namespace Data_Extraction;
public class GraphologyAnalysisService
{
    private readonly GraphologyConstantsOptions _constants;
    private readonly GraphologyFormulaWeightsOptions _formulaWeights;
    public GraphologyAnalysisService(IOptions<GraphologyConstantsOptions> constants, IOptions<GraphologyFormulaWeightsOptions> formulaWeights)
    {
        _constants = constants?.Value ?? throw new ArgumentNullException(nameof(constants));
        _formulaWeights = formulaWeights?.Value ?? throw new ArgumentNullException(nameof(formulaWeights));
    }
    public async Task<AnalysisResult> AnalyzeAsync(string imagePath, int candidateId)
    {
        // שימוש בקוד הקיים שלך, בלי לשנות אותו
        Tis tis = await Tis.CreateAsync(imagePath, _constants);
        
        if (tis.SortedLines == null || tis.SortedLines.Count == 0)
        {
            throw new Exception("לא נמצאו שורות או אותיות בתמונה. יש להעלות תמונת כתב יד ברורה יותר.");
        }

        if (tis.SortedLines.Any(line => line == null || line.Count == 0))
        {
            throw new Exception("נמצאה שורה ריקה בתוצאות הזיהוי. יש להעלות תמונה ברורה יותר.");
        }

        var metrics = new GraphologyMetrics(tis, _constants, _formulaWeights);

        var (type, diagnosis) = metrics.GetPersonalityDiagnosis();

        return new AnalysisResult
        {
            CandidateId = candidateId,
            HandwritingImage = imagePath,

            TopPersonalityType = type,
            DiagnosisText = diagnosis,

            ScoreTypeA = GetScore(metrics, "Type_A"),
            ScoreTypeB = GetScore(metrics, "Type_B"),
            ScoreTypeC = GetScore(metrics, "Type_C"),
            ScoreTypeD = GetScore(metrics, "Type_D"),
            ScoreTypeE = GetScore(metrics, "Type_E"),
            ScoreTypeF = GetScore(metrics, "Type_F"),
            ScoreTypeG = GetScore(metrics, "Type_G"),
            ScoreTypeH = GetScore(metrics, "Type_H"),
            ScoreTypeI = GetScore(metrics, "Type_I")
        };
    }

    private static double GetScore(GraphologyMetrics metrics, string typeName)
    {
        if (metrics.PersonalityScores == null)
            return 0.0;

        return metrics.PersonalityScores.TryGetValue(typeName, out double score) ? score : 0;
    }
}