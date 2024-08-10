﻿using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trade.Exchanger.Service.Contracts;
using Trade.Exchanger.Service.Dtos;
using Trade.Exchanger.Service.StateMachines;

namespace Trade.Exchanger.Service.Controllers
{
    [ApiController]
    [Route("purchase")]
    [Authorize]
    public class PurchaseController: ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IRequestClient<GetPurchaseState> _purchaseClient;

        public PurchaseController(
            IPublishEndpoint publishEndpoint, 
            IRequestClient<GetPurchaseState> purchaseClient)
        {
            _publishEndpoint = publishEndpoint;
            _purchaseClient = purchaseClient;
        }

        [HttpGet("status/{idempotencyId}")]
        public async Task<ActionResult> GetStatusAsync(Guid idempotencyId)
        {
            var response = await _purchaseClient.GetResponse<PurchaseState>(
                new GetPurchaseState(idempotencyId));

            var purchaseState = response.Message;

            var purchase = new PurchaseDto(
                purchaseState.UserId,
                purchaseState.ItemId,
                purchaseState.PurchaseTotal,
                purchaseState.Quantity,
                purchaseState.CurrentState,
                purchaseState.ErrorMessage,
                purchaseState.Received,
                purchaseState.LastUpdated
            );

            return Ok(purchase);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(SubmitPurchaseDto purchase)
        {
            var userId = User.FindFirstValue("sub");

            var message = new PurchaseRequested(
                Guid.Parse(userId),
                purchase.ItemId.Value,
                purchase.Quantity,
                purchase.IdempotencyId.Value
            );

            await _publishEndpoint.Publish(message);

            return AcceptedAtAction(
                nameof(GetStatusAsync), 
                new { purchase.IdempotencyId }, 
                new { purchase.IdempotencyId });
        }
    }
}
