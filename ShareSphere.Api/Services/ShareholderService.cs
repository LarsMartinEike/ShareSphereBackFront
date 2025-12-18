using Microsoft.EntityFrameworkCore;
using ShareSphere.Api.Models;
using ShareSphere.Api.Data;
using ShareSphere.Api.Models.Dtos;

namespace ShareSphere.Api.Services
{
    public class ShareholderService : IShareholderService
    {
        private readonly AppDbContext _context;

        public ShareholderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Shareholder>> GetAllAsync()
        {
            return await _context.Shareholders
                .Include(s => s. Portfolios)
                    .ThenInclude(p => p.Share)
                        .ThenInclude(sh => sh! .Company)
                .Include(s => s.Portfolios)
                    .ThenInclude(p => p.Share)
                        .ThenInclude(sh => sh!.Company)
                            .ThenInclude(c => c! .StockExchange)
                .ToListAsync();
        }

        public async Task<Shareholder? > GetByIdAsync(int shareholderId)
        {
            return await _context.Shareholders
                .Include(s => s. Portfolios)
                    . ThenInclude(p => p.Share)
                        .ThenInclude(sh => sh! .Company)
                .Include(s => s.Portfolios)
                    .ThenInclude(p => p.Share)
                        .ThenInclude(sh => sh!.Company)
                            .ThenInclude(c => c!.StockExchange)
                .FirstOrDefaultAsync(s => s.ShareholderId == shareholderId);
        }

        public async Task<Shareholder?> GetByEmailAsync(string email)
        {
            return await _context. Shareholders
                .Include(s => s.Portfolios)
                    .ThenInclude(p => p.Share)
                        .ThenInclude(sh => sh!.Company)
                .FirstOrDefaultAsync(s => s. Email == email);
        }

        public async Task<Shareholder> CreateAsync(Shareholder shareholder)
        {
            _context.Shareholders.Add(shareholder);
            await _context. SaveChangesAsync();
            return shareholder;
        }

        public async Task<Shareholder? > UpdateAsync(int shareholderId, Shareholder shareholder)
        {
            var existing = await _context.Shareholders. FindAsync(shareholderId);
            if (existing == null)
                return null;

            existing.Name = shareholder.Name;
            existing.Email = shareholder.Email;
            existing. PortfolioValue = shareholder.PortfolioValue;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int shareholderId)
        {
            var shareholder = await _context.Shareholders. FindAsync(shareholderId);
            if (shareholder == null)
                return false;

            _context.Shareholders.Remove(shareholder);
            await _context. SaveChangesAsync();
            return true;
        }


        /// <summary>
        /// Gibt vollständige Portfolio-Details zurück:  
        /// - List of shares owned
        /// - Quantity per share
        /// - Current price per share  
        /// - Total portfolio value
        /// </summary>
        public async Task<ShareholderPortfolioDto?> GetShareholderPortfolioAsync(int shareholderId)
        {
            var shareholder = await _context.Shareholders
                .Include(s => s.Portfolios)
                    .ThenInclude(p => p.Share)
                        .ThenInclude(sh => sh!.Company)
                            .ThenInclude(c => c!.StockExchange)
                .FirstOrDefaultAsync(s => s.ShareholderId == shareholderId);

            if (shareholder == null)
                return null;

            // Erstelle Liste der besessenen Shares
            var ownedShares = shareholder. Portfolios
                .Where(p => p.Share != null && p.Share.Company != null)
                .Select(p => new OwnedShareDto
                {
                    ShareId = p. ShareId,
                    CompanyId = p.Share! .CompanyId,
                    CompanyName = p.Share.Company! .Name,
                    TickerSymbol = p.Share. Company.TickerSymbol,
                    Quantity = p. amount,
                    CurrentPricePerShare = p.Share. Price,
                    TotalValue = p.amount * p.Share.Price,
                    StockExchange = p.Share.Company.StockExchange?. Name ?? "Unknown"
                })
                .OrderByDescending(s => s.TotalValue) // Sortiert nach Wert (höchster zuerst)
                .ToList();

            // Berechne Gesamtwert basierend auf aktuellen Preisen
            decimal totalPortfolioValue = ownedShares.Sum(s => s. TotalValue);

            // Gesamtanzahl aller Shares
            int totalSharesCount = ownedShares.Sum(s => s.Quantity);

            return new ShareholderPortfolioDto
            {
                ShareholderId = shareholder.ShareholderId,
                ShareholderName = shareholder.Name,
                Email = shareholder.Email,
                TotalPortfolioValue = totalPortfolioValue,
                OwnedShares = ownedShares,
                TotalSharesCount = totalSharesCount
            };
        }
    }
}