using System;

namespace Spamma.App.Infrastructure;

/// <summary>
/// Central place for Redis key prefixing. By default the prefix is read from
/// the environment variable REDIS_KEY_PREFIX. If empty, keys are unchanged.
/// This keeps backwards compatibility while allowing an app-wide prefix.
/// </summary>
public static class RedisKeys
{
    // Default to SPAMMA_ to avoid collisions with other applications using the same Redis instance.
    // Can be overridden by setting the REDIS_KEY_PREFIX environment variable.
    private static readonly string _prefix = Environment.GetEnvironmentVariable("REDIS_KEY_PREFIX") ?? "SPAMMA_";

    /// <summary>
    /// Gets the configured prefix. May be empty.
    /// </summary>
    public static string Prefix => _prefix;

    /// <summary>
    /// Returns the provided key prefixed when a prefix is configured; otherwise returns the key unchanged.
    /// </summary>
    /// <param name="key">The raw key to prefix.</param>
    /// <returns>The prefixed key if a prefix is configured; otherwise the original key.</returns>
    public static string WithPrefix(string key) => string.IsNullOrEmpty(_prefix) ? key : _prefix + key;
}
