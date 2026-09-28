using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.RecommendationFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RecommendationController : ApiControllerBase
    {
        [HttpPost("ForMe")]
        public async Task<ActionResult<Result<RecommendedForUserDto>>> GetRecommendedForUser(GetRecommendedForUserQuery query)
            => Single(await QueryAsync(query));
    }
}
