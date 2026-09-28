using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Application.Tests
{
    /// <summary>
    /// Minimal read-only in-memory IConfiguration used only by the test DI wiring.
    /// </summary>
    public sealed class TestConfiguration : IConfiguration
    {
        private readonly ConcurrentDictionary<string, string?> _values;

        public TestConfiguration(IEnumerable<KeyValuePair<string, string?>> values)
        {
            _values = new ConcurrentDictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        }

        public string? this[string key]
        {
            get => key == null ? null : _values.TryGetValue(key, out var value) ? value : null;
            set => _values[key!] = value;
        }

        public IConfigurationSection GetSection(string key) => new Section(this, key);

        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => NoOpChangeToken.Singleton;

        private sealed class Section : IConfigurationSection
        {
            private readonly TestConfiguration _root;
            private readonly string _path;

            public Section(TestConfiguration root, string path)
            {
                _root = root;
                _path = path;
            }

            public string Key => _path.Split(':').Last();

            public string Path => _path;

            public string? Value
            {
                get => _root[_path];
                set => _root[_path] = value;
            }

            public string? this[string key]
            {
                get => _root[_path + ":" + key];
                set => _root[_path + ":" + key] = value;
            }

            public IConfigurationSection GetSection(string key) => _root.GetSection(_path + ":" + key);

            public IEnumerable<IConfigurationSection> GetChildren()
                => _root._values.Keys
                    .Where(k => k.StartsWith(_path + ":", StringComparison.OrdinalIgnoreCase))
                    .Select(k => k[(_path.Length + 1)..].Split(':').First())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(k => _root.GetSection(_path + ":" + k));

            public IChangeToken GetReloadToken() => NoOpChangeToken.Singleton;
        }

        private sealed class NoOpChangeToken : IChangeToken
        {
            public static readonly NoOpChangeToken Singleton = new();

            public bool HasChanged => false;

            public bool ActiveChangeCallbacks => false;

            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new NoOpDisposable();

            private sealed class NoOpDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}