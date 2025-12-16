using ShareSphere.Api.Models;
namespace ShareSphere.Api.Services
{
    public interface IStockExchangeService
    {
        Task<IEnumerable<StockExchange>> GetAllAsync();
        Task<StockExchange?> GetByIdAsync(int exchangeId);
        Task<StockExchange> CreateAsync(StockExchange stockExchange);
        Task<StockExchange?> UpdateAsync(int exchangeId, StockExchange stockExchange);
        Task<bool> DeleteAsync(int exchangeId);
    }
}