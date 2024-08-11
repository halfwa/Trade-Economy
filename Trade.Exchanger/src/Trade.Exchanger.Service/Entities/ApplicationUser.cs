using Trade.Common;

namespace Trade.Exchanger.Service.Entities
{
    public class ApplicationUser : IEntity
    {
        public Guid Id { get; set; }
        public decimal Gil { get; set; }
    }
}
