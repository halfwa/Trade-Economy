using Microsoft.AspNetCore.Mvc;
using Trade.Catalog.Service.Dtos;
using Trade.Catalog.Service.Entities;
using Trade.Common;

namespace Trade.Catalog.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController: ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
        private static int requestCounter = 0;

        public ItemsController(IRepository<Item> itemsRepository)
        {
            _itemsRepository = itemsRepository;
        }

        // GET /items
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            requestCounter++;
            Console.WriteLine($"Request {requestCounter}: Starting...");

            if (requestCounter <= 2) 
            {
                Console.WriteLine($"Request {requestCounter}: Delaying...");
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
            if (requestCounter <= 4)
            {
                Console.WriteLine($"Request {requestCounter}: Error...");
                return StatusCode(500);
            }


            var items = (await _itemsRepository.GetAllAsync())
                        .Select(item => item.AsDto());
            Console.WriteLine($"Request {requestCounter}: 200 (Ok)");

            return Ok(items);
        }

        // GET /items/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null) 
            { 
                return NotFound();
            }

            return item.AsDto();
        }

        // POST /items
        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item 
            {
               Name =  createItemDto.Name, 
               Description = createItemDto.Description,
               Price = createItemDto.Price,
               CreatedDate = DateTimeOffset.UtcNow
            };

            await _itemsRepository.CreateAsync(item);

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id}, item);
        }

        // PUT /items/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await _itemsRepository.GetAsync(id);

            if (existingItem == null)
            {
                var item = new Item
                {
                    Name = updateItemDto.Name,
                    Description = updateItemDto.Description,
                    Price = updateItemDto.Price,
                    CreatedDate = DateTimeOffset.UtcNow
                };
                await _itemsRepository.CreateAsync(item);

                return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync(existingItem);

            return NoContent();
        }

        // DELETE /items/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            await _itemsRepository.RemoveAsync(item.Id);

            return NoContent();
        }
    }
}
