using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.BrandFeatures.Commands;
using Application.Features.BrandFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class BrandController : ApiControllerBase
    {
        [HttpPost("GetAll")]
        public async Task<ActionResult<Result<List<BrandDto>>>> GetAllBrands()
            => Single(await QueryAsync(new GetAllBrandsQuery()));

        [HttpPost("Add")]
        public async Task<ActionResult<Result<string>>> AddBrand(AddBrandCommand command)
            => Single(await CommandAsync(command));

        [HttpPut("Update")]
        public async Task<ActionResult<Result<string>>> UpdateBrand(UpdateBrandCommand command)
            => Single(await CommandAsync(command));

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<Result<string>>> DeleteBrand(string id)
            => Single(await CommandAsync(new DeleteBrandCommand { Id = id }));
    }
}
