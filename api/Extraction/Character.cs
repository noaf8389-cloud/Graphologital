using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using MathNet.Numerics.LinearAlgebra;

namespace Data_Extraction
{
// Character class
public class Character
{
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    public double Confidence { get; set; }
    public char? PredictedClass { get; set; }
    public Image<Rgba32>? CharImg { get; set; }
    public double angleChar { get; set; }
    // פעולה בונה ריקה
    public Character() { }
    // פעולה בונה מלאה
    public Character(double x, double y, double w, double h, double conf, char? pClass, Image<Rgba32>? sourceParent)
    {
        this.X = x;
        this.Y = y;
        this.W = w;
        this.H = h;
        this.Confidence = conf;
        this.PredictedClass = pClass;
        try
        {
            // הגדרת מלבן החיתוך לפי נתוני התו
            var cropRect = new Rectangle((int)Math.Round(X), (int)Math.Round(Y), (int)Math.Round(W), (int)Math.Round(H));
            // גזירה ושמירה לתוך האובייקט של התו
            this.CharImg = sourceParent.Clone(x => x.Crop(cropRect));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"נכשל חיתוך האות מהתמונה המקורית בקורדינטות: X={X}, Y={Y}, W={W}, H={H}.", ex);
        }

            
        if (this.CharImg == null)
        {
            throw new InvalidOperationException("יצירת תמונת האות נכשלה והאובייקט נותר null.");
        }
        
        this.angleChar = this.angle();
    }

    // פונקציה לחישוב זווית האות
    // על ידי PCA - מציאת הוקטור העצמי ובדיקת הנטיה שלו
    public double angle()
    {
        // איסוף הפיקסלים השחורים למטריצה
        List<double[]> pixelList = new List<double[]>();
        for (int y=0; y<this.H; y++)
        {
            for (int x=0; x<this.W; x++)
            {
                if (this.CharImg?[x, y].R < 128)
                {
                    pixelList.Add(new double[]{x,y});
                }
            }
        }
        // המרת רשימת המערכים למטריצה
        if (pixelList.Count < 2) return 90.0;
        var matrix = Matrix<double>.Build.DenseOfRowArrays(pixelList.ToArray());
        // מירכוז הנתונים לראשית
        // על ידי החסרת ממוצע ערכי X,Y מכל הנקודות
        double meanX = matrix.Column(0).Average();
        double meanY = matrix.Column(1).Average();
        // יצירת ווקטור ממוצעים וחיסורו מכל שורה במטריצה
        var meanVector = Vector<double>.Build.Dense(new[] { meanX, meanY });
        for (int i = 0; i < matrix.RowCount; i++)
        {
            matrix.SetRow(i, matrix.Row(i) - meanVector);
        }
        // חישוב מטריצת השונות המשותפת
        // החישוב מתבצע ע"י הכפלת המטריצה הממורכזת בטרנספורז שלה, וחילוק במס' הדגימות פחות 1
        var covarianceMatrix = matrix.TransposeThisAndMultiply(matrix).Divide(matrix.RowCount-1);
        // פירוק המטריצה לערכים ווקטורים עצמיים
        var evd = covarianceMatrix.Evd();
        var eigenValues = evd.EigenValues.Real(); 
        int maxIndex = eigenValues[0] >= eigenValues[1] ? 0 : 1;
        var principalVector = evd.EigenVectors.Column(maxIndex);
        double angle1 = Math.Atan2(principalVector[1], principalVector[0]) * (180.0 / Math.PI);
        // חישוב זווית הוקטור העצמי השני - הזווית המאונכת לזו של הוקטור העצמי הראשי
        double angle2 = angle1 + 90;
        // נרמול הזוויות לטווח של 0-180 מעלות
        double norm1 = ((angle1 % 180) + 180) % 180;
        double norm2 = ((angle2 % 180) + 180) % 180;
        // בחירת הזווית הזקופה שמייצגת את אורך האות
        return Math.Abs(norm1 - 90) < Math.Abs(norm2 - 90) ? norm1 : norm2;
    }
}
}