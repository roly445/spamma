# Audit Event Schemas for Email Campaigns

This document lists suggested audit event shapes to be emitted by the capture, export and delete flows.

1) capture.receipt
{
  "EventType": "capture.receipt",
  "MessageId": "<guid>",
  "SubdomainId": "<guid>",
  "CampaignValue": "<string>",
  "ReceivedAt": "<ISO-8601>",
  "HandledBy": "<service/instance>",
  "Result": "Accepted|Failed",
  "FailureReason": "<nullable>"
}

2) export.requested
{
  "EventType": "export.requested",
  "JobId": "<string>",
  "RequestedBy": "<user>",
  "SubdomainId": "<guid>",
  "CampaignValue": "<string>",
  "Format": "csv|json",
  "RequestedAt": "<ISO-8601>"
}

3) export.completed
{
  "EventType": "export.completed",
  "JobId": "<string>",
  "DownloadUrl": "<uri|null>",
  "CompletedAt": "<ISO-8601>",
  "Rows": <int>
}

4) campaign.delete
{
  "EventType": "campaign.delete",
  "Actor": "<user>",
  "SubdomainId": "<guid>",
  "CampaignValue": "<string>",
  "Mode": "soft|hard",
  "Timestamp": "<ISO-8601>"
}

Capture timeout recommendation:
- During SMTP ingestion the system should take a conservative synchronous receipt action (write minimal receipt/audit) and return SMTP response within a short timeout (recommended 100ms). Any long-running persistence should be enqueued asynchronously and retried by background workers.
