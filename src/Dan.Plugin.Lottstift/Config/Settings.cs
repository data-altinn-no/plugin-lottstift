using System;

namespace Dan.Plugin.Lottstift.Config;

public class Settings
{
    public int DefaultCircuitBreakerOpenCircuitTimeSeconds { get; init; }
    public int DefaultCircuitBreakerFailureBeforeTripping { get; init; }
    public int SafeHttpClientTimeout { get; init; }

    public string EndpointUrl { get; init; }

    public string LastUpdatedUrl { get; init; }

    public int CacheTimeToLiveDays { get; init; }
}
