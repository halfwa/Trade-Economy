using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using System;

namespace Trade.Identity.Service.Entities
{
    [CollectionName("Users")]
    public class ApplicationUser: MongoIdentityUser<Guid>
    {
        public decimal Gil { get; set; }
    }
}
