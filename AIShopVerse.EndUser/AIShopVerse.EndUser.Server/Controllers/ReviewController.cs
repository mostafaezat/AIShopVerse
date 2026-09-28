using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.ReviewFeatures.Commands;
using Application.Features.ReviewFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ApiControllerBase
    {
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<Result<List<ReviewDto>>>> GetProductReviews(string productId)
            => Single(await QueryAsync(new GetProductReviewsQuery { ProductId = productId }));

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Result<string>>> CreateReview(CreateReviewCommand command)
            => Single(await CommandAsync(command));

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<Result<string>>> DeleteReview(string id)
            => Single(await CommandAsync(new DeleteReviewCommand { ReviewId = id }));
    }
}
