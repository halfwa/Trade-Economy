﻿using MassTransit;
using System;
using System.Threading.Tasks;
using Trade.Common;
using Trade.Inventory.Contracts;
using Trade.Inventory.Service.Dtos;
using Trade.Inventory.Service.Entities;
using Trade.Inventory.Service.Exceptions;

namespace Trade.Inventory.Service.Consumers
{
    public class GrantItemsConsumer : IConsumer<GrantItems>
    {
        private readonly IRepository<InventoryItem> _inventoryItemsRepository;
        private readonly IRepository<CatalogItem> _catalogItemsRepository;

        public GrantItemsConsumer(
            IRepository<InventoryItem> inventoryItemsRepository, 
            IRepository<CatalogItem> catalogItemsRepository)
        {
            _inventoryItemsRepository = inventoryItemsRepository;
            _catalogItemsRepository = catalogItemsRepository;
        }

        public async Task Consume(ConsumeContext<GrantItems> context)
        {
            var message = context.Message;

            var item = await _catalogItemsRepository.GetAsync(message.CatalogItemId);

            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }

            var inventoryItem = await _inventoryItemsRepository.GetAsync(
                item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = message.CatalogItemId,
                    UserId = message.UserId,
                    Quantity = message.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                inventoryItem.MessageIds.Add(context.MessageId.Value);

                await _inventoryItemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
                {
                    await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                    return;
                }

                inventoryItem.Quantity += message.Quantity;
                inventoryItem.MessageIds.Add(context.MessageId.Value);
                await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            var itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
            var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity));

            await Task.WhenAll(itemsGrantedTask, inventoryUpdatedTask);
        }
    }
}
