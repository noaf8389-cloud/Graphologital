namespace Data_Extraction
{
public partial class Tis : Character
{
    // חישוב זווית הכתיבה
    public (double avg, double sq) angleWreating()
    {
        List<double> angles = this.SortedLines.SelectMany(line => line).Select(c => c.angleChar)
            .Where(a => !double.IsNaN(a) && !double.IsInfinity(a)).ToList();
        
        if (angles.Count == 0)
            return (0.0, 0.0);

        double avg = angles.Average();
        double sq = Math.Sqrt(angles.Average(angle => Math.Pow(angle - avg, 2)));
        return (avg, sq);
    }

    // שלושת איזורי הכתב ויחס תלת אזורי
    public (double cNeutral, double sq_neutral, double cMean, double cBase, double ThreeZone_ratio) writing_areas()
    {
        string neutralChars = "אבגדהוזחיכמםנסעפרשת";
        string meanChars = "טלףצץ";
        string baseChars = "ךןק";

        double sum_error_mean = 0, sum_error_base = 0, sum_neutral = 0;
        int count_mean = 0, count_base = 0, count_neutral = 0;

        foreach (List<Character> line in this.SortedLines)
        {
            // סינון השורה לאותיות האיזור האמצעי כדי לקבוע את האזור האמצעי של השורה
            List<Character> middle_chars = line.Where(c => neutralChars.Contains(c.PredictedClass.ToString() ?? "")).ToList();
            if (middle_chars.Count < 2) continue; // הגנה למקרה של שורה ריקה מדי
            
            // מציאת קו הרכס של השורה ע"י שיטת הריבועים הפחותים
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, x, y;
            foreach (Character c in middle_chars)
            {
                x = c.X;    
                y = c.Y;
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }
            int count = middle_chars.Count;

            double denom_mean = (count * sumX2) - (sumX * sumX);
            if (Math.Abs(denom_mean) < 0.001) continue;

            // חישוב השיפוע
            double m_mean = (count * sumXY - sumX * sumY) / denom_mean;
            // חיתוך עם ציר הY
            double b_mean = (sumY - m_mean * sumX) / count;

            sumX = 0; sumY = 0; sumXY = 0; sumX2 = 0;
            // מציאת קו הבסיס של השורה ע"י שיטת הריבועים הפחותים
            foreach (Character c in line)
            {
                x = c.X + c.W;
                y = c.Y +c.H;
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }
            int countLine = line.Count; // הגדרת הכמות הנכונה של אותיות השורה
            double denom_base = (countLine * sumX2) - (sumX * sumX);
            if (Math.Abs(denom_base) < 0.001) continue; // הגנה חיונית מחלוקה באפס שיוצרת NaN
            
            // חישוב השיפוע והחיתוך המתוקנים
            double m_base = (countLine * sumXY - sumX * sumY) / denom_base;
            double b_base = (sumY - m_base * sumX) / countLine;

            foreach (Character c in line)
            {
                x = c.X + c.W / 2;
                double pointY_mean = m_mean * x + b_mean; // ערך הנקודה שעל קו הרכס במיקום התו
                double pointY_base = m_base * x + b_base; // ערך הנקודה שעל קו הבסיס במיקום של התו
                double h_neutral = pointY_base - pointY_mean; // המרחק בין הקווים במיקום התו
                if (Math.Abs(h_neutral) < 1.0) h_neutral = 1.0; // הגנה למקרה של חלוקה ב-0

                // חישוב מרחק מקו הרכס
                if (meanChars.Contains(c.PredictedClass.ToString() ?? ""))
                {
                    y = c.Y;
                    double error_mean = Math.Abs(y - pointY_mean); // מרחק מהקו   
                    sum_error_mean += error_mean/ h_neutral; // חישוב יחס החלק העילי של האות יחסית למרכזה
                    count_mean++;               
                }
                // חישוב מרחק מקו הבסיס
                else if (baseChars.Contains(c.PredictedClass.ToString() ?? ""))
                {
                    y = c.Y + c.H;
                    double error_base = Math.Abs(y - pointY_base); // מרחק מהקו   
                    sum_error_base += error_base/ h_neutral; // חישוב יחס החלק התחתי של האות יחסית למרכזה
                    count_base++;             
                }
                else
                {
                    sum_neutral += c.H;
                    count_neutral++;
                }
            }
        }
        // הגובה הממוצע של אזורי הכתב
        // הגובה הממוצע של אזורי הכתב - עם הגנה מפני חלוקה באפס
        double avg_neutral = (count_neutral > 0) ? sum_neutral / count_neutral : 0;
        double avg_mean = (count_mean > 0) ? sum_error_mean / count_mean : 0;
        double avg_base = (count_base > 0) ? sum_error_base / count_base : 0;

        // סטיית תקן של האיזור האמצעי
        double sum = 0;
        foreach(List<Character> line in this.SortedLines)
        {
            foreach (Character c in line)
            {
                if (neutralChars.Contains(c.PredictedClass.ToString()))
                {
                    sum += Math.Pow(c.H - avg_neutral,2);
                }
            }
        }
        double sq_neutral = count_neutral > 0 ? Math.Sqrt(sum / count_neutral) : 0;        
        // חישוב היחס בין האזורים
        double ThreeZone_ratio = (avg_mean + avg_base) / 2.0;
        return (avg_neutral, sq_neutral, avg_mean, avg_base, ThreeZone_ratio);
    }

    // קריאות הכתיבה
    public double read()
    {
        // ממוצע
        double sum = 0;
        int count = 0;
        foreach (List<Character> line in this.SortedLines)
        {
            foreach (Character c in line)
            {
                sum += c.Confidence;
                count++;
            }
        }
        return sum/ count;
    }

    // רוחב הכתיבה
    public double wength()
    {
        double sum = 0;
        int count = 0;
        foreach (List<Character> line in this.SortedLines)
        {
            foreach (Character c in line)
            {
                sum += c.W;
                count++;
            }
        }
        return sum/ count;
    }

    // יחסי אורך - רוחב
    public (double avg, double sq) Geometric_shape()
    {
        // ממוצע
        double sum = 0;
        int count = 0;
        string neutralChars = "אבגדהוזחיכמםנסעפרשת";

        foreach (List<Character> line in this.SortedLines)
        {
            foreach(Character c in line)
            {
                if (neutralChars.Contains(c.PredictedClass.ToString()))
                {
                    sum += c.H / c.W;
                    count++;                    
                }
            }
        }
        double avg = sum/ count;
        // סטיית תקן
        sum = 0;
        foreach(List<Character> line in this.SortedLines)
        {
            foreach (Character c in line)
            {
                if (neutralChars.Contains(c.PredictedClass.ToString()))
                {
                    sum += Math.Pow((c.H / c.W)-avg ,2);
                }
            }
        }
        double sq = Math.Sqrt(sum/ count); 
        return (avg,sq);
    }

// תקן, פישוט, קישוט, העשרה

    public static (
    double standardPercent,     // עד כמה הכתב תקני/ברור
    double simplifiedPercent,   // עד כמה הכתב מופשט/פשוט
    double decoratedPercent,    // עד כמה יש קישוטים
    double enrichedPercent      // עד כמה הצורה מועשרת
    ) CalculateShapeStyleMetrics(
        double avgAngleChangePercent,  // ממוצע שינויי זווית
        double stdAngleChangePercent,  // סטיית תקן שינויי זווית
        double sharpCornersPercent,    // אחוז פינות חדות
        double waveRatioPercent,       // אחוז תנודות חוזרות
        double readabilityPercent,     // קריאות הכתב
        double widthStdPercent,         // סטיית תקן של עובי הקו
        Dictionary<string, Dictionary<string, double>> weights      // משקלים
    )
    {
        // משקלים
        Dictionary<string, double> complexityWeights = weights["Complexity"];
        Dictionary<string, double> decoratedWeights = weights["Decorated"];
        Dictionary<string, double> enrichedWeights = weights["Enriched"];
        Dictionary<string, double> standardWeights = weights["Standard"];

        // מורכבות צורנית: הרבה זוויות, תנודות ושונות = צורה עשירה יותר
        double complexity =
            avgAngleChangePercent * complexityWeights["AverageAngleChange"] +
            stdAngleChangePercent * complexityWeights["AngleChangeStd"] +
            sharpCornersPercent * complexityWeights["SharpCorners"] +
            waveRatioPercent * complexityWeights["WaveRatio"] +
            widthStdPercent * complexityWeights["WidthStd"];

        // פישוט - ההפך ממורכבות
        double simplified = 100 - complexity;

        // קישוט: בעיקר תנודות, עיקולים ושינויי צורה
        double decorated =
            waveRatioPercent * decoratedWeights["WaveRatio"] +
            avgAngleChangePercent * decoratedWeights["AverageAngleChange"] +
            stdAngleChangePercent * decoratedWeights["AngleChangeStd"] +
            widthStdPercent * decoratedWeights["WidthStd"];

        // העשרה: דומה לקישוט, אבל כוללת גם מורכבות כללית
        double enriched =
            complexity * enrichedWeights["Complexity"] +
            decorated * enrichedWeights["Decorated"];

        // תקן: קריאות גבוהה + יציבות + לא יותר מדי חדות/מורכבות
        double standard =
            readabilityPercent * standardWeights["Readability"] +
            (100 - stdAngleChangePercent) * standardWeights["AngleStability"] +
            (100 - sharpCornersPercent) * standardWeights["CornerSmoothness"] +
            (100 - widthStdPercent) * standardWeights["WidthStability"];

        return (
            standardPercent: Clamp(standard),
            simplifiedPercent: Clamp(simplified),
            decoratedPercent: Clamp(decorated),
            enrichedPercent: Clamp(enriched)
        );
    }


// 4 צורות הקו
    public static (
        double straightPercent,   // מדד קו ישר: יציב, פשוט, מעט שינויי כיוון
        double convexPercent,     // מדד קו קמור: רחב, עגול, פתוח
        double concavePercent,    // מדד קו קעור: צר, מכונס, פחות פתוח
        double threadlikePercent  // מדד קו חוטי: דק, רציף, פשוט וזורם
    ) CalculateLineShapeMetrics(

        double avgAngleChangePercent,   // ממוצע שינויי זווית בקונטור: גבוה = יותר עיקולים
        double stdAngleChangePercent,   // סטיית תקן של שינויי הזווית: גבוה = פחות יציב
        double sharpCornersPercent,     // אחוז פינות חדות: גבוה = יותר שבירות/זוויות חדות
        double waveRatioPercent,        // אחוז תנודות חוזרות: גבוה = יותר גליות/מחזוריות

        double widthPercent,            // עובי קו ממוצע באחוזים
        double widthStdPercent,         // סטיית תקן של עובי הקו: גבוה = עובי משתנה

        double writingWidthPercent,     // רוחב הכתיבה: גבוה = כתב רחב, נמוך = כתב צר
        double heightWidthRatioPercent, // יחס אורך-רוחב: גבוה = צורות מאורכות/פתוחות יותר

        double continuityPercent,       // רציפות הכתיבה: גבוה = פחות קיטועים
        double simplifiedPercent,       // פישוט הצורה: גבוה = צורה פשוטה/מופשטת
        double flowAvgPercent,          // שטף כתיבה ממוצע: גבוה = כתיבה זורמת יותר

        double edgeStraightScore,       // סוג דפנות הקו
        double edgeWavyScore,
        double edgeJaggedScore,
        double edgeIrregularScore,

        Dictionary<string, Dictionary<string, double>> weights      // משקלים
    )
    {
        Dictionary<string, double> straightWeights = weights["Straight"];
        Dictionary<string, double> roundnessWeights = weights["Roundness"];
        Dictionary<string, double> convexWeights = weights["Convex"];
        Dictionary<string, double> concaveWeights = weights["Concave"];
        Dictionary<string, double> threadlikeWeights = weights["Threadlike"];
        Dictionary<string, double> edgeBonus = weights["EdgeBonus"];

        // ישר:
        // מעט שינויי זווית + סטייה נמוכה + מעט פינות חדות
        double straight =
            (100 - avgAngleChangePercent) *  straightWeights["CornerSmoothness"] +
            (100 - stdAngleChangePercent) *  straightWeights["AngleStability"] +
            (100 - sharpCornersPercent) *  straightWeights["CornerSmoothness"] +
            simplifiedPercent * straightWeights["Simplified"];

        // קמור:
        // כתב רחב/פתוח + עגלוליות + מעט פינות חדות
        double roundness =
            avgAngleChangePercent * roundnessWeights["AverageAngleChange"] +
            (100 - sharpCornersPercent) * roundnessWeights["CornerSmoothness"];

        double convex =
            writingWidthPercent * convexWeights["WritingWidth"] +
            roundness * convexWeights["Roundness"] +
            heightWidthRatioPercent * convexWeights["HeightWidthRatio"] +
            waveRatioPercent * convexWeights["WaveRatio"] +
            (100 - stdAngleChangePercent) * convexWeights["AngleStability"];

        // קעור:
        // כתב צר/מכונס + יותר חוסר יציבות/חדות
        double concave =
            (100 - writingWidthPercent) * concaveWeights["NarrowWriting"] +
            (100 - heightWidthRatioPercent) * concaveWeights["LowHeightWidthRatio"] +
            stdAngleChangePercent * concaveWeights["AngleChangeStd"] +
            sharpCornersPercent * concaveWeights["SharpCorners"] +
            (100 - flowAvgPercent) * concaveWeights["LowFlow"];

        // חוטי:
        // דק + רציף + פשוט + זורם + עובי יציב
        double threadlike =
            (100 - widthPercent) * threadlikeWeights["ThinWidth"] +
            (100 - widthStdPercent) * threadlikeWeights["WidthStability"] +
            continuityPercent * threadlikeWeights["Continuity"] +
            simplifiedPercent * threadlikeWeights["Simplified"] +
            flowAvgPercent * threadlikeWeights["Flow"];

        // תיקון קטן לפי דפנות הקו
        straight += edgeStraightScore  * edgeBonus["Straight"];  // עד 8 נקודות בונוס
        convex   += edgeWavyScore      * edgeBonus["Wavy"];
        concave  += edgeJaggedScore    * edgeBonus["Jagged"] + edgeIrregularScore * edgeBonus["Irregular"];

        return (
            straightPercent: Clamp(straight),
            convexPercent: Clamp(convex),
            concavePercent: Clamp(concave),
            threadlikePercent: Clamp(threadlike)
        );
    }
    
}
}