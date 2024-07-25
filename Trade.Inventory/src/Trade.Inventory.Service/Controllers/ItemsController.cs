using Microsoft.AspNetCore.Mvc;
using Trade.Common;
using Trade.Inventory.Service.Clients;
using Trade.Inventory.Service.Dtos;
using Trade.Inventory.Service.Entities;

namespace Trade.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController: ControllerBase
    {
        private readonly IRepository<InventoryItem> _inventoryItemsRepository;
        private readonly IRepository<CatalogItem> _catalogItemsRepository;

        public ItemsController(
            IRepository<InventoryItem> inventoryItemsRepository,
            IRepository<CatalogItem> catalogItemsRepository)
        {
            _inventoryItemsRepository = inventoryItemsRepository;
            _catalogItemsRepository = catalogItemsRepository;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var inventoryItemEntities = await _inventoryItemsRepository.GetAllAsync(item => item.UserId == userId);
            var itemsIds = inventoryItemEntities.Select(item => item.CatalogItemId);
            var catalogItemEntities = await _catalogItemsRepository.GetAllAsync(item => itemsIds.Contains(item.Id));

            var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await _inventoryItemsRepository.GetAsync(
                item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    UserId = grantItemsDto.UserId,
                    Quantity = grantItemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _inventoryItemsRepository.CreateAsync(inventoryItem); 
            }
            else 
            {
                inventoryItem.Quantity += grantItemsDto.Quantity;
                await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            return Ok();
        }
    }
}
