using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.WishlistFeatures.Commands;
using Application.Features.WishlistFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WishlistController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Result<List<WishlistItemDto>>>> GetWishlist()
            => Single(await QueryAsync(new GetWishlistQuery()));

        [HttpPost("Add")]
        public async Task<ActionResult<Result<string>>> AddToWishlist(AddToWishlistCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Remove")]
        public async Task<ActionResult<Result<string>>> RemoveFromWishlist(RemoveFromWishlistCommand command)
            => Single(await CommandAsync(command));

        [HttpGet("IsInWishlist")]
        public async Task<ActionResult<Result<bool>>> IsInWishlist([FromQuery] string productId)
            => Single(await QueryAsync(new IsInWishlistQuery { ProductId = productId }));
    }
}
