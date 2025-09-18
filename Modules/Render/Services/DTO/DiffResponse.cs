namespace RenderModule.Services.DTO;

public sealed class DiffResponse
{
    public string Mode { get; set; } = "pixel";
    public double Score { get; set; } // 1.0 identical; lower = more different (placeholder metric)
    public string? DiffImagePath { get; set; } // when pixel diff succeeds
    public string ReportPath { get; set; } = ""; // JSON report path you can download/show
    public string Summary { get; set; } = "";
}