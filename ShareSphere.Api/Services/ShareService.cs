using Microsoft.EntityFrameworkCore;
using ShareSphere.Api.Models;
using ShareSphere.Api.Data;

namespace ShareSphere.Api.Services
{
    public class ShareService : IShareService
    {
        private readonly AppDbContext _context;

        public ShareService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Share>> GetAllAsync()
        {
            return await _context. Shares
                .Include(s => s. Company)
                    .ThenInclude(c => c! .StockExchange)
                .ToListAsync();
        }

        public async Task<Share?> GetByIdAsync(int shareId)
        {
            return await _context.Shares
                .Include(s => s.Company)
                    .ThenInclude(c => c!.StockExchange)
                .FirstOrDefaultAsync(s => s.ShareId == shareId);
        }

        public async Task<IEnumerable<Share>> GetByCompanyIdAsync(int companyId)
        {
            return await _context.Shares
                . Include(s => s.Company)
                    .ThenInclude(c => c!.StockExchange)
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<Share> CreateAsync(Share share)
        {
            _context.Shares.Add(share);
            await _context.SaveChangesAsync();

            // Lade die Company und StockExchange Navigation Properties nach dem Speichern
            await _context.Entry(share)
                .Reference(s => s.Company)
                .LoadAsync();

            if (share.Company != null)
            {
                await _context.Entry(share.Company)
                    .Reference(c => c.StockExchange)
                    .LoadAsync();
            }

            return share;
        }

        public async Task<Share?> UpdateAsync(int shareId, Share share)
        {
            var existing = await _context.Shares. FindAsync(shareId);
            if (existing == null)
                return null;

            bool priceChanged = existing.Price != share.Price;

            existing.CompanyId = share.CompanyId;
            existing.Price = share.Price;
            existing. AvailableQuantity = share. AvailableQuantity;

            await _context.SaveChangesAsync();

            // Wenn Preis geändert wurde, aktualisiere alle betroffenen Portfolios
            if (priceChanged)
            {
                await RecalculateAffectedPortfoliosAsync(shareId);
            }

            return existing;
        }

               /// <summary>
        /// Berechnet PortfolioValues aller Shareholders neu, die diesen Share besitzen
        /// </summary>
        private async Task RecalculateAffectedPortfoliosAsync(int shareId)
        {
            // Finde alle Shareholders, die diesen Share besitzen
            var affectedShareholders = await _context.Shareholders
                .Include(s => s.Portfolios)
                    .ThenInclude(p => p.Share)
                .Where(s => s. Portfolios.Any(p => p.ShareId == shareId))
                .ToListAsync();

            foreach (var shareholder in affectedShareholders)
            {
                // Berechne neuen Portfolio-Wert
                decimal newValue = shareholder.Portfolios
                    .Where(p => p.Share != null)
                    .Sum(p => p. amount * p.Share!.Price);

                shareholder.PortfolioValue = newValue;
            }

            await _context. SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int shareId)
        {
            var share = await _context.Shares.FindAsync(shareId);
            if (share == null)
                return false;

            _context.Shares. Remove(share);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}