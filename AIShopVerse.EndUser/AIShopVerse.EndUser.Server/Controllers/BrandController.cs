using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Features.BrandFeatures.Queries;
using Application.Base.Wrapper;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : ApiControllerBase
    {
        [HttpGet("All")]
        public async Task<ActionResult<Result<System.Collections.Generic.List<BrandDto>>>> GetAllBrands()
            => Single(await QueryAsync(new GetAllBrandsQuery()));

        [HttpGet("Filtered")]
        public async Task<ActionResult<Result<List<BrandWithCountDto>>>> GetBrandsWithCounts([FromQuery] string? categoryId = null)
            => Single(await QueryAsync(new GetBrandsWithCountsQuery { CategoryId = categoryId }));
    }
}
