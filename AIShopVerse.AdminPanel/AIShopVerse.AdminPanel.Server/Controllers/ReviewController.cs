using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Base.Shared;
using Application.Features.ReviewFeatures.Commands;
using Application.Features.ReviewFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ReviewController : ApiControllerBase
    {
        [HttpPost("GetAll")]
        public async Task<ActionResult<Result<PaginatedResult<AdminReviewDto>>>> GetAllReviews(GetAllReviewsQuery query)
            => Single(await QueryAsync(query));

        [HttpPost("Approve")]
        public async Task<ActionResult<Result<string>>> ApproveReview(ApproveReviewCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Reject")]
        public async Task<ActionResult<Result<string>>> RejectReview(RejectReviewCommand command)
            => Single(await CommandAsync(command));

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<Result<string>>> DeleteReview(string id)
            => Single(await CommandAsync(new AdminDeleteReviewCommand { ReviewId = id }));
    }
}
