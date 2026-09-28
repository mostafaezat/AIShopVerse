using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.OrderFeatures.Commands;
using Application.Features.OrderFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ApiControllerBase
    {
        [HttpPost("Checkout")]
        [EnableRateLimiting("checkout")]
        public async Task<ActionResult<Result<OrderDto>>> Checkout(CheckoutCommand command)
            => Single(await CommandAsync(command));

        [HttpGet("History")]
        public async Task<ActionResult<Result<List<OrderDto>>>> GetOrderHistory()
            => Single(await QueryAsync(new GetOrderHistoryQuery()));

        [HttpGet("{id}")]
        public async Task<ActionResult<Result<OrderDto>>> GetOrderById(string id)
            => Single(await QueryAsync(new GetOrderByIdQuery { OrderId = id }));

        [HttpPost("Cancel")]
        public async Task<ActionResult<Result<string>>> CancelOrder(CancelOrderCommand command)
            => Single(await CommandAsync(command));
    }
}
