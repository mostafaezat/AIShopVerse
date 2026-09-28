using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Signalr
{
    public class NotificationHub : Hub
    {
        private readonly IConnectedUserTracker _connectedTracker;

        public NotificationHub(IConnectedUserTracker connectedTracker)
        {
            _connectedTracker = connectedTracker;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                _connectedTracker.UserConnected(userId, Context.ConnectionId);

            if (Context.User != null &&
                (Context.User.IsInRole("Admin") || Context.User.IsInRole("SuperAdmin")))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _connectedTracker.UserDisconnected(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }

    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var httpContext = connection.GetHttpContext();
            if (httpContext == null) return null;

            var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userId;
        }
    }
}
