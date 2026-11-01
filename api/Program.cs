using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Data_Extraction;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("constants.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile("graphology_formula_weights.json", optional: false, reloadOnChange: false);

builder.Configuration.AddUserSecrets<Program>(optional: false);

string jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key. Run: dotnet user-secrets set \"Jwt:Key\" \"<at least 32 characters>\"");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
}

builder.Services.AddControllers();

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<GraphologyAnalysisService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddDataProtection().SetApplicationName("Graphologital");
builder.Services.AddSingleton<EncryptionService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<GraphologyConstantsOptions>(builder.Configuration.GetSection("GraphologyConstants"));
builder.Services.Configure<GraphologyFormulaWeightsOptions>(builder.Configuration.GetSection("GraphologyFormulaWeights"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();








// using System;
// using Data_Extraction;



// // הרצת החישובים והדפסה
// var (leftAvg, leftSq) = tis.leftMargin();
// var (rightAvg, rightSq) = tis.rightMardin();
// double bottom = tis.BottomMargin();
// double top = tis.topMargin();
// var (countChar, avgW, sqW, avgC, dsqC, avgSpaceC) = tis.spaces(); // כולל כמות התווים הכוללת
// var (avgAngle, sqAngle) = tis.angleWreating();
// var (line_angle, line_angle_stdDev, angle_signChanges, avg_word_slope) = tis.Row_direction(avgSpaceC);
// var (cNeutral,sq_neutral, cMean, cBase, ThreeZone_ratio) = tis.writing_areas();
// double read = tis.read();
// var (geometric_shapeAvg, geometric_shapeSQ) = tis.Geometric_shape();
// double wength = tis.wength();
// var (avg_pressur, sq_pressur, avgEdges_pressur) = tis.writing_pressure();
// var (avgChange, stdChange, sharpRatio, waveRatio, contours_count) = tis.EdgesByContours();
// var (avgDarkness, sqDarkness) = tis.Darkness();

// /// לחץ הכתיבה - לחשב קודם את כל הנתונים ששולחים
// // var (a, s) = tis.WritingPressure(
// //     darkness,      // כהות הכתב באחוזים
// //     darknessStdPercent,      // סטיית תקן כהות
// //     widthAvgPercent,         // עובי כתב ממוצע באחוזים
// //     widthStdPercent,         // סטיית תקן של עובי הכתב באחוזים
// //     edgeWidthPercent,        // עובי קצוות האותיות באחוזים
// //     edgeType);              // סוג דפנות הקו

// // חישוב רציפות הכתב עבור בדיקת שטף הכתיבה
// double Line_continuity = contours_count/ countChar;


// Console.WriteLine($"margin: left: (avg:{leftAvg}, sqrt:{leftSq}), right: (avg:{rightAvg}, sqrt:{rightSq}), tottom: {bottom}, top: {top}");
// Console.WriteLine($"spaces: avgW, sqW, avgC, dsqC= {avgW}, {sqW}, {avgC}, {dsqC}");
// Console.WriteLine($"angle: Avg: {avgAngle}, sq: {sqAngle}");
// Console.WriteLine($"line_angle: Avg: {line_angle}, sq: {line_angle_stdDev}, signChanges:{angle_signChanges}");
// Console.WriteLine($"writing areas: neutral: avg: {cNeutral}, sq: {sq_neutral} mean:{cMean}, base:{cBase}. ThreeZone_ratio:{ThreeZone_ratio}");
// Console.WriteLine($"read: {read}");
// Console.WriteLine($"geometric_shape: avg:{geometric_shapeAvg}, sq:{geometric_shapeSQ}");
// Console.WriteLine($"wength: {wength}");
// Console.WriteLine($"writing_pressure: avg_pressur-{avg_pressur}, sq_pressur-{sq_pressur}, avgEdges_pressur-{avgEdges_pressur}");
// Console.WriteLine($"avgChange, stdChange, sharpRatio, waveRatio: {avgChange}, {stdChange}, {sharpRatio}, {waveRatio}, contours_count: {contours_count}");
// Console.WriteLine($"Darkness: avgDarkness:{avgDarkness}, sqDarkness:{sqDarkness}");
// // if (myTis != null)
// // {
// //     Console.WriteLine($"מימדי התמונה הראשית שנשמרה באובייקט: {pythonReceivedData.Img.Width}x{pythonReceivedData.Img.Height}");
// // }

// Tis tis = await Tis.CreateAsync(@"User\Desktop\ה-פרויקט\data set\14996257\AgeAnnotations_and_Split\train\w2_M_3_form40.tif");

// var metrics = new GraphologyMetrics(tis);
// var (type, diagnosis) = metrics.GetPersonalityDiagnosis();

// Console.WriteLine(type);
// Console.WriteLine(diagnosis);
