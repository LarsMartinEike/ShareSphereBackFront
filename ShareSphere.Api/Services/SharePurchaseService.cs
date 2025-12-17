using Microsoft.EntityFrameworkCore;
using ShareSphere.Api.Data;
using ShareSphere.Api.Models;

namespace ShareSphere.Api.Services
{
    public class SharePurchaseService : ISharePurchaseService
    {
        private readonly AppDbContext _context;

        public SharePurchaseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PurchaseResult> PurchaseSharesAsync(int shareholderId, int shareId, int quantity, int brokerId)
        {
            // Validate quantity
            if (quantity <= 0)
            {
                return new PurchaseResult
                {
                    Success = false,
                    Message = "Quantity must be greater than zero."
                };
            }

            // Start a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate shareholder exists
                var shareholder = await _context.Shareholders.FindAsync(shareholderId);
                if (shareholder == null)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Shareholder with ID {shareholderId} not found."
                    };
                }

                // Validate share exists and load with company information
                var share = await _context.Shares
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.ShareId == shareId);
                if (share == null)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Share with ID {shareId} not found."
                    };
                }

                // Validate broker exists
                var broker = await _context.Brokers.FindAsync(brokerId);
                if (broker == null)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Broker with ID {brokerId} not found."
                    };
                }

                // Check share availability
                if (share.AvailableQuantity < quantity)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Insufficient shares available. Requested: {quantity}, Available: {share.AvailableQuantity}."
                    };
                }

                // Reduce available quantity
                share.AvailableQuantity -= quantity;

                // Update or create portfolio entry
                var portfolio = await _context.Portfolios
                    .FirstOrDefaultAsync(p => p.ShareholderId == shareholderId && p.ShareId == shareId);

                if (portfolio == null)
                {
                    // Create new portfolio entry
                    portfolio = new Portfolio
                    {
                        ShareholderId = shareholderId,
                        ShareId = shareId,
                        amount = quantity
                    };
                    _context.Portfolios.Add(portfolio);
                }
                else
                {
                    // Update existing portfolio entry
                    portfolio.amount += quantity;
                }

                // Create trade record
                var trade = new Trade
                {
                    BrokerId = brokerId,
                    ShareholderId = shareholderId,
                    CompanyId = share.CompanyId,
                    Quantity = quantity,
                    UnitPrice = share.Price,
                    Type = TradeType.Buy,
                    Timestamp = DateTime.UtcNow
                };
                _context.Trades.Add(trade);

                // Save all changes
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                // Load navigation properties for the portfolio
                await _context.Entry(portfolio)
                    .Reference(p => p.Share)
                    .LoadAsync();
                await _context.Entry(portfolio)
                    .Reference(p => p.Shareholder)
                    .LoadAsync();

                // Load navigation properties for the trade
                await _context.Entry(trade)
                    .Reference(t => t.Broker)
                    .LoadAsync();
                await _context.Entry(trade)
                    .Reference(t => t.Shareholder)
                    .LoadAsync();
                await _context.Entry(trade)
                    .Reference(t => t.Company)
                    .LoadAsync();

                return new PurchaseResult
                {
                    Success = true,
                    Message = $"Successfully purchased {quantity} shares of {share.Company?.Name ?? "Unknown Company"}.",
                    Trade = trade,
                    Portfolio = portfolio
                };
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                return new PurchaseResult
                {
                    Success = false,
                    Message = $"An error occurred during the purchase: {ex.Message}"
                };
            }
        }
    }
}
