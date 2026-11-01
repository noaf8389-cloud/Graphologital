using SixLabors.ImageSharp.Formats.Tiff.Compression.Decompressors;

namespace Data_Extraction
{
public partial class Tis : Character
{

    // שוליים עליונים
    public double topMargin()
    {
        List<Character> line = this.SortedLines[0];
        double sum = 0;
        foreach (Character c in line) { sum += c.Y; }
        return sum / line.Count;
    }

    // שוליים תחתונים
    public double BottomMargin()
    {
        List<Character> line = this.SortedLines[this.SortedLines.Count - 1];
        double sum = 0;
        foreach (Character c in line) { sum += c.Y + c.H; }
        return this.Img.Height - (sum / line.Count);
    }

    // שוליים ימניים
    public (double avg, double sq) rightMardin()
    {
        List<List<Character>> data = this.SortedLines;
        if (data == null || data.Count == 0) return (0, 0);
        // ממוצע
        double sum = 0;
        foreach (List<Character> line in data) // התו הראשון בכל שורה
        {
            sum += Math.Abs(line[0].X + line[0].W);
        } 
        double avg = sum / data.Count;
        //סטיית תקן
        sum = 0;
        foreach (List<Character> line in data)
        {
            sum += Math.Pow(line[0].X + line[0].W - avg, 2);
        }
        double sq = Math.Sqrt(sum / data.Count);
        return (avg, sq);
    }

    // שוליים שמאליים
    public (double avg, double sq) leftMargin()
    {
        // ממוצע
        double sum = 0;
        foreach (List<Character> line in this.SortedLines) { sum += line[line.Count - 1].X; }
        double avg = sum / this.SortedLines.Count;
        //סטיית תקן
        sum = 0;
        foreach (List<Character> line in this.SortedLines)
        {
            sum += Math.Pow(line[line.Count - 1].X - avg, 2);
        }
        double sq = Math.Sqrt(sum / this.SortedLines.Count);
        return (avg, sq);
    }

    // רווחים בין מילים ואותיות
    public (int countChar, double avgW, double sqW, double avgC, double sqC, double avg) spaces()
    {
        double sum = 0;
        int count = 0;
        // ממוצע הרווחים בין התווים
        foreach(List<Character> line in this.SortedLines)
        {
            for (int i=1; i<line.Count; i++)
            {
                sum += line[i-1].X - line[i].X - line[i].W;
            }
            count += line.Count;
        }
        
        int spacesCount = 0;
        foreach(var line in SortedLines)
            spacesCount += Math.Max(0, line.Count - 1);
        double avg = spacesCount > 0 ? sum / spacesCount : 0;
        
        // מציאת ממוצע הרווחים בין האותיות ובין המילים
        double sumC = 0, sumW = 0, space;
        int countC = 0, countW = 0;
        foreach(List<Character> line in this.SortedLines)
        {
            for (int i=1; i<line.Count; i++)
            {
                space = line[i-1].X - line[i].X - line[i].W;
                if (space > avg)
                {
                    sumW += space;
                    countW++;
                }
                else
                {
                    sumC += space;
                    countC++;
                }
            }
        }
        double avgC = countC > 0 ? sumC / countC : 0;
        double avgW = countW > 0 ? sumW / countW : 0;
        sumC = 0; sumW = 0;;
        // מציאת סטיית תקן של הרווחים בין האותיות ובין המילים
        foreach(List<Character> line in this.SortedLines)
        {
            for (int i=1; i<line.Count; i++)
            {
                space = line[i-1].X - line[i].X - line[i].W;
                if (space > avg)
                {
                    sumW += Math.Pow(space - avgW, 2);
                }
                else
                {
                    sumC += Math.Pow(space- avgC, 2);
                }
            }
        }
        double sqW = countW > 0 ? Math.Sqrt(sumW / countW) : 0; 
        double sqC = countC > 0 ? Math.Sqrt(sumC / countC) : 0;
        return (count, avgW, sqW, avgC, sqC, avg);
    }

    // רווחים בין שורות
    public (double avg, double sq) lineSpace()
    {
        // ממוצע
        double sum, spane = 0;
        List<double> list = new List<double>();
        for (int i=1; i<this.SortedLines.Count; i++)
        {
            // חישוב ממוצע החלק התחתון של השורה הקודמת
            sum = 0;
            foreach (Character c in this.SortedLines[i-1])
            {
                sum += c.Y + c.H;
            }
            double avgTop = sum/ this.SortedLines[i-1].Count;
            // חישוב ממוצע החלק העליון של השורה הנוכחית
            sum = 0;
            foreach (Character c in this.SortedLines[i])
            {
                sum += c.Y;
            }
            double avgBotton = sum/ this.SortedLines[i].Count;
            double gap = avgBotton - avgTop;
            spane += gap;
            list.Add(gap);
        }
        double avgSpace = spane/ (this.SortedLines.Count-1);

        // סטיית תקן
        sum = 0;
        foreach (double l in list)
        {
            sum += Math.Pow(l - avgSpace, 2);
        }
        double sqSpace = Math.Sqrt(sum / (this.SortedLines.Count-1));
        return (avgSpace, sqSpace);
    }

    // כיוון השורות
    public (double avg_angle, double avg_stdDev, double avg_signChanges, double avg_word_slope) Row_direction(double space_words)
    {
        if (this.SortedLines == null || this.SortedLines.Count == 0) return (0, 0, 0, 0);
        List<Character> word = new List<Character>();
        double sum_line_angle = 0, sum_line_stdDev = 0, sum_line_signChanges = 0;
        double sum_all_words_slope = 0; // צבירת שיפועי המילים לכל הדף
        int total_words_count = 0;      // מונה מילים כללי לכל הדף
        int validLines = 0;
        foreach (List<Character> line in this.SortedLines)
        {
            // הגנה, אם יש פחות מ-2 אותיות בשורה
            if (line.Count < 2) continue;
            validLines++;
            
            // זימון פונקציה למציאת קו הישר (שיפוע ונק' חיתוך)
            var (m, b) = least_squares(line);

            // חישוב זווית השורה במעלות לפי השיפוע
            double angle = Math.Atan(m) * (180.0 / Math.PI);
            sum_line_angle += angle;
            
            // חישוב סטיית תקן וזיהוי גַלִיוּת (חיתוך קו הישר)
            double sumSquaredErrors = 0; // סכום ריבועי המרחקים
            int signChanges = 0; // מספר הפעמים שהכתב חצה את קו הישר שנמצא
            double lastError = 0; // מרחק האות הקודמת
            foreach (Character c in line)
            {
                double x = c.X + c.W / 2.0;
                double y = c.Y + c.H / 2.0;
                double pointY = m * x + b; // ערך הנקודה שעל הישר במיקום של התו
                double error = y - pointY; // מרחק התו מהקו
                sumSquaredErrors += error * error;

                // בדיקת גַלִיוּת: האם האות חצתה את קו הישר לעומת האות הקודמת
                if (lastError != 0 && Math.Sign(error) != Math.Sign(lastError))
                {
                    signChanges++;
                }
                lastError = error;
            }

            // חישוב סטיית התקן
            double stdDev = Math.Sqrt(sumSquaredErrors / line.Count);
            sum_line_stdDev += stdDev;

            // חישוב הגַלִיוּת
            sum_line_signChanges += signChanges;

            // חישוב נטיה של כל מילה
            int i = 0;
            while (i < line.Count)            
            {
                word.Add(line[i]);
                while (i + 1 < line.Count && (line[i].X - (line[i+1].X + line[i+1].W)) < space_words)
                {
                    word.Add(line[i+1]);
                    i++;
                }
                // זימון פונקציה שמוצאת את נטיית המילה
                if (word.Count >= 2)
                {
                    (m, b) = least_squares(word);
                    double slopeWord = Math.Atan(m) * (180.0 / Math.PI);
                    sum_all_words_slope += slopeWord;
                    total_words_count++;
                }
                word.Clear();
                i++;
            }
        }
        double avg_angle = validLines > 0 ? sum_line_angle / validLines : 0.0;
        double avg_stdDev = validLines > 0 ? sum_line_stdDev / validLines : 0.0;
        double avg_signChanges = validLines > 0 ? sum_line_signChanges / validLines : 0.0;
        double avg_word_slope = total_words_count > 0 ? sum_all_words_slope / total_words_count : 0;
        return (avg_angle, avg_stdDev, avg_signChanges, avg_word_slope);
    }

    // פונקציית עזר למציאת קו הישר שמרחקו מכל הנקודות מינימלי על ידי שיטת הריבועים הפחותים
    public static (double m, double b) least_squares(List<Character> line)
    {
        int count = line.Count;
        if (count < 2) return (0, 0);
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, x, y;
        foreach (Character c in line)
        {
            x = c.X + c.W/2;
            y = c.Y + c.H/2;
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }
        double denominator = (count * sumX2) - (sumX * sumX);
        if (Math.Abs(denominator) < 0.0001) return (0, sumY / count); // הגנה במקרה וכל האותיות באותו X בדיוק

        // חישוב השיפוע והחיתוך
        double m = (count * sumXY - sumX * sumY) / denominator;
        double b = (sumY - m * sumX) / count;
        return (m, b);
    }
}
}