﻿using System;

namespace Trade.Inventory.Service.Exceptions
{
    [Serializable]
    internal class UnknownItemException : Exception
    {
        public UnknownItemException(Guid itemId)
            : base($"Unknown Item {itemId}")
        {
            ItemId = ItemId;
        }

        public Guid ItemId { get;}
    }
}