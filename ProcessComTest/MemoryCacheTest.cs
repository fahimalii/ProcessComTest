using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessComTest
{
    public interface IMemoryCacheTest
    {
        public void CacheTryGetValueSet();
        public void SetItemInCacheTest(string fileFullPath);
    }

    public class MemoryCacheTest : IMemoryCacheTest
    {
        private readonly IMemoryCache _cache;
        private string _data;
        private readonly string _errorFile;

        public MemoryCacheTest(IMemoryCache memoryCache, IHostEnvironment hostEnvironment)
        {
            _cache = memoryCache;
            _errorFile = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "errors", "error.txt");
        }

        public void SetItemInCacheTest(string fileFullPath)
        {
            // Read Data from file
            try
            {
                FileInfo file = new FileInfo(fileFullPath);
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        _data = reader.ReadToEnd();
                    }
                }

                CacheTryGetValueSet();

            }
            catch (Exception ex)
            {
                File.WriteAllText(_errorFile, ex.Message);
            }
        }

        public void CacheTryGetValueSet()
        {
            string cacheEntry;

            // Look for cache key.
            if (!_cache.TryGetValue(CacheKeys.Entry, out cacheEntry))
            {
                // Key not in cache, so get data.
                cacheEntry = _data;
                _data = string.Empty;

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromSeconds(20));

                // Save data in cache.
                _cache.Set(CacheKeys.Entry, cacheEntry, cacheEntryOptions);
            }
            else
            {
                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromSeconds(20));
                
                if(!string.IsNullOrWhiteSpace(_data) && !_data.Equals(cacheEntry))
                {
                    cacheEntry = _data;
                    _data = string.Empty;
                }

                _cache.Set(CacheKeys.Entry, cacheEntry, cacheEntryOptions);
            }
        }
    }
}
