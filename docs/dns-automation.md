# DNS automation

Spamma can create domain verification TXT records and subdomain MX records through a selected DNS integration. Cloudflare is the first provider. Manual DNS setup and public DNS checks remain available.

## Add a Cloudflare integration

An administrator opens **DNS Integrations** (`/admin/dns-integrations`), clicks **Add Integration**, and adds a named connection for a Cloudflare zone. Enter the zone name, Cloudflare zone ID, and an API token scoped to that zone with **Zone Read**, **DNS Read**, and **DNS Write** permissions. The listing shows saved connections; you can save multiple connections, including more than one Cloudflare connection. Use **Edit** on a row to update it in a slide out panel. Leave the new API token blank to keep the stored token, or enter one to rotate it.

The API token is encrypted with ASP.NET Core Data Protection before it is stored. It is never returned by the integration list query. Keep the Data Protection key ring persistent and shared across app instances; losing it makes saved tokens unreadable. The Cloudflare API base URL defaults to the production API and can be overridden with `DnsAutomation__CloudflareApiBaseUrl` for testing.

## Select an integration for a domain

When adding a domain, choose **Manual DNS setup** or a named integration. The integration's zone name must match the domain exactly. Spamma saves the selected integration ID with the domain so that its verification TXT record and future subdomain MX records use the same connection. Existing domains created before this selection was added remain on manual DNS setup.

For an existing domain, open its details and choose **Edit Domain**. Select a DNS integration in the **DNS setup** field and save. You can also switch back to manual setup. Changing the selection does not publish or remove DNS records; use **Set up DNS automatically** afterward to create the verification record through the selected provider.

After creating a domain with an integration, click **Set up DNS automatically** to submit the `spamma-verification=<token>` TXT record at the apex. On a subdomain details page, the same action submits its MX record using the configured mail server hostname and MX priority. Click the existing verification or MX check action once public DNS reflects the change.

An exact existing record is accepted without another write. A different Spamma verification token or a different MX record at the same name is reported as a conflict for manual review. Other TXT records at the apex are left alone. Spamma does not delete or replace existing DNS records. If the provider is unavailable, add the displayed record manually and retry the public DNS check.

Additional providers implement `IDnsRecordProvider` in the DomainManagement module and register with dependency injection. The integration document stores the provider name, zone, and protected credential; setup commands and the UI use the same provider-independent path. Provider-specific input and validation can be added to the integration form as each provider is introduced.

## UI evidence

These screenshots were captured by the DNS browser scenarios with a disposable Cloudflare fixture token:

- [Integration listing](images/dns-automation/dns-integrations.png)
- [Add integration modal](images/dns-automation/add-dns-integration.png)
- [Edit integration slide out](images/dns-automation/edit-dns-integration.png)
- [DNS selection when adding a domain](images/dns-automation/add-domain-dns-selection.png)
- [DNS selection for an existing domain](images/dns-automation/existing-domain-dns-selection.png)
