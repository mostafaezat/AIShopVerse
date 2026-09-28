using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.CategoryFeatures.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ApiControllerBase
    {
        [HttpGet("Tree")]
        public async Task<ActionResult<Result<List<CategoryTreeDto>>>> GetCategoryTree()
            => Single(await QueryAsync(new GetCategoryTreeQuery()));

        [HttpGet("Filtered")]
        public async Task<ActionResult<Result<List<CategoryWithCountDto>>>> GetCategoriesWithCounts([FromQuery] string? brandId = null)
            => Single(await QueryAsync(new GetCategoriesWithCountsQuery { BrandId = brandId }));
    }
}
