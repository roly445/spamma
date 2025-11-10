using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries.PushIntegration;

/// <summary>
/// Query to get push integrations.
/// </summary>
[BluQubeQuery(Path = "api/email-push/integrations")]
public record GetPushIntegrationsQuery : IQuery<GetPushIntegrationsQueryResult>;