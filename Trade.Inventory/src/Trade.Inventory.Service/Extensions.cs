﻿using Trade.Inventory.Service.Dtos;
using Trade.Inventory.Service.Entities;

namespace Trade.Inventory.Service
{
    public static class Extensions
    {
        public static InventoryItemDto AsDto(this InventoryItem item, string name, string description) 
        {
            return new InventoryItemDto(item.CatalogItemId, name, description, item.Quantity, item.AcquiredDate);
        }

    }
}
