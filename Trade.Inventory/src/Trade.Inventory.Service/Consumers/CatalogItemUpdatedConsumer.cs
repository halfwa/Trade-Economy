﻿using MassTransit;
using System.Threading.Tasks;
using Trade.Catalog.Contracts;
using Trade.Common;
using Trade.Inventory.Service.Entities;

namespace Trade.Inventory.Service.Consumers
{
    public class CatalogItemUpdatedConsumer: IConsumer<CatalogItemUpdated>
    {

        private readonly IRepository<CatalogItem> _repository;

        public CatalogItemUpdatedConsumer(IRepository<CatalogItem> repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<CatalogItemUpdated> context)
        {
            var message = context.Message;

            var item = await _repository.GetAsync(message.ItemId);

            if (item == null)
            {
                item = new CatalogItem
                {
                    Id = message.ItemId,
                    Name = message.Name,
                    Description = message.Description
                };

                await _repository.CreateAsync(item);
            }
            else
            {
                item.Name = message.Name;
                item.Description = message.Description;

                await _repository.UpdateAsync(item);
            }

        }
    }
}
