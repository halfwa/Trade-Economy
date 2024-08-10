using MassTransit;
using System.Threading.Tasks;
using Trade.Common;
using Trade.Inventory.Contracts;
using Trade.Inventory.Service.Entities;
using Trade.Inventory.Service.Exceptions;

namespace Trade.Inventory.Service.Consumers
{
    public class SubtractItemsConsumer : IConsumer<SubtractItems>
    {
        private readonly IRepository<InventoryItem> _inventoryItemsRepository;
        private readonly IRepository<CatalogItem> _catalogItemsRepository;

        public SubtractItemsConsumer(
            IRepository<InventoryItem> inventoryItemsRepository,
            IRepository<CatalogItem> catalogItemsRepository)
        {
            _inventoryItemsRepository = inventoryItemsRepository;
            _catalogItemsRepository = catalogItemsRepository;
        }

        public async Task Consume(ConsumeContext<SubtractItems> context)
        {
            var message = context.Message;

            var item = await _catalogItemsRepository.GetAsync(message.CatalogItemId);

            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }

            var inventoryItem = await _inventoryItemsRepository.GetAsync(
                item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

            if (inventoryItem != null)
            {
                if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
                {
                    await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
                    return;
                }

                inventoryItem.Quantity -= message.Quantity;
                inventoryItem.MessageIds.Add(context.MessageId.Value);
                await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
        }
    }
}
