namespace DesignModule.Services.DTO;

public sealed class ApproveRequest
{
    public string Reviewer { get; set; } = "reviewer";
    public string SignatureHash { get; set; } = ""; // hash/signature of the diff/report/package
}