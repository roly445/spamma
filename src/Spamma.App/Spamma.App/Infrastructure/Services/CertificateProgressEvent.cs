namespace Spamma.App.Infrastructure.Services;

/// <summary>
/// Represents a certificate generation progress event.
/// </summary>
public sealed class CertificateProgressEvent
{
    /// <summary>
    /// Gets the current step number (1-based).
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Gets the total number of steps.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage => (this.Step * 100) / this.TotalSteps;

    /// <summary>
    /// Gets the step message describing what is currently happening.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the certificate generation is complete.
    /// </summary>
    public bool IsComplete => this.Step >= this.TotalSteps;
}