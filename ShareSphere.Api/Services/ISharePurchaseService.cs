using ShareSphere.Api.Models;

namespace ShareSphere.Api.Services
{
    public interface ISharePurchaseService
    {
        Task<PurchaseResult> PurchaseSharesAsync(int shareholderId, int shareId, int quantity, int brokerId);
    }

    /// <summary>
    /// Ergebnis einer Share-Kauf-Transaktion
    /// </summary>
    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Trade?  Trade { get; set; }
        public Portfolio? Portfolio { get; set; }
    }
}