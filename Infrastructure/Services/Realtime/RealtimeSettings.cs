using System;

namespace Infrastructure.Services.Realtime
{
    public class RedisRelayOptions
    {
        public string? Configuration { get; set; }
        public string HostName { get; set; } = "host";

        public bool Enabled => !string.IsNullOrWhiteSpace(Configuration);
    }
}
