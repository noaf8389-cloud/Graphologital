using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Data_Extraction
{
public partial class Tis : Character
{
// עובי הקו
    public (double avg, double sq, double avgEdges) writing_pressure(GraphologyConstantsOptions constants)
    {
        // הפיכת התמונה למטריצה
        bool[,] binaryMatrix = this.matrix;
        // זימון הפונקציה למציאת מטריצת השלד
        bool[,] skeletonMatrix = GenerateSkeleton(binaryMatrix);

        // זימון פונקציה לבניית מטריצת סכומים עבור המטריצות
        int[,] sumBinary = CreateIntegralImage(binaryMatrix);
        int[,] sumSkeleton = CreateIntegralImage(skeletonMatrix);

        // שמירת רוחב הקו עבור כל אות ובקצוות האותיות בשביל חישוב סטיית התקן
        double widthSum = 0;
        double widthSumSquares = 0;
        int widthCount = 0;

        double edgeWidthSum = 0;
        int edgeWidthCount = 0;

        int edgeWindowRadius = constants.ImageProcessing.EdgeWindowRadius;
        int edgeWindowDiameter = edgeWindowRadius * 2 + 1;

        foreach (List<Character> line in this.SortedLines)
        {
            foreach (Character c in line)
            {
                // פונקציית החישוב: $$Sum = S(x2, y2) - S(x1-1, y2) - S(x2, y1-1) + S(x1-1, y1-1)$$
                // הגדרת נקודות הקצה של התיבה התוחמת
                int x1 = (int) c.X;
                int y1 = (int) c.Y;
                int x2 = (int) c.X + (int) c.W - 1;
                int y2 = (int) c.Y + (int) c.H - 1;

                // מניעת קריסה: בדיקה שנקודות הקצה לא חורגות מגבולות המטריצה בפועל
                x2 = Math.Min(x2, sumBinary.GetLength(0) - 1);
                y2 = Math.Min(y2, sumBinary.GetLength(1) - 1);
                x1 = Math.Max(x1, 0);
                y1 = Math.Max(y1, 0);
                
                // 🛡️ חישוב בטוח של האינטגרלים ללא גישה לאינדקס שלילי
                double bin_xy = sumBinary[x2, y2];
                double bin_x1y = (x1 > 0) ? sumBinary[x1 - 1, y2] : 0;
                double bin_xy1 = (y1 > 0) ? sumBinary[x2, y1 - 1] : 0;
                double bin_x1y1 = (x1 > 0 && y1 > 0) ? sumBinary[x1 - 1, y1 - 1] : 0;
                
                double cBinary = bin_xy - bin_x1y - bin_xy1 + bin_x1y1;

                double skel_xy = sumSkeleton[x2, y2];
                double skel_x1y = (x1 > 0) ? sumSkeleton[x1 - 1, y2] : 0;
                double skel_xy1 = (y1 > 0) ? sumSkeleton[x2, y1 - 1] : 0;
                double skel_x1y1 = (x1 > 0 && y1 > 0) ? sumSkeleton[x1 - 1, y1 - 1] : 0;

                double cSkeleton = skel_xy - skel_x1y - skel_xy1 + skel_x1y1;
                
                if (cSkeleton > 0)
                {
                    // עובי ממוצע לתו הנוכחי
                    double currentWidth = cBinary / cSkeleton;
                    // הוספה לרשימה
                    widthSum += currentWidth;
                    widthSumSquares += currentWidth * currentWidth;
                    widthCount++;

                    // חישוב קצוות בתוך גבולות התו הנוכחי
                    for (int x = x1; x <= x2; x++)
                    {
                        for (int y = y1; y <= y2; y++)
                        {
                            // אם הפיקסל קיים בשלד, נבדוק אם הוא קצה (שכן אחד בלבד)
                            if (skeletonMatrix[x, y] && CountNeighbors(skeletonMatrix, x, y) == 1)
                            {
                                // יצירת חלון בגודל 5*5 סביב הקצה שנמצא
                                double edgeLocalWidth = SumLocalWindow(binaryMatrix, x, y, edgeWindowRadius) / edgeWindowDiameter;
                                edgeWidthSum += edgeLocalWidth;
                                edgeWidthCount++;
                            }
                        }
                    }
                }
            }
        }

        if (widthCount == 0)
            return (0, 0, 0);
        // ממוצע
        double avgWidth = widthSum / widthCount;
        // סטיית תקן
        double variance = (widthSumSquares / widthCount) - (avgWidth * avgWidth);
        double stdDevWidth = Math.Sqrt(Math.Max(0, variance));
        // ממוצע עובי הקצוות
        double avgEdgeWidth = edgeWidthCount > 0 ? edgeWidthSum / edgeWidthCount : 0;

        return (avgWidth, stdDevWidth, avgEdgeWidth);
    }


    // בניית מטריצת סכומים על ידי תכנון לינארי
    public int[,] CreateIntegralImage(bool[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);
        int[,] sumMatrix = new int[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int val = matrix[x, y] ? 1 : 0;

                int top = (y > 0) ? sumMatrix[x, y - 1] : 0;
                int left = (x > 0) ? sumMatrix[x - 1, y] : 0;
                int topLeft = (x > 0 && y > 0) ? sumMatrix[x - 1, y - 1] : 0;
                sumMatrix[x, y] = val + top + left - topLeft;
            }
        }
        return sumMatrix;
    }

    // סכימת מספר הפיקסלים השחורים שסמוכים לפיקסל
    private int CountNeighbors(bool[,] skeleton, int x, int y)
    {
        int count = 0;
    
        int width = skeleton.GetLength(0);
        int height = skeleton.GetLength(1);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                bool isCenter = dx == 0 && dy == 0;

                int nx = x + dx;
                int ny = y + dy;

                bool inBounds = nx >= 0 && ny >= 0 && nx < width && ny < height;

                if (!isCenter && inBounds && skeleton[nx, ny])
                    count++;
            }
        }
        return count;
    }

    // בניית מטריצת שלד הכתב ממטריצת הכתב
    public static bool[,] GenerateSkeleton(bool[,] binaryMatrix)
    {
        bool[,] skeleton = (bool[,]) binaryMatrix.Clone();
        bool changed = true;

        int width = skeleton.GetLength(0);
        int height = skeleton.GetLength(1);

        // רשימה זמנית לאחסון קואורדינטות למחיקה בכל שלב
        List<(int x, int y)> toRemove = new List<(int, int)>();

        while (changed)
        {
            changed = false;

            // האלגוריתם מחולק לשני תתי-שלבים (Steps) כדי לשמור על מבנה סימטרי
            for (int step = 1; step <= 2; step++)
            {
                toRemove.Clear();

                // סריקת המטריצה
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        // אם הפיקסל הנוכחי הוא רקע
                        if (!skeleton[x, y]) continue;

                        // שליפת ערכי 8 השכנים (P2 עד P9 לפי סדר השעון)
                        int p2 = skeleton[x, y - 1] ? 1 : 0; // צפון
                        int p3 = skeleton[x + 1, y - 1] ? 1 : 0; // צפון-מזרח
                        int p4 = skeleton[x + 1, y] ? 1 : 0; // מזרח
                        int p5 = skeleton[x + 1, y + 1] ? 1 : 0; // דרום-מזרח
                        int p6 = skeleton[x, y + 1] ? 1 : 0; // דרום
                        int p7 = skeleton[x - 1, y + 1] ? 1 : 0; // דרום-מערב
                        int p8 = skeleton[x - 1, y] ? 1 : 0; // מערב
                        int p9 = skeleton[x - 1, y - 1] ? 1 : 0; // צפון-מערב

                        // בדיקת מספר השכנים הפעילים: B(P1)
                        int neighbors = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;

                        // בדיקת מספר המעברים מלבן לשחור: A(P1)
                        int transitions = 0;
                        if (p2 == 0 && p3 == 1) transitions++;
                        if (p3 == 0 && p4 == 1) transitions++;
                        if (p4 == 0 && p5 == 1) transitions++;
                        if (p5 == 0 && p6 == 1) transitions++;
                        if (p6 == 0 && p7 == 1) transitions++;
                        if (p7 == 0 && p8 == 1) transitions++;
                        if (p8 == 0 && p9 == 1) transitions++;
                        if (p9 == 0 && p2 == 1) transitions++;

                        // תנאי הסף למחיקת פיקסל
                        if (neighbors >= 2 && neighbors <= 6 && transitions == 1)
                        {
                            bool canDelete;
                            if (step == 1)
                                // שלב 1: בדיקה שהפיקסל אינו קצה דרומי או מזרחי
                                canDelete = (p2 * p4 * p6 == 0) && (p4 * p6 * p8 == 0);
                            else
                                // שלב 2: בדיקה שהפיקסל אינו קצה צפוני או מערבי
                                canDelete = (p2 * p4 * p8 == 0) && (p2 * p6 * p8 == 0);

                            if (canDelete)
                            {
                                toRemove.Add((x, y));
                            }
                        }
                    }
                }

                // מחיקה בפועל של הפיקסלים שסומנו בתת-השלב הנוכחי
                if (toRemove.Count > 0)
                {
                    changed = true;
                    foreach (var (rx, ry) in toRemove)
                    {
                        skeleton[rx, ry] = false;
                    }
                }
            }
        }
        return skeleton;
    }

    // חישוב מספר הפיקסלים השחורים בחלון
    private int SumLocalWindow(bool[,] matrix, int x, int y, int radius)
    {
        int count = 0;
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                int nx = x + i;
                int ny = y + j;

                // בדיקת גבולות בקצוות התמונה כדי למנוע קריסה
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (matrix[nx, ny]) count++;
                }
            }
        }
        return count;
    }


/////////////////////////////////////////////////////////////////
// כהות הכתב
    public (double avg, double sq) Darkness(GraphologyConstantsOptions constants, Dictionary<string, double> weights)
    {
        double maxPixelValue = 255.0;
        double stdDivisor = constants.ImageProcessing.DarknessStandardDeviationDivisor;

        double sum = 0;
        double sumSquares = 0;
        double maxDarkness = 0;
        int count = 0;
        int w = this.ImgGray.Width;
        int h = this.ImgGray.Height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // אם זהו פיקסל רקע
                if (this.matrix[x, y])
                {
                    Rgba32 pixel = this.ImgGray[x, y];
                    double brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
                    double darkness = maxPixelValue  - brightness; // החלפת הכהות והבהירות כדי שהכהה יהיה מס' יותר גבוה

                    sum += darkness;
                    sumSquares += darkness * darkness;
                    maxDarkness = Math.Max(maxDarkness, darkness);
                    count++;  
                }
            }
        }

        if (count == 0)
            return (0, 0);
        // ממוצע כהות הכתב
        double avgDarkness = sum / count;
        // סטיית תקן של הכהות
        double variance = (sumSquares / count) - (avgDarkness * avgDarkness);
        double stdDarkness = Math.Sqrt(Math.Max(0, variance));
        // המרה לאחוזים
        double avgPercent = avgDarkness / maxPixelValue  * 100.0;
        double maxPercent = maxDarkness / maxPixelValue  * 100.0;
        double stdPercent = stdDarkness / stdDivisor * 100.0;

        // שלא יעבור 100
        stdPercent = Math.Clamp(stdPercent, 0.0, 100);

        // שקלול הכהות עבור לחץ הכתיבה לפי החשיבות:
        // הממוצע הכי חשוב
        // המקסימום מראה כמה יש אזורים כהים מאוד
        // המינימום מראה אם גם החלקים הבהירים עדיין כהים
        // סטיית תקן משפיעה מעט, כי היא יותר מדד לחוסר אחידות
        double pressure =
            avgPercent * weights["AverageDarkness"] +
            maxPercent * weights["MaximumDarkness"] +
            stdPercent * weights["DarknessStd"];

        // החזרה בטווח 0 עד 100
        return (Math.Clamp(pressure, 0.0, 100.0), stdPercent);
    }


/////////////////////////////////////////////////////////////////
// דפנות הקו

    // פונקציה שמחשבת את צורת דפנות הקו עבור מדגם מתוך הקונטורים
    public (double straightScore, double changingScore, double wavyScore, double jaggedScore, double irregularScore, int contours_count, int sampledCandidatesCount, double avgChangePercent, double stdChangePercent) 
        EdgesByContours(GraphologyConstantsOptions constants, Dictionary<string, Dictionary<string, double>> weights)
    {
        var cfg = constants.Contours;

        // רשימת הקונטורים
        (List<List<Point>> contours, int sampledCandidatesCount) = FindSampleContours(this.matrix, cfg.MaximumContours, cfg.MinimumPoints, cfg.TraceMaximumSteps);

        if (contours == null || contours.Count == 0)
            return (0, 0, 0, 0, 0, 0, sampledCandidatesCount, 0, 0);

        int contours_count = contours.Count;

         // כמה כל נקודה בקונטור סטטה מקו הישר
         double sumChange = 0;
        double sumChange2 = 0;

        int totalSharpCorners = 0; // מספר הפינות החדות
        int totalDirectionChanges = 0; // מספר התנודות - עולה יורד
        int totalCheckedPoints = 0; // כמות הנקודות שנבדקו

        int step = cfg.PointStep; // דילוג נקודות כדי להפחית רעש

        foreach (List<Point> contour in contours)
        {
            if (contour.Count > step * 2)
            {
                double prevChange = 0;
                int prevSign = 0;
    
                for (int i = step; i < contour.Count - step; i++)
                {
                    // שלוש נקודות על הקונטור: לפני, נוכחית, אחרי
                    Point a = contour[i - step];
                    Point b = contour[i];
                    Point c = contour[i + step];
    
                    // מחשבים את הזווית בנקודה b
                    double angle = Angle(a, b, c);
    
                    // שינוי מקו ישר: 0 = ישר, גדול = עיקול/שבירה
                    double change = 180 - angle;
    
                    sumChange += change;
                    sumChange2 += change * change;
                    totalCheckedPoints++;
    
                    // פינה חדה
                    if (angle < cfg.SharpCornerAngle)
                        totalSharpCorners++;
    
                    // תנודות: שינוי עולה-יורד
                    int sign = change > prevChange ? 1 :
                               change < prevChange ? -1 :
                               0;
    
                    // אם השינוי של הזויית קטן או גדול מהזווית הקודמת זו תנודה
                    if (prevSign != 0 && sign != 0 && sign != prevSign)
                        totalDirectionChanges++;
    
                    if (sign != 0)
                        prevSign = sign;
    
                    prevChange = change;
                }
            }
        }
        if (totalCheckedPoints <  cfg.MinimumCheckedPoints)
            return (0, 0, 0, 0, 0, contours_count, sampledCandidatesCount, 0, 0);

        // ממוצע שינויי הזווית
        double avgChange = sumChange / totalCheckedPoints;;

        // סטיית תקן של שינויי הזווית
        double variance = (sumChange2 / totalCheckedPoints) - (avgChange * avgChange);
        if (variance < 0)
        variance = 0;
        double stdChange = Math.Sqrt(variance);

        avgChange = Math.Clamp(avgChange / cfg.AverageChangeDivisor * 100.0, 0.0, 100.0);
        stdChange = Math.Clamp(stdChange / cfg.StandardDeviationDivisor  * 100.0, 0.0, 100.0);

        // אחוז פינות חדות
        double sharpRatio = (double)totalSharpCorners / totalCheckedPoints;
        double sharpScore = Math.Clamp(sharpRatio * 100.0, 0.0, 100.0);

        // אחוז תנודות חוזרות
        double waveRatio = (double)totalDirectionChanges / totalCheckedPoints;
        double waveScore  = Math.Clamp(waveRatio  * 100.0, 0.0, 100.0);

        // חישוב ציון לכל דופן

        // דפנות ישרות:
        // ככל שהשינוי, התנודתיות, החדוּת והגליות נמוכים יותר — הדופן ישרה יותר.
        double straightScore =
        100.0 -
        (
            weights["Straight"]["AverageAngleChange"] * avgChange +
            weights["Straight"]["AngleChangeStd"] * stdChange +
            weights["Straight"]["SharpCorners"] * sharpScore +
            weights["Straight"]["WaveRatio"] * waveScore
        );

        // דפנות משתנות:
        // שינויי כיוון וחוסר יציבות של הדופן.
        double changingScore =
            weights["Changing"]["AverageAngleChange"] * avgChange +
            weights["Changing"]["AngleChangeStd"] * stdChange +
            weights["Changing"]["WaveRatio"] * waveScore;

        // דפנות מפעפעות:
        // מבוסס בעיקר על תנודות חוזרות וגליות.
        double wavyScore =
            weights["Wavy"]["WaveRatio"] * waveScore +
            weights["Wavy"]["AngleChangeStd"] * stdChange;

        // דפנות משוסעות:
        // מבוסס בעיקר על פינות חדות ושבירות.
        double jaggedScore =
            weights["Jagged"]["SharpCorners"] * sharpScore +
            weights["Jagged"]["AverageAngleChange"] * avgChange +
            weights["Jagged"]["AngleChangeStd"] * stdChange;

        // דפנות לא סדירות:
        // חוסר יציבות כללי שמשלב תנודתיות, שינוי, גליות וחדות.
        double irregularScore =
            weights["Irregular"]["AngleChangeStd"] * stdChange +
            weights["Irregular"]["AverageAngleChange"] * avgChange +
            weights["Irregular"]["WaveRatio"] * waveScore +
            weights["Irregular"]["SharpCorners"] * sharpScore;

        // הגנה - הגבלת כל הציונים לטווח תקין 0-100
        straightScore  = Math.Clamp(straightScore,  0.0, 100.0);
        changingScore  = Math.Clamp(changingScore,  0.0, 100.0);
        wavyScore      = Math.Clamp(wavyScore,      0.0, 100.0);
        jaggedScore    = Math.Clamp(jaggedScore,    0.0, 100.0);
        irregularScore = Math.Clamp(irregularScore, 0.0, 100.0);

        return (straightScore, changingScore, wavyScore, jaggedScore, irregularScore, contours_count, sampledCandidatesCount, avgChange, stdChange);
    }

    // מציאת מדגם של קונטרים
    public static (List<List<Point>>, int sampledCandidatesCount) FindSampleContours(bool[,] binary, int maxContours, int minPoints, int traceMaximumSteps)
    {
        int w = binary.GetLength(0);
        int h = binary.GetLength(1);

        bool[,] boundaryMap = BuildBoundaryMap(binary, w, h);

        // מטריצה לסימון הפיקסלים שכבר עברנו עליהם
        bool[,] visited = new bool[w, h];

        // רשימת הקונטורים שנמצאו
        List<List<Point>> contours = new List<List<Point>>();

        // מספר הקונטרים שבמדגם
        int sampledCandidatesCount = 0;

        // דילוג קבוע בין עמודות כדי לפזר את המדגם
        int columnStep = Math.Max(1, w / maxContours);

        for (int x = 1; x < w - 1 && contours.Count < maxContours; x+=columnStep)
        {
            for (int y = 1; y < h - 1 && contours.Count < maxContours; y++)
            {
                // מתחילים קונטור רק מפיקסל שחור שהוא גבול
                if (!visited[x, y] && boundaryMap[x, y])
                {
                    sampledCandidatesCount++;
                    List<Point> contour = TraceContour(boundaryMap ,visited, x, y, traceMaximumSteps);

                    // מסננים קונטורים קטנים שהם כנראה רעש
                    if (contour.Count >= minPoints)
                        contours.Add(contour);
                        // משתמשים בקונטור תקין אחד בלבד מכל עמודת דגימה
                        break;
                }
            }
        }
        return (contours, sampledCandidatesCount);
    }

    // בדיקה האם פיקסל בגבול
    private static bool[,] BuildBoundaryMap (bool[,] binary, int w, int h)
    {
        bool[,] map = new bool[w, h];

        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (binary[x, y])
                {
                    for (int k = 0; k < BoundaryDx.Length && !map[x, y]; k++)
                    {
                        // אם יש לידו פיקסל רקע — הוא פיקסל גבול
                        if (!binary[x + BoundaryDx[k], y + BoundaryDy[k]])
                        {
                            map[x, y] = true;
                        }
                    }
                }
            }
        }
        return map;
    }

    // סיבוב סביב קונטור אחד
    private static List<Point> TraceContour(bool[,] boundaryMap, bool[,] visited, int startX, int startY, int traceMaximumSteps)
    {
        // רשימת פיקסלי הקונטור לפי הסדר
        List<Point> contour = new List<Point>();
        int w = boundaryMap.GetLength(0);
        int h = boundaryMap.GetLength(1);

        int x = startX;
        int y = startY;
        int dir = 7; // כיוון התחלתי

        int maxSteps = Math.Min(w * h, traceMaximumSteps);

        bool keepTracing = true;
        for (int i = 0; i < maxSteps && keepTracing; i++)
        {
            // הוספת הנקודה הנוכחית לקונטור
            contour.Add(new Point(x, y));
            // סימון שביקרנו בנקודה הזאת
            visited[x, y] = true;

            bool foundNext = false;

            // חישוב כיוון הכניסה לפיקסל והתחלת הסריקה משם
            int startDir = (dir + 6) % 8;

            for (int k = 0; k < TraceDx.Length && !foundNext; k++)
            {
                int checkDir = (startDir + k) % 8;
                int nx = x + TraceDx[checkDir];
                int ny = y + TraceDy[checkDir];
                
                // בדיקה שלא חורג מגבולות התמונה
                bool inBounds = nx > 0 && ny > 0 && nx < w - 1 && ny < h - 1;
                // אם הפיקסל חורג מגבולות התמונה או אם זה הפיקסל ממנו באנו
                if (inBounds)
                {
                    // אם זהו פקסל גבול
                    bool isStartPoint = nx == startX && ny == startY && contour.Count > 2;
                
                    if (boundaryMap[nx, ny] && (!visited[nx, ny] || isStartPoint))
                    {
                        // התקדמות לנקודה הבאה בקונטור
                        x = nx;
                        y = ny;          
                        dir = checkDir;
                        foundNext = true;
                    }
                }
            }
            // הגעה חזרה לנקודת ההתחלה
            bool returnedToStart = x == startX && y == startY && contour.Count > 2;
                keepTracing = foundNext && !returnedToStart;

        }
        return contour;
    }

    // חישוב זויית בין 3 נקודות בקונטור של האות
    public static double Angle(Point a, Point b, Point c)
    {
        // וקטור בין נקודה הראשונה לשניה
        double v1x = a.X - b.X;
        double v1y = a.Y - b.Y;

        // וקטור בין הנקודה השניה לשלישית
        double v2x = c.X - b.X;
        double v2y = c.Y - b.Y;

        // מכפלה סקלרית בין שני הווקטורים
        double dot = v1x * v2x + v1y * v2y;

        // אורך הווקטורים
        double lenSq = (v1x * v1x + v1y * v1y) * (v2x * v2x + v2y * v2y);

        // אין שינוי כיוון
        if (lenSq == 0)
            return 180;

        // נוסחת קוסינוס הזווית:
        double cos = dot / Math.Sqrt(lenSq);

        // תיקון קטן למניעת שגיאות חישוב עקב דיוק מחשב
        cos = Math.Clamp(cos, -1.0, 1.0);

        // המרת הזווית למעלות
        return Math.Acos(cos) * 180.0 / Math.PI;
    }


///////////////////////////////////////////////////////////
// לחץ הכתיבה
    public static (double avg, double sq) WritingPressure(
    double darkness,      // כהות הכתב באחוזים
    double darknessStdPercent,      // סטיית תקן כהות
    double widthAvgPercent,         // עובי כתב ממוצע באחוזים
    double widthStdPercent,         // סטיית תקן של עובי הכתב באחוזים
    double edgeWidthPercent,        // עובי קצוות האותיות באחוזים
    double edgeInstability,              // דפנות הקו
    Dictionary<string, Dictionary<string, double>> weights)     // משקלי לחץ הכתיבה
    {
        // משקלים לכל סימן לקביעת הלחץ
        Dictionary<string, double> average = weights["Average"];
        Dictionary<string, double> standardDeviation = weights["StandardDeviation"];

        // שקלול ממוצע הלחץ
        double pressure =
            darkness * average["Darkness"] +   // כהות הכתב
            widthAvgPercent * average["WidthAverage"] +      // עובי הכתב
            edgeWidthPercent * average["EdgeWidth"] +     // פורקן / עובי קצוות
            edgeInstability * average["EdgeInstability"];            // דפנות הקו
        pressure = Math.Clamp(pressure, 0.0, 100.0);

        // שקלול סטיית תקן של הלחץ
        double pressureStd =
            darknessStdPercent * standardDeviation["DarknessStd"] +
            widthStdPercent * standardDeviation["WidthStd"] +
            edgeInstability * standardDeviation["EdgeInstability"];
        pressureStd = Math.Clamp(pressureStd, 0.0, 100.0);

        return (pressure, pressureStd);
    }


//////////////////////////////////////////////////////
// שטף הכתיבה
    public static (double flowAvgPercent, double flowStdPercent) CalculateWritingFlowPercent(
        double continuityPercent,       // ממוצע רציפות
    
        double angleStabilityPercent,   // ממוצע יציבות זוויות
        double angleStdPercent,         // סטיית זוויות
    
        double spacingStabilityPercent, // ממוצע יציבות רווחים
        double spacingStdPercent,       // סטיית רווחים
    
        double lineStabilityPercent,    // ממוצע יציבות שורות
        double lineStdPercent,          // סטיית שורות
    
        double readabilityPercent,      // ממוצע קריאות

        double pressureStabilityPercent,// ממוצע יציבות לחץ
        double pressureStdPercent,      // סטיית לחץ
    
        double widthStabilityPercent,   // ממוצע יציבות עובי
        double widthStdPercent,         // סטיית עובי
        Dictionary<string, Dictionary<string, double>> weights)     // משקלי חשיבות
    {
        Dictionary<string, double> average = weights["Average"];
        Dictionary<string, double> standardDeviation = weights["StandardDeviation"];

        // ממוצע שטף הכתיבה
        double flowAvg =
        continuityPercent * average["Continuity"] +
        angleStabilityPercent * average["AngleStability"] +
        spacingStabilityPercent * average["SpacingStability"] +
        lineStabilityPercent * average["LineStability"] +
        readabilityPercent * average["Readability"] +
        pressureStabilityPercent * average["PressureStability"] +
        widthStabilityPercent * average["WidthStability"];

        double stdWeightsSum = standardDeviation.Values.Sum();
    
        // סטיית שטף הכתיבה
        double flowStd = 
        (   angleStdPercent * standardDeviation["AngleStd"] +
            spacingStdPercent * standardDeviation["SpacingStd"] +
            lineStdPercent * standardDeviation["LineStd"] +
            pressureStdPercent * standardDeviation["PressureStd"] +
            widthStdPercent * standardDeviation["WidthStd"]
        ) / stdWeightsSum;
    
        // הגבלה לטווח 0-100
        flowAvg = Math.Max(0, Math.Min(100, flowAvg));
        flowStd = Math.Max(0, Math.Min(100, flowStd));
    
        return (flowAvg, flowStd);
    }

//////////////////////////////////////////////////////
// תנועתיות
    public static (
        double movementAvgPercent,     // ממוצע תנועתיות כללי
        double movementStdPercent,     // סטיית תנועתיותה
        double energyPercent,          // עוצמת תנועה
        double sharpnessPercent,       // חדות תנועה
        double controlPercent,         // שליטה/ריסון
        double blockingPercent,        // עצירות
        double smearPercent,           // מריחה
        double periodicityPercent)     // מחזוריות
     CalculateMovementMetrics(
        double flowAvgPercent,         // ממוצע שטף הכתיבה באחוזים
        double flowStdPercent,         // סטיית תקן של שטף הכתיבה באחוזים

        double edgeStraightPercent,     // צורת הדפנות
        double edgeChangingPercent,
        double edgeWavyPercent,
        double edgeJaggedPercent,
        double edgeIrregularPercent,

        double pressurePercent,        // לחץ הכתיבה באחוזים
        double pressureStdPercent,     // סטיית תקן של לחץ הכתיבה באחוזים

        double widthPercent,           // עובי הקו הממוצע באחוזים
        double widthStdPercent,         // סטיית תקן של עובי הקו באחוזים
        
        Dictionary<string, Dictionary<string, double>> weights      // משקלים

    )
    {
        // משקלים
        Dictionary<string, double> energyWeights = weights["Energy"];
        Dictionary<string, double> sharpnessWeights = weights["Sharpness"];
        Dictionary<string, double> periodicityWeights = weights["Periodicity"];
        Dictionary<string, double> controlWeights = weights["Control"];
        Dictionary<string, double> blockingWeights = weights["Blocking"];
        Dictionary<string, double> smearWeights = weights["Smear"];
        Dictionary<string, double> averageWeights = weights["Average"];
        Dictionary<string, double> stdWeights = weights["StandardDeviation"];

        double energy =
        energyWeights["Pressure"] * pressurePercent +
        energyWeights["Flow"] * flowAvgPercent +
        energyWeights["Width"] * widthPercent +
        energyWeights["EdgeChanging"] * edgeChangingPercent;

    double sharpness =
        sharpnessWeights["EdgeJagged"] * edgeJaggedPercent +
        sharpnessWeights["EdgeChanging"] * edgeChangingPercent +
        sharpnessWeights["EdgeIrregular"] * edgeIrregularPercent;

    double periodicity =
        periodicityWeights["EdgeWavy"] * edgeWavyPercent +
        periodicityWeights["EdgeChanging"] * edgeChangingPercent;

    double control =
        controlWeights["EdgeStraight"] * edgeStraightPercent +
        controlWeights["Flow"] * flowAvgPercent +
        controlWeights["EdgeRegularity"] * (100.0 - edgeIrregularPercent) +
        controlWeights["PressureStability"] * (100.0 - pressureStdPercent);

    double blocking =
        blockingWeights["LowFlow"] * (100.0 - flowAvgPercent) +
        blockingWeights["Pressure"] * pressurePercent +
        blockingWeights["EdgeJagged"] * edgeJaggedPercent +
        blockingWeights["EdgeIrregular"] * edgeIrregularPercent;

    double smear =
        smearWeights["WidthStd"] * widthStdPercent +
        smearWeights["EdgeIrregular"] * edgeIrregularPercent +
        smearWeights["PressureStd"] * pressureStdPercent;

    double movementAvg =
        averageWeights["Energy"] * energy +
        averageWeights["Flow"] * flowAvgPercent +
        averageWeights["Sharpness"] * sharpness +
        averageWeights["Periodicity"] * periodicity +
        averageWeights["Control"] * control;

    double movementStd =
        stdWeights["FlowStd"] * flowStdPercent +
        stdWeights["PressureStd"] * pressureStdPercent +
        stdWeights["WidthStd"] * widthStdPercent +
        stdWeights["EdgeIrregular"] * edgeIrregularPercent;

    return (
        Math.Clamp(movementAvg, 0.0, 100.0),
        Math.Clamp(movementStd, 0.0, 100.0),
        Math.Clamp(energy, 0.0, 100.0),
        Math.Clamp(sharpness, 0.0, 100.0),
        Math.Clamp(control, 0.0, 100.0),
        Math.Clamp(blocking, 0.0, 100.0),
        Math.Clamp(smear, 0.0, 100.0),
        Math.Clamp(periodicity, 0.0, 100.0)
    );
    }

    private static double Clamp(double value)
    {
        return Math.Max(0, Math.Min(100, value));
    }

//////////////////////////////////////////////////////////
// מהירות הכתיבה
    public static (double speedAvgPercent, double speedStdPercent) CalculateWritingSpeed
    (
    double flowAvgPercent,          // ממוצע שטף
    double flowStdPercent,          // סטיית שטף

    double movementAvgPercent,      // ממוצע תנועתיות
    double movementStdPercent,      // סטיית תנועתיות

    double continuityPercent,       // רציפות כתיבה

    double simplifiedPercent,       // פישוט צורה

    double avgAngleChangePercent,   // ממוצע שינויי זווית
    double angleStdPercent,          // סטיית שינויי זווית

    Dictionary<string, Dictionary<string, double>> weights      // משקלים
    )
    {
        // משקלים
        Dictionary<string, double> average = weights["Average"];
        Dictionary<string, double> standardDeviation = weights["StandardDeviation"];

        // ממוצע מהירות כתיבה
        double speedAvg =
            flowAvgPercent * average["Flow"] +
            movementAvgPercent * average["Movement"] +
            continuityPercent * average["Continuity"] +
            simplifiedPercent * average["Simplified"] +
            (100 - avgAngleChangePercent) * average["AngleSmoothness"];

        double stdWeightsSum = standardDeviation.Values.Sum();

        // סטיית תקן מהירות לפי אותן יחסיות
        double speedStd =
        (   flowStdPercent * standardDeviation["FlowStd"] +
            movementStdPercent * standardDeviation["MovementStd"] +
            angleStdPercent * standardDeviation["AngleStd"]
        ) / stdWeightsSum;

        return (
            speedAvgPercent: Clamp(speedAvg),
            speedStdPercent: Clamp(speedStd)
        );
    }
}
}