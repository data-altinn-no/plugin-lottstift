using Dan.Plugin.Lottstift.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Altinn.Dan.Plugin.Lottstift.Services.Interfaces
{
    public interface IMemoryCacheProvider
    {
        public Task<(bool success, CacheModel result)> TryGet(string key);

        public Task<CacheModel> SetCache(string key, CacheModel model, TimeSpan timeToLive);
    }
}
