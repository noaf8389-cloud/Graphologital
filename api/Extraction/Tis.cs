using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Data_Extraction
{
// Tis class
public partial class Tis
{
    // מיקומים סביב פיקסל
    private static readonly int[] BoundaryDx = { -1, 0, 1, -1, 1, -1, 0, 1 };
    private static readonly int[] BoundaryDy = { -1, -1, -1, 0, 0, 1, 1, 1 };
    private static readonly int[] TraceDx = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] TraceDy = { -1, -1, 0, 1, 1, 1, 0, -1 };

    // משתני התקשורת
    private static readonly HttpClient _client = new HttpClient{ Timeout = TimeSpan.FromMinutes(5) };

    // משתני המחלקה
    public string ImgPath { get; set; }
    public Image<Rgba32>? Img { get; set; }
    public Image<Rgba32>? ImgGray { get; set; }
    public List<List<Character>> SortedLines { get; set; }
    public bool[,] matrix { get; set; }
    //public int count { get; set; }

    // פעולה בונה
    // הפעולה הבונה מוגדרת כ-private כדי לאלץ שימוש ב-CreateAsync האסינכרוני
    private Tis(string imgPath)
    {
        this.ImgPath = imgPath;
        this.SortedLines = new List<List<Character>>();
    }

    // פונקציה אסינכרונית ליצירת ומילוי האובייקט על ידי שרת הפייתון
    public static async Task<Tis> CreateAsync(string imgPath, GraphologyConstantsOptions constants)
    {
        if (constants == null)
            throw new ArgumentNullException(nameof(constants));
    
        // טעינת תמונה ראשית
        if (!File.Exists(imgPath))
            throw new FileNotFoundException("קובץ התמונה לא נמצא", imgPath);
        // יצירת אובייקט ריק
        Tis tis = new Tis(imgPath);

        try
        {
            Console.WriteLine($"שליחה לשרת הפייתון: {DateTime.Now:HH:mm:ss.fff}");
            // המרה לג'ייסון ושליחה לשרת 
            var requestData = new { image_path = imgPath };
            string jsonPayload = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PostAsync(constants.PythonService.ApiUrl, content);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"שגיאת רשת מול שרת פייתון: {response.StatusCode}");

            // פיענוח התשובה מהשרת
            // מחרוזת הטקסט שחזרה מהשרת
            string jsonResponse = await response.Content.ReadAsStringAsync();
            // התעלמות מאותיות קטנות וגדולות
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // הפיכת הטקסט לאובייקט
            PythonApiResponse apiResult = JsonSerializer.Deserialize<PythonApiResponse>(jsonResponse, options);
            
            if (apiResult == null || apiResult.Status != "success")
                throw new Exception($"שרת פייתון החזיר שגיאה: {apiResult?.Message}");

            // טעינת התמונה המעובדת - לא נשלחה בשרת כדי לשמור על יעילות
            string directory = Path.GetDirectoryName(imgPath);
            string processedPath = Path.Combine(directory, "processed_result.png");

            // התמונה המקורית נשמרת עבור חישוב הכהות
            if (File.Exists(processedPath))
            {
                tis.Img = Image.Load<Rgba32>(processedPath);
                tis.ImgGray = tis.Img.Clone();
                // הפיכת התמונה לשחור-לבן
                tis.Img.Mutate(x => x.BinaryThreshold(0.5f)); 
                tis.matrix = ConvertImageToMatrix(tis.Img); 
            }
            // גיבוי: אם קובץ התמונה המעובדת לא נוצר, טעינת התמונה המקורית
            else
            {
                tis.Img = Image.Load<Rgba32>(imgPath);
                tis.Img.Mutate(x => x.BinaryThreshold(0.5f));
                tis.ImgGray = tis.Img.Clone();
                tis.matrix = ConvertImageToMatrix(tis.Img);
            }

            // יצירת רשימות התווים לפי הנתונים מהשרת
            tis.SortedLines.Clear();

            foreach (List<PythonCharDto> pyLine in apiResult.Predictions)
            {
                var cSharpLine = new List<Character>();
                foreach (var pyChar in pyLine)
                {
                    if (pyChar.W <= 0 || pyChar.H <= 0)
                        continue;
                    char? actualChar = string.IsNullOrEmpty(pyChar.Char) ? null : (char?)pyChar.Char[0];
                    Character characterObj = new Character(x: pyChar.X, y: pyChar.Y, w: pyChar.W, h: pyChar.H, conf: pyChar.Confidence, pClass: actualChar, sourceParent: tis.Img );
                    
                    // הוספת התו לשורה הנוכחית
                    cSharpLine.Add(characterObj);
                }
                
                // הוספת השורה לרשימת השורות
                if (cSharpLine.Count > 0)
                    tis.SortedLines.Add(cSharpLine);
            }

            Console.WriteLine($"חישוב נתונים גרפולוגיים:{DateTime.Now:HH:mm:ss.fff}");

            return tis;
        }

        catch (Exception ex)
        {
            throw new Exception($"נכשלה יצירת אובייקט הניתוח: {ex.Message}", ex);
        }
    }


    // המרת התמונה למטריצה
    public static bool[,] ConvertImageToMatrix(Image<Rgba32> img)
    {
        int height = img.Height;
        bool[,] matrix = new bool[img.Width, img.Height];

        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    matrix[x, y] = row[x].R < 128;
                }
            }
        });
        return matrix;
    }



// מחלקות לקריאת נתוני הג'ייסון
    public class PythonCharDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public string Char { get; set; }
        public double Confidence { get; set; }
    }

    public class PythonApiResponse
    {
        public string Status { get; set; }
        public List<List<PythonCharDto>> Predictions { get; set; }
        public string Message { get; set; }
    }


//////////////////////////////////////////////////////////////////////////////
    /// פונקציה שמדמה קבלת אובייקט Tis מלא מפייתון לצורך בדיקות
    // public static Tis CreateMockData()
    // {
    //     // יצירת האובייקט הראשי
    //     Tis mockTis = new Tis(@"User\Desktop\ה-פרויקט\data set\14996257\AgeAnnotations_and_Split\train\w2_M_3_form31.tif");
    //     // שורה ראשונה
    //     List<Character> line1 = new List<Character>
    //     {
    //         new Character(950, 40, 15, 30, 0.98, 'י', mockTis.Img),
    //         new Character(925, 40, 25, 35, 0.95, 'מ', mockTis.Img),
    //         new Character(865, 35, 30, 45, 0.97, 'ש', mockTis.Img),
    //         new Character(950, 40, 15, 30, 98, 'י', mockTis.Img),
    //         new Character(925, 40, 25, 35, 95, 'מ', mockTis.Img),
    //         new Character(910, 40, 10, 30, 94, 'י', mockTis.Img),
    //         new Character(865, 35, 30, 45, 97, 'ש', mockTis.Img),
    //         new Character(840, 30, 20, 50, 96, 'ל', mockTis.Img),
    //         new Character(800, 42, 25, 35, 92, 'ת', mockTis.Img),
    //         new Character(775, 42, 20, 35, 93, 'מ', mockTis.Img),
    //         new Character(755, 40, 10, 35, 98, 'י', mockTis.Img),
    //         new Character(735, 42, 20, 35, 91, 'ו', mockTis.Img),
    //         new Character(710, 42, 25, 35, 94, 'ס', mockTis.Img),
    //         new Character(685, 42, 25, 35, 95, 'מ', mockTis.Img),
    //         new Character(630, 40, 25, 40, 96, 'ה', mockTis.Img),
    //         new Character(605, 40, 25, 40, 92, 'ס', mockTis.Img),
    //         new Character(580, 40, 25, 40, 91, 'כ', mockTis.Img),
    //         new Character(555, 40, 25, 40, 94, 'מ', mockTis.Img),
    //         new Character(530, 40, 20, 45, 95, 'ל', mockTis.Img),
    //         new Character(480, 45, 25, 35, 93, 'ם', mockTis.Img),
    //         new Character(455, 45, 10, 35, 98, 'י', mockTis.Img),
    //         new Character(435, 45, 20, 35, 94, 'א', mockTis.Img),
    //         new Character(415, 45, 20, 35, 92, 'כ', mockTis.Img),
    //         new Character(390, 45, 20, 35, 91, 'ז', mockTis.Img),
    //         new Character(340, 45, 20, 35, 96, 'ו', mockTis.Img),
    //         new Character(320, 45, 20, 35, 94, 'נ', mockTis.Img),
    //         new Character(300, 45, 20, 35, 92, 'ח', mockTis.Img),
    //         new Character(280, 45, 20, 35, 91, 'נ', mockTis.Img),
    //         new Character(260, 45, 20, 35, 95, 'א', mockTis.Img),
    //         new Character(230, 45, 10, 20, 99, ',', mockTis.Img),
    //         new Character(190, 42, 25, 35, 94, 'ק', mockTis.Img),
    //         new Character(165, 42, 20, 35, 92, 'ו', mockTis.Img),
    //         new Character(145, 42, 25, 35, 91, 'ח', mockTis.Img),
    //         new Character(125, 42, 20, 35, 95, 'ה', mockTis.Img),
    //         new Character(85, 42, 10, 30, 97, 'י', mockTis.Img),
    //         new Character(65, 42, 20, 30, 93, 'פ', mockTis.Img),
    //         new Character(30, 42, 20, 35, 96, 'ל', mockTis.Img),
    //         new Character(10, 42, 20, 35, 94, 'ע', mockTis.Img)
    //     };
    //     // שורה שנייה
    //     List<Character> line2 = new List<Character>
    //     {
    //         new Character(960, 140, 15, 35, 0.92, 'ו', mockTis.Img),
    //         new Character(940, 140, 20, 35, 0.91, 'נ', mockTis.Img),
    //         new Character(895, 140, 25, 40, 0.98, 'ש', mockTis.Img),
    //         new Character(960, 140, 15, 35, 92, 'ו', mockTis.Img),
    //         new Character(940, 140, 20, 35, 91, 'נ', mockTis.Img),
    //         new Character(920, 140, 20, 45, 96, 'ל', mockTis.Img),
    //         new Character(895, 140, 25, 40, 98, 'ש', mockTis.Img),
    //         new Character(840, 142, 25, 35, 94, 'ק', mockTis.Img),
    //         new Character(815, 142, 25, 35, 93, 'ת', mockTis.Img),
    //         new Character(795, 142, 15, 35, 92, 'ו', mockTis.Img),
    //         new Character(775, 142, 20, 35, 95, 'ב', mockTis.Img),
    //         new Character(720, 145, 20, 35, 94, 'ה', mockTis.Img),
    //         new Character(700, 145, 10, 35, 98, 'י', mockTis.Img),
    //         new Character(680, 145, 20, 45, 91, 'ו', mockTis.Img),
    //         new Character(655, 145, 25, 35, 93, 'ל', mockTis.Img),
    //         new Character(630, 145, 25, 35, 92, 'ת', mockTis.Img),
    //         new Character(575, 140, 25, 35, 94, 'ם', mockTis.Img),
    //         new Character(550, 140, 20, 35, 93, 'ה', mockTis.Img),
    //         new Character(530, 140, 20, 35, 91, 'ל', mockTis.Img),
    //         new Character(505, 140, 25, 35, 96, 'ש', mockTis.Img),
    //         new Character(450, 145, 20, 35, 94, 'ת', mockTis.Img),
    //         new Character(430, 145, 20, 35, 92, 'ו', mockTis.Img),
    //         new Character(405, 145, 25, 35, 95, 'מ', mockTis.Img),
    //         new Character(385, 145, 20, 35, 91, 'כ', mockTis.Img),
    //         new Character(365, 145, 20, 35, 94, 'ה', mockTis.Img),
    //         new Character(340, 145, 25, 35, 96, 'ש', mockTis.Img),
    //         new Character(315, 145, 20, 35, 92, 'כ', mockTis.Img),
    //         new Character(240, 140, 20, 35, 94, 'ה', mockTis.Img),
    //         new Character(220, 140, 20, 35, 92, 'נ', mockTis.Img),
    //         new Character(200, 140, 25, 35, 95, 'ש', mockTis.Img),
    //         new Character(180, 140, 20, 35, 91, 'ב', mockTis.Img),
    //         new Character(130, 145, 20, 35, 96, 'ה', mockTis.Img),
    //         new Character(110, 145, 25, 35, 94, 'ש', mockTis.Img),
    //         new Character(85, 145, 20, 35, 92, 'פ', mockTis.Img),
    //         new Character(65, 145, 15, 35, 95, 'ו', mockTis.Img),
    //         new Character(40, 145, 25, 35, 93, 'ח', mockTis.Img)
    //     };
    //     // שורה שלישית
    //     List<Character> line3 = new List<Character>
    //     {
    //         new Character(940, 940, 25, 40, 0.97, 'ם', mockTis.Img),
    //         new Character(920, 940, 10, 35, 0.94, 'י', mockTis.Img),
    //         new Character(955, 240, 30, 45, 97, 'ם', mockTis.Img),
    //         new Character(930, 240, 10, 35, 95, 'י', mockTis.Img),
    //         new Character(910, 240, 20, 40, 96, 'מ', mockTis.Img),
    //         new Character(890, 240, 10, 35, 94, 'י', mockTis.Img),
    //         new Character(850, 245, 25, 45, 99, '5', mockTis.Img),
    //         new Character(800, 245, 20, 40, 92, 'ך', mockTis.Img),
    //         new Character(780, 245, 25, 40, 94, 'ש', mockTis.Img),
    //         new Character(755, 245, 25, 40, 93, 'מ', mockTis.Img),
    //         new Character(730, 245, 20, 40, 95, 'ב', mockTis.Img),
    //         new Character(680, 240, 15, 35, 94, 'ם', mockTis.Img),
    //         new Character(660, 240, 10, 35, 92, 'י', mockTis.Img),
    //         new Character(640, 240, 20, 35, 91, 'ד', mockTis.Img),
    //         new Character(620, 240, 20, 35, 96, 'ב', mockTis.Img),
    //         new Character(600, 240, 15, 35, 94, 'ו', mockTis.Img),
    //         new Character(580, 240, 20, 35, 95, 'ע', mockTis.Img),
    //         new Character(530, 245, 25, 35, 93, 'ם', mockTis.Img),
    //         new Character(510, 245, 20, 35, 92, 'ת', mockTis.Img),
    //         new Character(490, 245, 20, 35, 98, 'א', mockTis.Img),
    //         new Character(440, 240, 20, 35, 94, 'ל', mockTis.Img),
    //         new Character(420, 240, 25, 35, 92, 'ש', mockTis.Img),
    //         new Character(400, 240, 25, 35, 91, 'מ', mockTis.Img),
    //         new Character(380, 240, 20, 35, 95, 'ל', mockTis.Img),
    //         new Character(330, 245, 20, 35, 94, 'ך', mockTis.Img),
    //         new Character(310, 245, 20, 35, 92, 'כ', mockTis.Img),
    //         new Character(240, 240, 15, 35, 96, 'ן', mockTis.Img),
    //         new Character(220, 240, 15, 35, 94, 'ו', mockTis.Img),
    //         new Character(200, 240, 20, 35, 91, 'ג', mockTis.Img),
    //         new Character(180, 240, 20, 35, 93, 'ר', mockTis.Img),
    //         new Character(160, 240, 20, 35, 95, 'א', mockTis.Img),
    //         new Character(140, 240, 20, 35, 92, 'ב', mockTis.Img)
    //     };
    //     // הוספת השורות לאובייקט הראשי
    //     mockTis.SortedLines.Add(line1);
    //     mockTis.SortedLines.Add(line2);
    //     mockTis.SortedLines.Add(line3);
    //     return mockTis;
    // }
}
}