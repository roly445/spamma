namespace Spamma.Modules.Common;

public class CampaignCaptureConfig
{
    public string HeaderName { get; init; } = "x-spamma-camp";

    public int SampleRetentionDays { get; init; } = 30;

    public int MaxHeaderLength { get; init; } = 255;

    public bool SampleStorageEnabled { get; init; } = true;

    public int SynchronousExportRowLimit { get; init; } = 10000;
}