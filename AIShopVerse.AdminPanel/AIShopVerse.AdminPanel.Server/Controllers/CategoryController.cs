using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.CategoryFeatures.Commands;
using Application.Features.CategoryFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class CategoryController : ApiControllerBase
    {
        [HttpPost("GetAll")]
        public async Task<ActionResult<Result<List<CategoryDto>>>> GetAllCategories()
            => Single(await QueryAsync(new GetAllCategoriesQuery()));

        [HttpPost("Add")]
        public async Task<ActionResult<Result<string>>> AddCategory(AddCategoryCommand command)
            => Single(await CommandAsync(command));

        [HttpPut("Update")]
        public async Task<ActionResult<Result<string>>> UpdateCategory(UpdateCategoryCommand command)
            => Single(await CommandAsync(command));

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<Result<string>>> DeleteCategory(string id)
            => Single(await CommandAsync(new DeleteCategoryCommand { Id = id }));
    }
}
