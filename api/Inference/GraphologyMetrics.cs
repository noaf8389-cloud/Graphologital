using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
namespace Data_Extraction
{

// --- מודלים לקריאת ה-JSON ---
public class Level
{
    // ערך מייצג של הרמה:
    // ברמה הראשונה — סף עליון להתאמה מלאה
    // ברמה אמצעית — נקודת הממוצע
    // ברמה האחרונה — סף תחתון להתאמה מלאה
    public double ReferenceValue  { get; set; }
    public string Label { get; set; } = string.Empty;
}
public class FeatureConfig
{
    public double      Min    { get; set; }
    public double      Max    { get; set; }
    public List<Level> Levels { get; set; } = new();
}

public class PersonalityData
{
    [JsonPropertyName("Personality_Types")]
    public Dictionary<string, Dictionary<string, string>> Personality_Types { get; set; } = new();
}
// מחלקת הגרפולוגיה!
public class GraphologyMetrics
{
    // === ארגון הכתב ===
    public double Top_Margin;                                                        // שוליים עיליים
    public double Bottom_Margin;                                                     // שוליים תחתיים
    public double Right_Margin,         Right_Margin_SD;                            // שוליים ימניים
    public double Left_Margin,          Left_Margin_SD;                             // שוליים שמאליים
    public int    Char_count;                                                        // כמות התווים
    public double Word_Spacing_Avg,     Word_Spacing_SD;                            // רווח בין מילים
    public double Char_Spacing_Avg,     Char_Spacing_SD;                            // רווח בין אותיות
    public double CW_spacing_avg;                                                    // ממוצע רווחים כולל
    public double Line_Spacing_Avg,     Line_Spacing_SD;                            // רווח בין שורות
    public double Row_Direction,        Row_Direction_SD, Row_undulations, Word_slope; // כיוון שורות

    // === צורת הכתב ===
    public double Angle_Avg,            Angle_SD;                                   // זווית כתיבה
    public double Neutral_Zone_Avg,     Neutral_Zone_SD;                            // גודל אזור אמצעי
    public double Upper_Zone_Mean;                                                   // גודל אזור עילי
    public double Lower_Zone_Mean;                                                   // גודל אזור תחתי
    public double ThreeZone_Ratio;                                                   // יחס תלת אזורי
    public double Avg_Word_Width;                                                    // רוחב הכתיבה
    public double Readability;                                                       // קריאות הכתיבה
    public double Geometric_Shape,      Geometric_Shape_SD;                         // יחסי אורך-רוחב
    // תקן / פישוט / קישוט / העשרה
    public double Style_Standard;     // רמת תקניות צורנית
    public double Style_Simplified;   // רמת פישוט צורני
    public double Style_Decorated;    // רמת קישוטיות
    public double Style_Enriched;     // רמת העשרה צורנית

    // ארבעת צורות הקו
    public double LineShape_Straight;     // קו ישר
    public double LineShape_Convex;       // קו קמור
    public double LineShape_Concave;      // קו קעור
    public double LineShape_Threadlike;   // קו חוטי

    // === תנועת הכתיבה ===
    // לחץ חדירה
    public double Pressure_Avg;       // לחץ כתיבה משוקלל
    public double Pressure_SD;        // חוסר יציבות בלחץ

    // שטף הכתיבה
    public double Flow_Avg;           // רמת שטף כללית
    public double Flow_SD;            // חוסר יציבות בשטף

    // תנועתיות
    public double Movement_Avg;           // רמת תנועתיות כללית
    public double Movement_SD;            // חוסר יציבות בתנועתיות
    public double Movement_Energy;        // אנרגיה / עוצמה
    public double Movement_Sharpness;     // חדות / אימפולסיביות
    public double Movement_Control;       // ריסון / שליטה
    public double Movement_Blocking;      // עצירות / חסימה
    public double Movement_Smear;         // מריחות
    public double Movement_Periodicity;   // מחזוריות

    // מהירות כתיבה
    public double Speed_Avg;          // מדד מהירות כללי
    public double Speed_SD;           // חוסר יציבות במהירות

    public double Stroke_Thickness_avg, Stroke_Thickness_SD, avgEdges;             // עובי הקו
    public double darkness, darkness_SD;                                // כהות הכתיבה

    // דפנות הקו - אחוז לכל סוג דופן
    public double Edge_Straight_Score;    // דפנות ישרות
    public double Edge_Changing_Score;    // דפנות משתנות
    public double Edge_Wavy_Score;        // דפנות מפעפעות
    public double Edge_Jagged_Score;      // דפנות משוסעות
    public double Edge_Irregular_Score;   // דפנות לא סדירות
    public int    contours_count; // מספר הקונטורים
    public int sampled_contours_count;  // מספר הקונטרים שנבדקו במדגם
    public double Edges_Pressure_Avg;  
        
    // === תוצאות מחושבות ===
    public double[,]                    PersonalityDistanceMatrix { get; private set; } = new double[0, 0]; // מטריצת מרחקים [פרמטר, טיפוס]
    public Dictionary<string, double>   PersonalityScores         { get; private set; } = new();           // ציון סופי לכל טיפוס
    public string[]                     TypeNames                 { get; private set; } = Array.Empty<string>(); // שמות הטיפוסים לפי סדר העמודות

    // === מצב פנימי ===
    // הגדרות כל מדד מתוך graphology_config.json
    private Dictionary<string, FeatureConfig> _config = new();
    // טבלת הטיפוסים
    private Dictionary<string, Dictionary<string, string>> _personalityTable = new();
    //מיפוי בין שם מדד לבין השדה המתאים במחלקה 
    private static readonly Dictionary<string, MemberInfo> _memberCache = BuildMemberCache();
    // משקלי חשיבות המדדים
    private Dictionary<string, double> _weights = new();
    // משקל כל מדד לפי סדר השורות במטריצת האישיות
    private double[] _rowWeights = Array.Empty<double>();
    // ערכי מדדי הכתב הגולמיים
    private Dictionary<string, double> _userValues = new();
    // שמירת חישובי האחוזים של סימני הכתב
    private readonly Dictionary<string, double> _percentCache = new();
    // תיאור טיפוסי האישיות
    private Dictionary<string, string> _descriptions = new();
    // הגדרות הסינדרומים
    private Dictionary<string, SyndromeDefinition> _syndromes = new(StringComparer.Ordinal);
    // משתנים קבועים בקוד - קריאה מגייסון
    private readonly GraphologyConstantsOptions _constants;
    private readonly GraphologyFormulaWeightsOptions _formulaWeights;


    // פעולה בונה
    public GraphologyMetrics(Tis tis, GraphologyConstantsOptions constants, GraphologyFormulaWeightsOptions formulaWeights)
    {
        //  קריאת הקבועים
        _constants = constants ?? throw new ArgumentNullException(nameof(constants));
        _formulaWeights = formulaWeights ?? throw new ArgumentNullException(nameof(formulaWeights));
        
        LoadConfigurations();         // טעינת קבצי JSON
        ExtractMetrics(tis);          // חישוב המדדים הפיזיים
        _userValues = GetUserRawValues();       // שמירת חישוב המדדים הפיזיים

        Console.WriteLine($"מספר מדדים שנשמרו: {_userValues.Count} " + $"מתוך {_personalityTable.Count}");
        foreach (KeyValuePair<string, double> metric in _userValues.OrderBy(item => item.Key))
        Console.WriteLine($"{metric.Key} = {metric.Value:F3}");
        Console.WriteLine($"GetUserRawValues, after fizical: {DateTime.Now:HH:mm:ss.fff}");

        var (matrix, rowNames) = LoadPersonalityToMatrix();                      // שלב 3: טבלת טיפוסים למטריצה
        _rowWeights = rowNames.Select(GetWeight).ToArray();     // שמירת משקל כל שורה
        PersonalityDistanceMatrix = BuildDistanceMatrix(matrix, rowNames);       // שלב 4: מטריצת מרחקים
        PersonalityScores = BuildPersonalityScores(PersonalityDistanceMatrix, rowNames);    //  שלב 5: חישוב ציון התאמה משוקלל לכל טיפוס
        
        Console.WriteLine("===== ציוני טיפוסי האישיות =====");
        foreach (KeyValuePair<string, double> item in PersonalityScores.OrderByDescending(x => x.Value))
            Console.WriteLine($"{item.Key}: {item.Value:F2}");
        Console.WriteLine($"סיום חישוב מדדי הכתב: {DateTime.Now:HH:mm:ss.fff}");
    }
            
    // חישוב מדדי הכתב
    private void ExtractMetrics(Tis tis)
    {
        // חישוב פרמטרי הכתב הבלתי תלויים
        // === ארגון הכתב ===
        this.Top_Margin = tis.topMargin();  // שוליים עיליים
        this.Bottom_Margin = tis.BottomMargin();    // שוליים תחתיים
        (this.Left_Margin, this.Left_Margin_SD) = tis.leftMargin(); // שוליים שמאליים
        (this.Right_Margin, this.Right_Margin_SD) = tis.rightMardin();  // שוליים ימניים

        (this.Char_count, Word_Spacing_Avg, this.Word_Spacing_SD, this.Char_Spacing_Avg, this.Char_Spacing_SD, this.CW_spacing_avg) = tis.spaces();  // רווחים בין תווים וכמות תווים
        (this.Line_Spacing_Avg, this.Line_Spacing_SD) = tis.lineSpace();  // רווח בין שורות

        (this.Row_Direction, this.Row_Direction_SD, this.Row_undulations, this.Word_slope) = tis.Row_direction(Word_Spacing_Avg);  // כיוון השורה

        // === צורת הכתב ===
        (this.Angle_Avg, this.Angle_SD) = tis.angleWreating();  // זווית הכתיבה
        (this.Neutral_Zone_Avg, this.Neutral_Zone_SD, this.Upper_Zone_Mean, this.Lower_Zone_Mean, this.ThreeZone_Ratio) = tis.writing_areas();  // שלושת איזורי הכתב ויחס תלת אזורי
        this.Avg_Word_Width = tis.wength(); // רוחב הכתיבה
        this.Readability = tis.read();  // קריאות הכתיבה
        // # תקן פישוט קישוט העשרה
        (this.Geometric_Shape, this.Geometric_Shape_SD) = tis.Geometric_shape();    // יחסי אורך - רוחב
        // # 4 צורות הקו

        // === תנועת הכתיבה ===
        (this.Stroke_Thickness_avg, this.Stroke_Thickness_SD, this.avgEdges) = tis.writing_pressure(_constants);  // עובי הכתב
        Console.WriteLine($"writing_pressure: {DateTime.Now:HH:mm:ss.fff}");
        (this.darkness, this.darkness_SD) = tis.Darkness(_constants, _formulaWeights.Darkness["Average"]); // כהות הכתב
        double avgChangeNorm;
        double stdChangeNorm;
        (this.Edge_Straight_Score, this.Edge_Changing_Score, this.Edge_Wavy_Score, this.Edge_Jagged_Score, this.Edge_Irregular_Score, this.contours_count, this.sampled_contours_count, avgChangeNorm, stdChangeNorm) = tis.EdgesByContours(_constants,  _formulaWeights.ContourEdges); // דפנות הקו
        Console.WriteLine($"EdgesByContours: {DateTime.Now:HH:mm:ss.fff}");

        // === נרמול מדדים בלתי תלויים ===
        double widthPct = GetPercent(nameof(Stroke_Thickness_avg));
        double widthSdPct = GetPercent(nameof(Stroke_Thickness_SD));
        double edgeWidthPct = GetPercent(nameof(avgEdges));

        double darknessPct = GetPercent(nameof(darkness));
        double darknessSdPct = GetPercent(nameof(darkness_SD));

        double edgeStraightPct = GetPercent(nameof(Edge_Straight_Score));
        double edgeChangingPct = GetPercent(nameof(Edge_Changing_Score));
        double edgeWavyPct = GetPercent(nameof(Edge_Wavy_Score));
        double edgeJaggedPct = GetPercent(nameof(Edge_Jagged_Score));
        double edgeIrregularPct = GetPercent(nameof(Edge_Irregular_Score));

        double angleSdPct = GetPercent(nameof(Angle_SD));
        double wordSpacingSdPct = GetPercent(nameof(Word_Spacing_SD));
        double charSpacingSdPct = GetPercent(nameof(Char_Spacing_SD));
        double lineSpacingSdPct = GetPercent(nameof(Line_Spacing_SD));
        double rowDirectionSdPct = GetPercent(nameof(Row_Direction_SD));
        double readabilityPct = GetPercent(nameof(Readability));

        double angleStabilityPct = 100.0 - angleSdPct;
        double wordSpacingStabilityPct = 100.0 - wordSpacingSdPct;
        double lineSpacingStabilityPct = 100.0 - lineSpacingSdPct;

        // === חישוב מדדי הכתב התלויים ===

        // שטף הכתיבה
        // אחוז הקונטרים במדגם - אמדן הרציפות
        double continuityPct = this.sampled_contours_count > 0 ? Math.Clamp(this.contours_count /(double)this.sampled_contours_count * 100.0, 0.0, 100.0) : _constants.Syndromes.DefaultContinuityPercent;

        // לחץ כתיבה
        Dictionary<string, double> edges = _formulaWeights.DerivedMetrics["EdgesPressure"];
        this.Edges_Pressure_Avg = Math.Clamp(edges["EdgeJagged"] * edgeJaggedPct + edges["EdgeIrregular"] * edgeIrregularPct + edges["EdgeChanging"] * edgeChangingPct, 0.0, 100.0);
        (this.Pressure_Avg, this.Pressure_SD) = 
            Tis.WritingPressure(darknessPct, darknessSdPct, widthPct, widthSdPct, edgeWidthPct, this.Edges_Pressure_Avg, _formulaWeights.WritingPressure);
        
        // שטף הכתיבה
        Dictionary<string, double> lineWeights = _formulaWeights.DerivedMetrics["LineStructureStd"];
        double lineStructureStdPct = Math.Clamp(lineWeights["LineSpacingStd"] * lineSpacingSdPct + lineWeights["RowDirectionStd"] * rowDirectionSdPct, 0.0, 100.0); // יציבות הרווח בין השורות ויציבות כיוון השורות
        (this.Flow_Avg, this.Flow_SD) = 
            Tis.CalculateWritingFlowPercent(continuityPct, angleStabilityPct, angleSdPct, wordSpacingStabilityPct, wordSpacingSdPct, 100.0 - lineStructureStdPct, lineStructureStdPct, readabilityPct, 100.0 - this.Pressure_SD, this.Pressure_SD, 100.0 - widthSdPct, widthSdPct, _formulaWeights.WritingFlow);

        // תנועתיות
        (this.Movement_Avg, this.Movement_SD, this.Movement_Energy, this.Movement_Sharpness, this.Movement_Control, this.Movement_Blocking, this.Movement_Smear, this.Movement_Periodicity) =
            Tis.CalculateMovementMetrics(this.Flow_Avg, this.Flow_SD, edgeStraightPct, edgeChangingPct, edgeWavyPct, edgeJaggedPct, edgeIrregularPct, this.Pressure_Avg, this.Pressure_SD, widthPct, widthSdPct, _formulaWeights.Movement);
        // תקן, פישוט, קישוט, העשרה
        (this.Style_Standard, this.Style_Simplified, this.Style_Decorated, this.Style_Enriched) = 
            Tis.CalculateShapeStyleMetrics(avgChangeNorm, stdChangeNorm, edgeJaggedPct, edgeWavyPct, readabilityPct, widthSdPct, _formulaWeights.ShapeStyle);
        // 4 צורות הכתב
        (this.LineShape_Straight, this.LineShape_Convex, this.LineShape_Concave, this.LineShape_Threadlike) = 
            Tis.CalculateLineShapeMetrics(avgChangeNorm, stdChangeNorm, edgeJaggedPct, edgeWavyPct, widthPct, widthSdPct, GetPercent(nameof(Avg_Word_Width)), GetPercent(nameof(Geometric_Shape)), continuityPct, this.Style_Simplified, this.Flow_Avg, edgeStraightPct, edgeWavyPct, edgeJaggedPct, edgeIrregularPct, _formulaWeights.LineShape);
        // מהירות הכתיבה
        (this.Speed_Avg, this.Speed_SD) = 
            Tis.CalculateWritingSpeed( this.Flow_Avg, this.Flow_SD, this.Movement_Avg, this.Movement_SD, continuityPct, this.Style_Simplified, avgChangeNorm, stdChangeNorm, _formulaWeights.WritingSpeed);
    }

    // המרת מדד הכתב לאחוז
    private double GetPercent(string metricName)
    {
        // אם האחוז של המדד כבר חושב בעבר - החזרת הערך שחושב
        if (_percentCache.TryGetValue(metricName, out double cached))
            return cached;

        // בדיקה שהערך קיים בקבצי הגייסון כדי לחשב את אחוזו
        if (!_config.TryGetValue(metricName, out FeatureConfig? feature) || !_memberCache.TryGetValue(metricName, out MemberInfo? member))
            return 0.0;

        // שליפת ערך המדד מתוך האובייקט
        double value = member switch
        {
            FieldInfo fi => Convert.ToDouble(fi.GetValue(this)),
            PropertyInfo pi => Convert.ToDouble(pi.GetValue(this)),
            _ => 0.0
        };

        if (feature.Max <= feature.Min)
            return 0.0;

        // חישוב האחוז בפועל
        double percent = Math.Clamp((value - feature.Min) / (feature.Max - feature.Min) * 100.0, 0.0, 100.0);

        _percentCache[metricName] = percent;
        return percent;
    }

    // טעינת קבצי ההגדרה בפורמט ג'ייסון הדרושים לחישוב
    private void LoadConfigurations()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        // טעינת הגדרות הנרמול
        string configJson = File.ReadAllText("graphology_config.json");
        _config = JsonSerializer.Deserialize<Dictionary<string, FeatureConfig>>(configJson, opts)
                  ?? throw new InvalidDataException("נכשלה טעינת graphology_config.json");

        // טעינת טבלת הטיפוסים
        string typesJson = File.ReadAllText("graphology_personality_types.json");
        var data = JsonSerializer.Deserialize<PersonalityData>(typesJson, opts)
                   ?? throw new InvalidDataException("נכשלה טעינת graphology_personality_types.json");
        _personalityTable = data.Personality_Types;

        // טעינת משקלי חשיבות
        if (File.Exists("graphology_weights.json"))
        {
            string weightsJson = File.ReadAllText("graphology_weights.json");
            _weights = JsonSerializer.Deserialize<Dictionary<string, double>>(weightsJson, opts)
                       ?? new Dictionary<string, double>();
        }
        else
        {
            _weights = new Dictionary<string, double>();
        }

        // טעינת תיאורי טיפוסי האישיות
        const string descriptionsPath = "graphology_personality_descriptions.json";
        if (File.Exists(descriptionsPath))
        {
            string descriptionsJson = File.ReadAllText(descriptionsPath);
            _descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionsJson, opts) ?? new Dictionary<string, string>();
        }
        else
        {
            _descriptions = new Dictionary<string, string>();
            Console.WriteLine($"[אזהרה] הקובץ '{descriptionsPath}' לא נמצא.");
        }

        // טעינת קובץ הסינדרומים
        string syndromesJson = File.ReadAllText("graphology_syndromes.json");
        _syndromes = JsonSerializer.Deserialize<Dictionary<string, SyndromeDefinition>>(syndromesJson, opts)
             ?? throw new InvalidDataException("נכשלה טעינת graphology_syndromes.json");

        // בדיקת התאמה בין הקבצים למחלקה
        ValidateConfigAlignment();
    }

    // בדיקה על קבצי הגייסון שנטענים
    // בדיקת התאמה בין קבצי ה-JSON לבין שדות המחלקה
    private void ValidateConfigAlignment()
    {
        // מדדים שמופיעים ב-config אבל אין להם שדה במחלקה
        foreach (string param in _config.Keys)
        {
            if (!_memberCache.ContainsKey(param))
                Console.WriteLine($"[config→class חסר] המדד '{param}' מופיע ב-config אך לא נמצא כשדה במחלקה.");
        }

        // מדדים שמופיעים בטבלת הטיפוסים אבל אין להם config
        foreach (string param in _personalityTable.Keys)
        {
            if (!_config.ContainsKey(param))
                Console.WriteLine($"[types→config חסר] המדד '{param}' מופיע בטבלת הטיפוסים אך לא נמצא ב-config.");
        }

        // מדדים שמופיעים בטבלת הטיפוסים אבל אין להם שדה במחלקה
        foreach (string param in _personalityTable.Keys)
        {
            if (!_memberCache.ContainsKey(param))
                Console.WriteLine($"[types→class חסר] המדד '{param}' מופיע בטבלת הטיפוסים אך לא נמצא כשדה במחלקה.");
        }

        // מדדים שמופיעים במשקלים אבל אין להם config
        foreach (string param in _weights.Keys)
        {
            if (!_config.ContainsKey(param))
                Console.WriteLine($"[weights→config חסר] המדד '{param}' מופיע במשקלים אך לא נמצא ב-config.");
        }

        // מדדים שמופיעים במשקלים אבל אין להם שדה במחלקה
        foreach (string param in _weights.Keys)
        {
            if (!_memberCache.ContainsKey(param))
                Console.WriteLine($"[weights→class חסר] המדד '{param}' מופיע במשקלים אך לא נמצא כשדה במחלקה.");
        }

        // מדדים שמופיעים ב-config אבל לא משתתפים בטבלת הטיפוסים
        foreach (string param in _config.Keys)
        {
            if (!_personalityTable.ContainsKey(param))
                Console.WriteLine($"[config→types לא משתתף] המדד '{param}' מופיע ב-config אך לא מופיע בטבלת הטיפוסים.");
        }

        // מדדים שמופיעים בטבלת הטיפוסים אך אין להם משקל מוגדר
        foreach (string param in _personalityTable.Keys)
        {
            if (!_weights.ContainsKey(param))
                Console.WriteLine($"[types→weights חסר] המדד '{param}' מופיע בטבלת הטיפוסים אך אין לו משקל מוגדר. ייעשה שימוש בברירת מחדל 1.0.");
        }

        // בדיקה שלכל מדד קיימת תווית עבור כל טיפוס
        string[] expectedTypes = _personalityTable.Count > 0 ? _personalityTable.Values.First().Keys.ToArray() : Array.Empty<string>();
        foreach (var paramEntry in _personalityTable)
        {
            foreach (string typeName in expectedTypes)
            {
                if (!paramEntry.Value.TryGetValue(typeName, out string? label) || string.IsNullOrWhiteSpace(label))
                {
                    Console.WriteLine($"[types חסר] למדד " + $"'{paramEntry.Key}' אין תווית " + $"עבור '{typeName}'.");
                }
            }
        }
    }

    // המרת טבלת הטיפוסים למטריצה
    private (string[,] Matrix, string[] RowNames) LoadPersonalityToMatrix()
    {
        string[] rowNames = _personalityTable.Keys.ToArray();
        TypeNames = _personalityTable.Values.First().Keys.ToArray(); // Type_A … Type_I

        int rows   = rowNames.Length;
        int cols   = TypeNames.Length;
        var matrix = new string[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            Dictionary<string, string> typeRow = _personalityTable[rowNames[i]];

            for (int j = 0; j < cols; j++)
            {
                // גישה לפי שם הטיפוס — מבטיח שהעמודה j תמיד מתאימה ל-TypeNames[j]
                matrix[i, j] = typeRow.TryGetValue(TypeNames[j], out string? val) ? val : string.Empty;
            }
        }

        return (matrix, rowNames);
    }

    // בניית מטריצת מרחקים - חישוב מרחק בין הערך של המשתמש לבין ההגדרה של כל טיפוס
    private double[,] BuildDistanceMatrix(string[,] matrix, string[] rowNames)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        var dist = new double[rows, cols];

        // אתחול המטריצה ב-נאל למניעת התייחסות לפרמטרים שלא חושבו
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                dist[i, j] = double.NaN;

        for (int i = 0; i < rows; i++)
        {
            string param = rowNames[i];

            // פרמטר שלא מוגדר בקונפיג או שאין לו ערך — נשאר NaN
            if (!_config.TryGetValue(param, out FeatureConfig? feature) || !_userValues.TryGetValue(param, out double userVal)) continue;

            for (int j = 0; j < cols; j++)
            {
                string label = matrix[i, j];
                if (string.IsNullOrWhiteSpace(label))
                {
                    dist[i, j] = double.NaN;
                    continue;
                }
                dist[i, j] = ComputeDistance(userVal, label, feature);  
            }       
        }
        return dist;
    }

    // בונה מילון שם-שדה → ערך double עבור כל השדות הרשומים ב-_config
    private Dictionary<string, double> GetUserRawValues()
    {
        var result = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (string paramName in _config.Keys)
        {
            if (!_memberCache.TryGetValue(paramName, out MemberInfo? member))
                continue;

            double value = member switch
            {
                FieldInfo fi => Convert.ToDouble(fi.GetValue(this)),
                PropertyInfo pi => Convert.ToDouble(pi.GetValue(this)),
                _  => double.NaN
            };

            // ערך לא תקין אינו משתתף באבחון
            if (double.IsNaN(value) || double.IsInfinity(value))
                continue;

            FeatureConfig feature = _config[paramName];
            if (feature.Min > 0.0 && Math.Abs(value) < 0.000001)
            {
                Console.WriteLine($"[מדד לא חושב] {paramName} קיבל 0 " + $"כאשר Min={feature.Min}. המדד לא ישתתף באבחון.");
                continue;
            }

            result[paramName] = value;
        }
        return result;
    }

    // חישוב מרחק לתווית בודדת
    private static double ComputeDistance(double userValue, string label, FeatureConfig feature)
    {
        if (feature.Levels == null || feature.Levels.Count == 0)
            return double.NaN;
    
        double range = feature.Max - feature.Min;
        if (range <= 0)
            return double.NaN;

        // ערך מעבר להם מקבל את ערך הגבול אינו נפסל.
        double value = Math.Clamp(userValue, feature.Min, feature.Max);
    
        int last = feature.Levels.Count - 1;
    
        for (int i = 0; i <= last; i++)
        {
            Level level = feature.Levels[i];
    
            if (level.Label != label)
                continue;
    
            double representative = level.ReferenceValue;
            double distRaw;
            double normDivisor;
    
            if (i == 0)
            {
                // רמה ראשונה (קטן / בהיר / רך / סדיר / ...)
                // representative = הגבול העליון של הרמה
                // מתחת לגבול או בדיוק בו → התאמה מלאה
                distRaw   = value <= representative ? 0.0 : value - representative;
                normDivisor = range;
            }
            else if (i == last)
            {
                // רמה אחרונה (גדול / כהה / חד / תנודתי / ...)
                // representative = הגבול התחתון של הרמה
                // מעל הגבול או בדיוק בו → התאמה מלאה
                distRaw   = value >= representative ? 0.0 : representative - value;
                normDivisor = range;
            }
            else
            {
                // רמה אמצעית (ממוצע / זקוף / עגול / ישר / ...)
                // representative = הנקודה המייצגת של הרמה (הממוצע שהוגדר ב-JSON)
                // מרחק מוחלט מנקודה זו, מנורמל לפי מחצית הטווח
                distRaw     = Math.Abs(value - representative);
                normDivisor = range / 2.0;
            }
    
            return Math.Clamp(distRaw / normDivisor * 100.0, 0.0, 100.0);
        }
    
        // התווית לא נמצאה ברשימת הרמות של הפרמטר
        return double.NaN;
    }

    // --- ציון סופי לכל טיפוס לפי ממוצע מרחקים משוקלל ---
    private Dictionary<string, double> BuildPersonalityScores(double[,] dist, string[] rowNames)
    {
        int rows = dist.GetLength(0);
        int cols = dist.GetLength(1);
    
        var scores = new Dictionary<string, double>(cols);
    
        for (int j = 0; j < cols; j++)
        {
            double weightedSum = 0;
            double weightsSum = 0;
    
            for (int i = 0; i < rows; i++)
            {
                double distance = dist[i, j];

                // פרמטר חסר לא נכנס לחישוב
                if (double.IsNaN(distance))
                    continue;

                string metricName = rowNames[i];

                // טבלת התוויות של כל הטיפוסים עבור המדד הנוכחי והתווית של הטיפוס הנוכחי במדד
                if (!_personalityTable.TryGetValue(metricName, out Dictionary<string, string>? labelsByType)
                    || !labelsByType.TryGetValue(TypeNames[j], out string? currentLabel) || string.IsNullOrWhiteSpace(currentLabel))
                    continue;

                // כמה טיפוסים חולקים את אותה התווית
                int sameLabelCount = labelsByType.Values.Count(label => string.Equals(label, currentLabel, StringComparison.Ordinal));

                int typeCount = labelsByType.Count;

                // ככל שפחות טיפוסים חולקים את התווית, היא מבחינה יותר בין הטיפוסים
                double discriminationFactor =typeCount <= 1 ? 1.0 : Math.Log((double)typeCount / sameLabelCount) / Math.Log(typeCount);;

                // מדד שאינו מבדיל כלל בין הטיפוסים לא משתתף
                if (discriminationFactor <= 0.0)
                    continue;

                double effectiveWeight = _rowWeights[i] * discriminationFactor;
                weightedSum += distance * effectiveWeight;
                weightsSum += effectiveWeight;
            }

            if (weightsSum == 0.0)
            {
                scores[TypeNames[j]] = double.NaN;
                continue;
            }

            double avgWeightedDistance = weightedSum / weightsSum;

            // ציון התאמה = 100 פחות המרחק הממוצע המשוקלל
            scores[TypeNames[j]] = Math.Clamp(100.0 - avgWeightedDistance, 0.0, 100.0);
        }

        return scores;
    }

    // חישוב המשקל לכל פרמטר
    private double GetWeight(string paramName)
    {
        return _weights.TryGetValue(paramName, out double w) && w > 0
            ? w
            : 1.0;
    }

    // מחזירה את שם הטיפוס המוביל ואת האבחון שלו מהקובץ
    public (string TypeKey, string Diagnosis) GetPersonalityDiagnosis()
    {
        if(PersonalityScores == null || PersonalityScores.Count == 0)
            return (string.Empty, "לא קיימים ציוני אישיות לחישוב.");

        KeyValuePair<string, double> top = PersonalityScores.MaxBy(kv => kv.Value);

        string topKey = top.Key;
        string diagnosis = "תיאור האישיות הכללי: " + Environment.NewLine;
        diagnosis +=  _descriptions.TryGetValue(topKey, out string? text) ? text : "אין תיאור זמין לטיפוס זה.";
        diagnosis += Environment.NewLine + this.SyndromesPersonalityDiagnosis();

        return (topKey, diagnosis);
    }

    // אבחון לפי סינדרומים
    public string SyndromesPersonalityDiagnosis()
    {
        // רשימת הסינדרומים שמתאימים לכותב
        List<(SyndromeDefinition syndrome, double score)> matches = new();
        // לפחות 55% ממשקל הסימנים שנבדקו חייבים לתמוך בסינדרום
        double MinimumCoverageRatio = 0.55;

        // מעבר על כל הסינדרומים
        foreach (KeyValuePair<string, SyndromeDefinition> syndromeEntry in this._syndromes)
        {
            // הסנדרום הנוכחי
            SyndromeDefinition syndrome = syndromeEntry.Value;
            // מספר הסימנים שתומכים בסנדרום
            int matchedSigns = 0;
            // מספר הסימנים התקינים שנבדקו בפועל
            int checkedSigns = 0;
            // סכום משקלי כל הסימנים שנבדקו בפועל
            double checkedWeight = 0.0;
            // משקל הסימנים החזקים שהתאימו
            double matchedWeight = 0.0;
            // סכום ציוני הסימנים החזקים לפי משקל
            double matchedScoreSum = 0.0;
            // התחומים הבלתי תלויים שבהם נמצאו סימנים חזקים
            HashSet<string> matchedDomains = new(StringComparer.OrdinalIgnoreCase);

            // מעבר על כל סימני הכתב שמרכיבים את הסינדרום.
            foreach (SyndromeSign sign in syndrome.Signs)
            {
                // בדיקה שהמדד קיים בקובץ ההגדרות, שיש לו ערך שחושב עבור הכותב ושהוגדרה עבורו תווית בסינדרום
                if (!_config.TryGetValue(sign.Metric, out FeatureConfig? feature)
                    || !_userValues.TryGetValue(sign.Metric,out double userValue)
                    || string.IsNullOrWhiteSpace(sign.Label))
                    continue;

                // משקל סימן הכתב
                double weight = sign.Weight;

                // משקל שגוי אינו משתתף בחישוב
                if (weight <= 0.0)
                    continue;

                // חישוב מרחק מכל הגדרה ובחירת ההגדרה המתאימה לסימן הכתב
                double distance = ComputeDistance(userValue, sign.Label, feature);

                // התווית אינה קיימת או שלא ניתן לחשב מרחק
                if (double.IsNaN(distance))
                    continue;

                // הפיכת המרחק לציון התאמה
                double signScore = Math.Clamp(100.0 - distance, 0.0, 100.0);

                checkedSigns++;
                checkedWeight += weight;

                // רק סימן בעל התאמה חזקה נחשב תומך
                if (signScore < Math.Min(100.0, _constants.Syndromes.StrongSignScore + _constants.Syndromes.StrictSignScoreAddition))
                    continue;

                // הוספה למספר הסימנים שתומכים בסנדרום
                matchedSigns++;
                matchedWeight += weight;
                matchedScoreSum += signScore * weight;

                // שמירת התחום שבו נמצא הסימן התומך
                if (!string.IsNullOrWhiteSpace(sign.Domain))
                    matchedDomains.Add(sign.Domain);
            }

            if (checkedWeight <= 0.0 || matchedWeight <= 0.0)
                continue;

            // ממוצע ההתאמה של הסימנים החזקים שעברו את הסף
            double matchedAverage = Math.Clamp(matchedScoreSum / matchedWeight, 0.0, 100.0);

            // אחוז המשקל של הסימנים החזקים מתוך כל הסימנים שנבדקו
            double coverageRatio = Math.Clamp(matchedWeight / checkedWeight, 0.0, 1.0);

            // ציון הסינדרום:
            double syndromeScore = Math.Clamp(matchedAverage * (0.85 + 0.15 * coverageRatio), 0.0, 100.0);

            double requiredSyndromeScore = Math.Min(100.0, syndrome.MinimumScore + _constants.Syndromes.StrictSyndromeScoreAddition);

            Console.WriteLine( $"{syndrome.Trait}: " + $"סימנים מתאימים={matchedSigns}, " + $"ציון={syndromeScore:F1}, " + $"נדרש סימנים={syndrome.MinimumMatchedSigns}, " + $"נדרש ציון={requiredSyndromeScore}");

            // הסינדרום מתקבל רק כאשר: נמצאו מספיק סימנים תומכים ומשקלם הכולל עובר את הסף
            if (matchedSigns >= syndrome.MinimumMatchedSigns &&  matchedDomains.Count >= syndrome.MinimumIndependentDomains && coverageRatio >= MinimumCoverageRatio && syndromeScore >= requiredSyndromeScore)
            {
                matches.Add((syndrome, syndromeScore));
            }
        }

        // מיון הסינדרומים שעברו את התנאי מהסינדרום הגבוה לנמוך
        List<(SyndromeDefinition syndrome, double score)> strongestMatches = matches.OrderByDescending(match => match.score).ToList();
        
        Console.WriteLine("מספר הסינדרומים " + strongestMatches.Count);

        if (strongestMatches.Count == 0)
            return string.Empty;

        StringBuilder diagnosis = new();

        diagnosis.AppendLine("התכונות הבאות התגלו באופן מיוחד אצל הכותב:");

        // מילות קישור ליצירת טקסט רציף וטבעי
        string[] connectors = {"", "נוסף על כך, ", "כמו כן, ", "לצד זאת, ", "עוד ניכרת ", "זאת ועוד, "};
        
        for (int i = 0; i < strongestMatches.Count; i++)
        {
            var match = strongestMatches[i];
            diagnosis.Append(i < connectors.Length ? connectors[i] : "בנוסף, ");
            diagnosis.Append($"בתחום {match.syndrome.Trait}, ");
            diagnosis.AppendLine(match.syndrome.Diagnosis.Trim());
            // רווח בין תכונה לתכונה
            if (i < strongestMatches.Count - 1)
            {
                diagnosis.Append(" ");
            }
        }

        return diagnosis.ToString().Trim();
    }


    // בונה פעם אחת (static) מילון של כל השדות הציבוריים במחלקה
    private static Dictionary<string, MemberInfo> BuildMemberCache()
    {
        var cache = new Dictionary<string, MemberInfo>(StringComparer.Ordinal);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        foreach (FieldInfo    fi in typeof(GraphologyMetrics).GetFields(flags))     cache[fi.Name] = fi;
        foreach (PropertyInfo pi in typeof(GraphologyMetrics).GetProperties(flags)) cache.TryAdd(pi.Name, pi); // שדות מנצחים על מאפיינים
        return cache;
    }
}
}