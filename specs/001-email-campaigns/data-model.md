# data-model.md

Entities:

- CampaignSummary
  - CampaignId: Guid
  - SubdomainId: Guid
  - CampaignValue: string (normalized lower-case)
  - FirstReceivedAt: DateTimeOffset
  - LastReceivedAt: DateTimeOffset
  - TotalCaptured: int

- CampaignSample
  - CampaignId: Guid
  - MessageId: Guid
  - Subject: string
  - From: string
  - To: string
  - ReceivedAt: DateTimeOffset
  - StoredAt: DateTimeOffset
  - ContentPreview: string (sanitized, truncated)

- CampaignCaptureConfig
  - HeaderName: string (default `x-spamma-camp`)
  - SampleRetentionDays: int (default 30)
  - MaxHeaderLength: int (255)
  - SampleStorageEnabled: bool
  - SynchronousExportRowLimit: int (default 10000)

Indexes and uniqueness:
- Unique index on (SubdomainId, CampaignValue)
- Query index on LastReceivedAt for sorting

State transitions:
- CampaignSummary created when first captured; updated on subsequent captures (LastReceivedAt, TotalCaptured)
- CampaignSample created only on first capture (immutable unless operator reconfigures)

Validation rules:
- CampaignValue length <= 255, printable characters only after sanitization
