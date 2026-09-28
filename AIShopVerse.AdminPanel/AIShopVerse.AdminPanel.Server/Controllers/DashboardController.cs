using AIShopVerse.AdminPanel.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.DashboardFeatures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.AdminPanel.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class DashboardController : ApiControllerBase
    {
        [HttpPost("GetAnalytics")]
        public async Task<ActionResult<Result<DashboardAnalyticsDto>>> GetAnalytics(GetDashboardAnalyticsQuery query)
            => Single(await QueryAsync(query));
    }
}
