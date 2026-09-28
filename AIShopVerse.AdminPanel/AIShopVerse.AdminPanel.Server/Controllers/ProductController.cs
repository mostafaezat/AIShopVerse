using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Base.Shared;
using Application.Features.ProductFeatures.Commands;
using Application.Features.ProductFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ProductController : ApiControllerBase
    {
        [HttpPost("GetAll")]
        public async Task<ActionResult<Result<PaginatedResult<ProductListItemDto>>>> GetAllProducts([FromBody] GetAllProductsQuery query)
            => Single(await QueryAsync(query));

        [HttpGet("{id}")]
        public async Task<ActionResult<Result<ProductDetailDto>>> GetProductById(string id)
            => Single(await QueryAsync(new GetProductByIdQuery(id)));

        [HttpPost("Add")]
        public async Task<ActionResult<Result<string>>> AddProduct(AddProductCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("UploadImage")]
        public async Task<ActionResult<Result<string>>> UploadImage(IFormFile file)
            => Single(await CommandAsync(new UploadImageCommand { File = file }));

        [HttpPut("Update")]
        public async Task<ActionResult<Result<string>>> UpdateProduct(UpdateProductCommand command)
            => Single(await CommandAsync(command));

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<Result<string>>> DeleteProduct(string id)
            => Single(await CommandAsync(new DeleteProductCommand { Id = id }));

        [HttpPost("AdjustStock")]
        public async Task<ActionResult<Result<int>>> AdjustStock(AdjustStockCommand command)
            => Single(await CommandAsync(command));
    }
}
