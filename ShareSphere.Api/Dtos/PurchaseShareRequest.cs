using System.ComponentModel.DataAnnotations;

namespace ShareSphere.Api.Dtos
{
    /// <summary>
    /// DTO for purchasing shares.
    /// </summary>
    public record PurchaseShareRequest(
        [Required] int ShareholderId,
        [Required] int ShareId,
        [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")] int Quantity,
        [Required] int BrokerId
    );
}
