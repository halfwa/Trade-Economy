using System.ComponentModel.DataAnnotations;

namespace Trade.Exchanger.Service.Dtos
{

    public record SubmitPurchaseDto(
        [Required] Guid? ItemId, 
        [Range(1,100)]int Quantity
    );
}
