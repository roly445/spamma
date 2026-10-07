# Spam feedback webhook

Spamma sends `POST` requests with `Content-Type: application/json` to the HTTPS URL configured for a subdomain. The URL must be on that subdomain's verified parent domain. ARF email and webhook delivery can be enabled independently. The webhook is a test event and does not submit a complaint to a mailbox provider.

## Event body

```json
{
  "type": "spam.reported",
  "reportId": "f45c8a19-0e04-4f67-bd0f-fce1ce94791c",
  "messageId": "f45c8a19-0e04-4f67-bd0f-fce1ce94791c",
  "recipient": "receiver@inbound.example.com",
  "sender": "sender@example.net",
  "subdomainId": "4dca3e6e-8b24-45d9-8708-937bd82825e7",
  "campaignId": null,
  "campaignValue": null,
  "trigger": "Manual",
  "reportedAt": "2026-10-06T12:00:00+00:00"
}
```

`trigger` is `Manual` or `ChaosAddress`. `campaignId` and `campaignValue` are null when the message has no campaign. The report and message IDs are the same because each captured message can have one spam report.

## Verify the signature

When the URL is first saved or changed, Spamma shows a new 32-byte signing secret as 64 hexadecimal characters. Store it securely; Spamma shows it only once. Each request includes:

- `X-Spamma-Report-Id`: the report ID in the body.
- `Idempotency-Key`: the same report ID. Use it to ignore duplicate deliveries.
- `X-Spamma-Timestamp`: Unix time in seconds.
- `X-Spamma-Signature`: `sha256=` followed by lowercase hex HMAC-SHA256.

Decode the secret from hex. Compute HMAC-SHA256 over the UTF-8 bytes of `X-Spamma-Timestamp`, a period, and the **exact raw request body**. Compare the result to `X-Spamma-Signature` with a constant-time comparison. Reject old timestamps according to your own replay window. Parse and act on the JSON only after signature verification.

Any 2xx response marks delivery successful. Other responses and network failures are recorded per channel. Spamma retries automatically up to five attempts with increasing delays; an operator can retry a failed channel from the message view. Retries keep the same report and idempotency key, but use a fresh timestamp and signature.

ARF email is a separate RFC 5965 abuse report sent to the configured address on the verified parent domain. It tests that address's complaint handler; it does not represent a provider-issued complaint or affect provider reputation by itself.
