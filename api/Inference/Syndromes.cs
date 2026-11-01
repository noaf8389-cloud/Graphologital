using System.Collections.Generic;

namespace Data_Extraction
{
    // הגדרה של סינדרום אחד
    public sealed class SyndromeDefinition
    {
        // שם התכונה להצגה
        public string Trait { get; set; } = string.Empty;

        // טקסט האבחון שיוצג למשתמש
        public string Diagnosis { get; set; } = string.Empty;

        // מספר מינימלי של סימנים שחייבים להתאים
        public int MinimumMatchedSigns { get; set; } = 3;

        // מספר מינימלי של תחומי כתב שונים
        public int MinimumIndependentDomains { get; set; } = 2;

        // ציון מינימלי להצגת הסינדרום
        public double MinimumScore { get; set; } = 70.0;

        // סימני הכתב המרכיבים את הסינדרום
        public List<SyndromeSign> Signs { get; set; } = new();
    }

    // סימן כתב אחד בתוך סינדרום
    public sealed class SyndromeSign
    {
        public string Metric { get; set; } = string.Empty;
    
        public string Label { get; set; } = string.Empty;
    
        public double Weight { get; set; } = 1.0;
    
        public string Domain { get; set; } = string.Empty;
    }
}