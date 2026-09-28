using AIShopVerse.EndUser.Server.Controllers.Base;
using Application.Base.Wrapper;
using Application.Features.OrderFeatures.Queries;
using Application.Features.PaymentFeatures.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AIShopVerse.EndUser.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ApiControllerBase
    {
        [HttpPost("CreateCheckout")]
        [Authorize]
        public async Task<ActionResult<Result<CreateCheckoutResponse>>> CreateCheckout(CreateCheckoutCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Complete")]
        [Authorize]
        public async Task<ActionResult<Result<OrderDto>>> Complete(CompletePaymentCommand command)
            => Single(await CommandAsync(command));

        [HttpPost("Webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            string json;
            using (var reader = new System.IO.StreamReader(HttpContext.Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            var signature = HttpContext.Request.Headers["Stripe-Signature"].ToString();

            var result = await CommandAsync(new HandleStripeWebhookCommand { Json = json, Signature = signature });
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
