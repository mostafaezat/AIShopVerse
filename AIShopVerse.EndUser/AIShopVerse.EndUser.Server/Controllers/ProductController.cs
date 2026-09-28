using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.ProductFeatures.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ApiControllerBase
    {
        [HttpPost("Filter")]
        public async Task<ActionResult<Result<Application.Base.Shared.PaginatedResult<ProductListItemDto>>>> GetFilteredProducts(GetFilteredProductsQuery query)
            => Single(await QueryAsync(query));

        [HttpGet("{id}")]
        public async Task<ActionResult<Result<ProductDetailDto>>> GetProductById(string id)
            => Single(await QueryAsync(new GetProductByIdQuery(id)));

        [HttpGet("BestSellers")]
        public async Task<ActionResult<Result<List<ProductListItemDto>>>> GetBestSellers([FromQuery] int count = 12)
            => Single(await QueryAsync(new GetBestSellersQuery { Count = count }));

        [HttpGet("NewArrivals")]
        public async Task<ActionResult<Result<List<ProductListItemDto>>>> GetNewArrivals([FromQuery] int count = 12)
            => Single(await QueryAsync(new GetNewArrivalsQuery { Count = count }));

        [HttpGet("Promotions")]
        public async Task<ActionResult<Result<List<ProductListItemDto>>>> GetPromotions([FromQuery] int count = 12)
            => Single(await QueryAsync(new GetPromotionalProductsQuery { Count = count }));
    }
}
