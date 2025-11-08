namespace Spamma.App.Infrastructure.Services;

public sealed class CertificateProgressEvent
{
    public int Step { get; set; }

    public int TotalSteps { get; set; }

    public int ProgressPercentage => (this.Step * 100) / this.TotalSteps;
    public string Message { get; set; } = string.Empty;
    public bool IsComplete => this.Step >= this.TotalSteps;
}