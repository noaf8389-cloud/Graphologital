using System.Collections.Generic;
namespace Data_Extraction;

public sealed class GraphologyConstantsOptions
{
    public SyndromeConstants Syndromes { get; set; } = new();
    public ContourConstants Contours { get; set; } = new();
    public ImageProcessingConstants ImageProcessing { get; set; } = new();
    public PythonServiceConstants PythonService { get; set; } = new();
}

public sealed class SyndromeConstants
{
    public double StrongSignScore { get; set; }

    public double StrictSignScoreAddition { get; set; }

    public double StrictSyndromeScoreAddition { get; set; }

    public double DefaultContinuityPercent { get; set; }
}

public sealed class ContourConstants
{
    public int MaximumContours { get; set; } = 30;
    public int MinimumPoints { get; set; } = 20;
    public int PointStep { get; set; } = 3;
    public double SharpCornerAngle { get; set; } = 140.0;
    public double AverageChangeDivisor { get; set; } = 180.0;
    public double StandardDeviationDivisor { get; set; } = 90.0;
    public int MinimumCheckedPoints { get; set; } = 10;
    public int TraceMaximumSteps { get; set; } = 3000;
}

public sealed class ImageProcessingConstants
{
    public byte BlackPixelThreshold { get; set; } = 128;
    public float BinaryThreshold { get; set; } = 0.5f;
    public int EdgeWindowRadius { get; set; } = 2;
    public double DarknessStandardDeviationDivisor { get; set; } = 128.0;
}

public sealed class PythonServiceConstants
{
    public string ApiUrl { get; set; } =
        "http://127.0.0.1:8000/analyze";

    public int TimeoutMinutes { get; set; } = 5;
}

// משקלים

public sealed class GraphologyFormulaWeightsOptions
{
    public Dictionary<string, Dictionary<string, double>>
        WritingPressure { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        WritingFlow { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        DerivedMetrics { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        Movement { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        WritingSpeed { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        Darkness { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        ContourEdges { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        ShapeStyle { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>>
        LineShape { get; set; } = new();
}