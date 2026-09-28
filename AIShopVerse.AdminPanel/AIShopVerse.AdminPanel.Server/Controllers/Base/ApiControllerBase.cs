using Microsoft.AspNetCore.Mvc;

namespace AIShopVerse.AdminPanel.Server.Controllers.Base
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        private ISender _mediator = null!;
        protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

        protected async Task<TResult> QueryAsync<TResult>(IRequest<TResult> query)
            => await Mediator.Send(query);

        protected async Task<TResult> CommandAsync<TResult>(IRequest<TResult> command)
            => await Mediator.Send(command);

        protected ActionResult<T> Single<T>(T data)
            => data == null ? NotFound() : Ok(data);
    }
}
