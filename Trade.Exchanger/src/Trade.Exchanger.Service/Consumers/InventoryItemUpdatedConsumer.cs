using MassTransit;
using Trade.Common;
using Trade.Exchanger.Service.Entities;
using Trade.Inventory.Contracts;

namespace Trade.Exchanger.Service.Consumers
{
    public class InventoryItemUpdatedConsumer : IConsumer<InventoryItemUpdated>
    {
        private readonly IRepository<InventoryItem> _repository;

        public InventoryItemUpdatedConsumer(
            IRepository<InventoryItem> repository)
        {
            _repository = repository;
        }
        public async Task Consume(ConsumeContext<InventoryItemUpdated> context)
        {
            var message = context.Message;

            var inventoryItem = await _repository.GetAsync(
                item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = message.CatalogItemId,
                    UserId = message.UserId,
                    Quantity = message.NewTotalQuantity
                };

                await _repository.CreateAsync(inventoryItem);   
            }
            else
            {
                inventoryItem.Quantity = message.NewTotalQuantity;
                await _repository.UpdateAsync(inventoryItem);
            }
        }
    }
}
