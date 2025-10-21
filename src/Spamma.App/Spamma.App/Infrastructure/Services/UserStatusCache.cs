using System.Text.Json;
using BluQube.Constants;
using BluQube.Queries;
using MaybeMonad;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;
using StackExchange.Redis;

namespace Spamma.App.Infrastructure.Services;

public class UserStatusCache(IConnectionMultiplexer redisMultiplexer, IQuerier querier, ITempObjectStore tempObjectStore)
{
    private const string Prefix = "userstatus:";
    private readonly IDatabase _redis = redisMultiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(1);

    public async Task<Maybe<CachedUser>> GetUserLookupAsync(Guid userId)
    {
        var key = Prefix + userId;
        var value = await this._redis.StringGetAsync(key);

        if (!value.HasValue)
        {
            var query = new GetUserByIdQuery(userId);
            tempObjectStore.AddReferenceForObject(query);
            var result = await querier.Send(query);
            if (result.Status == QueryResultStatus.Succeeded)
            {
                var user = new CachedUser
                {
                    UserId = result.Data.Id,
                    IsSuspended = result.Data.IsSuspended,
                    SystemRole = result.Data.SystemRole,
                    Domains = result.Data.ModeratedDomains.ToList(),
                    Subdomains = result.Data.ModeratedSubdomains.ToList(),
                    EmailAddress = result.Data.EmailAddress,
                    Name = result.Data.Name,
                };
                await this.SetUserLookupAsync(user);
                return Maybe.From(user);
            }
        }

        try
        {
            var cachedUser = JsonSerializer.Deserialize<CachedUser>(value!);
            return cachedUser != null ? Maybe.From(cachedUser) : Maybe<CachedUser>.Nothing;
        }
        catch (JsonException)
        {
            return Maybe<CachedUser>.Nothing;
        }
    }

    public async Task SetUserLookupAsync(CachedUser user)
    {
        var key = Prefix + user.UserId;
        var json = JsonSerializer.Serialize(user);
        await this._redis.StringSetAsync(key, json, this._ttl);
    }

    public async Task InvalidateAsync(Guid userId)
    {
        var key = Prefix + userId;
        await this._redis.KeyDeleteAsync(key);
    }

    public class CachedUser
    {
        public Guid UserId { get; set; }

        public bool IsSuspended { get; set; }

        public SystemRole SystemRole { get; set; }

        public List<Guid> Domains { get; set; } = new();

        public List<Guid> Subdomains { get; set; } = new();

        public string EmailAddress { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}