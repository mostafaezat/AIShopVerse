using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Base.Shared;
using Application.Features.InventoryFeatures.Queries;
using Application.Features.ProductFeatures.Commands;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class InventoryController : ApiControllerBase
    {
        private readonly IOptions<InventoryOptions> _inventoryOptions;

        public InventoryController(IOptions<InventoryOptions> inventoryOptions)
        {
            _inventoryOptions = inventoryOptions;
        }

        [HttpPost("GetLevels")]
        public async Task<ActionResult<Result<PaginatedResult<InventoryLevelDto>>>> GetInventoryLevels(GetInventoryLevelsQuery query)
        {
            query.LowStockThreshold = _inventoryOptions.Value.LowStockThreshold;
            return Single(await QueryAsync(query));
        }

        [HttpPost("AdjustStock")]
        public async Task<ActionResult<Result<int>>> AdjustStock(AdjustStockCommand command)
            => Single(await CommandAsync(command));
    }
}
