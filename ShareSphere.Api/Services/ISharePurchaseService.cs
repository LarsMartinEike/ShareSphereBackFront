using ShareSphere.Api.Models;

namespace ShareSphere.Api.Services
{
    public interface ISharePurchaseService
    {
        /// <summary>
        /// Purchases shares for a shareholder.
        /// </summary>
        /// <param name="shareholderId">ID of the shareholder making the purchase</param>
        /// <param name="shareId">ID of the share to purchase</param>
        /// <param name="quantity">Quantity of shares to purchase</param>
        /// <param name="brokerId">ID of the broker handling the transaction</param>
        /// <returns>Result of the purchase operation</returns>
        Task<PurchaseResult> PurchaseSharesAsync(int shareholderId, int shareId, int quantity, int brokerId);
    }
}
