using Altinn.Dan.Plugin.Lottstift.Services.Interfaces;
using Dan.Plugin.Lottstift.Config;
using Dan.Plugin.Lottstift.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Altinn.Dan.Plugin.Lottstift.Services

{
    public class MemoryCacheProvider : IMemoryCacheProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly Settings _settings;

        public MemoryCacheProvider(IMemoryCache memoryCache, IOptions<Settings> settings)
        {
            _memoryCache = memoryCache;
            _settings = settings.Value;
        }
        public Task<(bool success, CacheModel result)> TryGet(string key)
        {
            bool success = _memoryCache.TryGetValue(key, out CacheModel result);
            return Task.FromResult((success, result));
        }

        public async Task<CacheModel> SetCache(string key, CacheModel value, TimeSpan timeToLive)
        {
            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
            {
                Priority = CacheItemPriority.High,
            };

            cacheEntryOptions.SetAbsoluteExpiration(timeToLive);
            var result = _memoryCache.Set(key, value, cacheEntryOptions);

            await Task.CompletedTask;

            return result;
        }      
    }
}
