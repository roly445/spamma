# data-model.md

## Email Aggregate (relevant fields)

- Id: Guid
- Subject: string
- Body: string
- From: MailboxAddress
- To/Cc/Bcc: collections
- CampaignId: Guid?  // nullable GUID indicates email part of campaign
- IsDeleted: bool
- IsFavorite: bool
- RegisteredAt: DateTime

Notes:
- CampaignId must be nullable to express absence/presence of campaign relationship.
- No change to campaign aggregate required (campaign:email is 1:1 and deletion of campaign synchronously deletes related email).
- Persistence: Marten stores CampaignId as part of event projections; ensure projections include CampaignId when generated.

