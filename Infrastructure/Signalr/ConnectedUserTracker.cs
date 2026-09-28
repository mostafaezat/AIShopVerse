using System.Collections.Concurrent;

namespace Infrastructure.Signalr
{
    public interface IConnectedUserTracker
    {
        void UserConnected(string userId, string connectionId);
        void UserDisconnected(string connectionId);
        IEnumerable<string> GetConnections(string userId);
        bool IsUserConnected(string userId);
    }

    public class ConnectedUserTracker : IConnectedUserTracker
    {
        private static readonly ConcurrentDictionary<string, List<string>> _userConnections = new();

        public void UserConnected(string userId, string connectionId)
        {
            _userConnections.AddOrUpdate(userId,
                new List<string> { connectionId },
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(connectionId);
                    }
                    return existing;
                });
        }

        public void UserDisconnected(string connectionId)
        {
            foreach (var kvp in _userConnections)
            {
                lock (kvp.Value)
                {
                    kvp.Value.Remove(connectionId);
                }
            }
        }

        public IEnumerable<string> GetConnections(string userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return connections.ToList();
                }
            }
            return Enumerable.Empty<string>();
        }

        public bool IsUserConnected(string userId)
        {
            return _userConnections.ContainsKey(userId) && _userConnections[userId].Any();
        }
    }
}
