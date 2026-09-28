using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.ProductFeatures.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscoveryController : ApiControllerBase
    {
        [HttpPost("Related")]
        public async Task<ActionResult<Result<List<ProductListItemDto>>>> Related(GetRelatedProductsQuery query)
            => Single(await QueryAsync(query));

        [HttpPost("BoughtTogether")]
        public async Task<ActionResult<Result<List<ProductListItemDto>>>> BoughtTogether(GetFrequentlyBoughtTogetherQuery query)
            => Single(await QueryAsync(query));
    }
}
