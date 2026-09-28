using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.CartFeatures.Commands;
using Application.Features.CartFeatures.Queries;
using Application.Features.PromotionFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Result<CartDto>>> GetCart()
            => Single(await QueryAsync(new GetCartQuery()));

        [HttpPost("Add")]
        public async Task<ActionResult<Result<CartDto>>> AddToCart(AddToCartCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Update")]
        public async Task<ActionResult<Result<CartDto>>> UpdateCartItem(UpdateCartItemCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Remove")]
        public async Task<ActionResult<Result<string>>> RemoveCartItem(RemoveCartItemCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Coupon")]
        public async Task<ActionResult<Result<CartDto>>> ApplyCoupon(ApplyCartCouponCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("ValidateCoupon")]
        public async Task<ActionResult<Result<CouponValidationDto>>> ValidateCoupon(ValidateCouponQuery query)
            => Single(await QueryAsync(query));
    }
}
