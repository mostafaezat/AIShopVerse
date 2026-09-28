using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.NotificationFeatures.Commands;
using Domain.Entities.NotificationEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [EnableRateLimiting("fixed")]
    public class NotificationController : ApiControllerBase
    {
        [HttpPost("Broadcast")]
        public async Task<ActionResult<Result<string>>> Broadcast(BroadcastNotificationCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Send")]
        public async Task<ActionResult<Result<string>>> Send(CreateNotificationCommand command)
            => Single(await CommandAsync(command));
    }
}
