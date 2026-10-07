using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace Core.CrossCuttingConcerns.Caching.Microsoft
{
    public class MemoryCacheManager : ICacheManager
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, byte> _keys = new();
        public MemoryCacheManager(IMemoryCache cache) => _cache = cache;
        public T? Get<T>(string key) => _cache.Get<T>(key);
        public object? Get(string key) => _cache.Get(key);
        public bool IsAdd(string key) => _cache.TryGetValue(key, out _);
        public void Add(string key, object data, int duration)
        {
            _keys[key] = 0;
            _cache.Set(key, data, TimeSpan.FromMinutes(duration));
        }
        public void Remove(string key)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
        public void RemoveByPattern(string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            foreach (var key in _keys.Keys)
            {
                if (regex.IsMatch(key)) Remove(key);
                else if (!_cache.TryGetValue(key, out _)) _keys.TryRemove(key, out _);
            }
        }
    }
}
