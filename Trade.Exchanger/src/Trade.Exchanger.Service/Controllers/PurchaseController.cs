using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trade.Exchanger.Service.Contracts;
using Trade.Exchanger.Service.Dtos;

namespace Trade.Exchanger.Service.Controllers
{
    [ApiController]
    [Route("purchase")]
    [Authorize]
    public class PurchaseController: ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        public PurchaseController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(SubmitPurchaseDto purchase)
        {
            var userId = User.FindFirstValue("sub");
            var correlationId = Guid.NewGuid();

            var message = new PurchaseRequested(
                Guid.Parse(userId),
                purchase.ItemId.Value,
                purchase.Quantity,
                correlationId
            );

            await _publishEndpoint.Publish(message);

            return Accepted();
        }
    }
}
