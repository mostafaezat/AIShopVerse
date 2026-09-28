using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.NotificationFeatures.Commands;
using Application.Features.NotificationFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Result<List<NotificationDto>>>> GetMyNotifications([FromQuery] bool? onlyUnread, [FromQuery] int? take)
            => Single(await QueryAsync(new GetMyNotificationsQuery { OnlyUnread = onlyUnread, Take = take }));

        [HttpGet("UnreadCount")]
        public async Task<ActionResult<Result<int>>> GetUnreadCount()
            => Single(await QueryAsync(new GetUnreadNotificationsCountQuery()));

        [HttpPost("{id}/Read")]
        public async Task<ActionResult<Result<string>>> MarkAsRead(string id)
            => Single(await CommandAsync(new MarkNotificationReadCommand { NotificationId = id }));

        [HttpPost("ReadAll")]
        public async Task<ActionResult<Result<string>>> MarkAllAsRead()
            => Single(await CommandAsync(new MarkAllNotificationsReadCommand()));
    }
}
