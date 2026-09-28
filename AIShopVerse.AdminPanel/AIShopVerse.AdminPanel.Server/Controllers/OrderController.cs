using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.OrderFeatures.Commands;
using Application.Features.OrderFeatures.Queries;
using Application.Features.PaymentFeatures.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class OrderController : ApiControllerBase
    {
        [HttpPost("GetAll")]
        public async Task<ActionResult<Result<List<OrderDto>>>> GetAllOrders(GetAllOrdersQuery query)
            => Single(await QueryAsync(query));

        [HttpGet("{id}")]
        public async Task<ActionResult<Result<AdminOrderDetailDto>>> GetOrderById(string id)
            => Single(await QueryAsync(new GetAdminOrderByIdQuery { OrderId = id }));

        [HttpPost("UpdateStatus")]
        public async Task<ActionResult<Result<string>>> UpdateOrderStatus(UpdateOrderStatusCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Refund")]
        public async Task<ActionResult<Result<string>>> RefundOrder(RefundOrderCommand command)
            => Single(await CommandAsync(command));
    }
}
