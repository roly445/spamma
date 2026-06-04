using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.Common.Client.Application.Queries;

[BluQubeQuery(Path = "api/system/settings")]
public record GetSystemSettingsQuery : IQuery<GetSystemSettingsQueryResult>;
