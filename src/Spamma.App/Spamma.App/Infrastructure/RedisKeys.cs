using System;

namespace Spamma.App.Infrastructure;

public static class RedisKeys
{
    // Default to SPAMMA_ to avoid collisions with other applications using the same Redis instance.
    // Can be overridden by setting the REDIS_KEY_PREFIX environment variable.
    private static readonly string _prefix = Environment.GetEnvironmentVariable("REDIS_KEY_PREFIX") ?? "SPAMMA_";
    public static string Prefix => _prefix;
    public static string WithPrefix(string key) => string.IsNullOrEmpty(_prefix) ? key : _prefix + key;
}
