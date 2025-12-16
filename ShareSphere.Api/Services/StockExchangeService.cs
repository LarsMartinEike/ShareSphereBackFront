using Microsoft.EntityFrameworkCore;
using ShareSphere.Api.Models;
using ShareSphere.Api.Data;
namespace ShareSphere.Api.Services
{
    public class StockExchangeService : IStockExchangeService
    {
        private readonly AppDbContext _context;

        public StockExchangeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockExchange>> GetAllAsync()
        {
            return await _context.StockExchanges
                .Include(se => se.Companies)
                .ToListAsync();
        }

        public async Task<StockExchange?> GetByIdAsync(int exchangeId)
        {
            return await _context.StockExchanges
                .Include(se => se.Companies)
                .FirstOrDefaultAsync(se => se.ExchangeId == exchangeId);
        }

        public async Task<StockExchange> CreateAsync(StockExchange stockExchange)
        {
            _context.StockExchanges.Add(stockExchange);
            await _context.SaveChangesAsync();
            return stockExchange;
        }

        public async Task<StockExchange?> UpdateAsync(int exchangeId, StockExchange stockExchange)
        {
            var existing = await _context.StockExchanges.FindAsync(exchangeId);
            if (existing == null)
                return null;

            existing.Name = stockExchange.Name;
            existing.Country = stockExchange.Country;
            existing.Currency = stockExchange.Currency;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int exchangeId)
        {
            var stockExchange = await _context.StockExchanges.FindAsync(exchangeId);
            if (stockExchange == null)
                return false;

            _context.StockExchanges.Remove(stockExchange);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}