using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.PromotionFeatures.Commands;
using Application.Features.PromotionFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [EnableRateLimiting("fixed")]
    public class PromotionController : ApiControllerBase
    {
        [HttpPost("GetAll")]
        public async Task<ActionResult<Result<List<CouponDto>>>> GetAllCoupons()
            => Single(await QueryAsync(new GetAllCouponsQuery()));

        [HttpPost("Add")]
        public async Task<ActionResult<Result<string>>> AddCoupon(AddCouponCommand command)
            => Single(await CommandAsync(command));

        [HttpPut("Update")]
        public async Task<ActionResult<Result<string>>> UpdateCoupon(UpdateCouponCommand command)
            => Single(await CommandAsync(command));

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<Result<string>>> DeleteCoupon(string id)
            => Single(await CommandAsync(new DeleteCouponCommand { Id = id }));

        [HttpPut("Toggle/{id}")]
        public async Task<ActionResult<Result<string>>> ToggleCoupon(string id)
            => Single(await CommandAsync(new ToggleCouponCommand { Id = id }));
    }
}
