using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.SearchFeatures.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ApiControllerBase
    {
        [HttpPost("Suggest")]
        public async Task<ActionResult<Result<List<SearchSuggestionDto>>>> Suggest(SearchSuggestQuery query)
            => Single(await QueryAsync(query));
    }
}
