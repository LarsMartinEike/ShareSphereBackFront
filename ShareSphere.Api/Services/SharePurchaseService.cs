using Microsoft.EntityFrameworkCore;
using ShareSphere.Api. Data;
using ShareSphere. Api.Models;

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
            // Validierung:  Quantity muss positiv sein
            if (quantity <= 0)
            {
                return new PurchaseResult
                {
                    Success = false,
                    Message = "Die Menge muss größer als 0 sein."
                };
            }

            // Verwende eine Transaktion für atomare Operationen
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Prüfe ob Shareholder existiert
                var shareholder = await _context.Shareholders
                    .Include(s => s. Portfolios)
                    .FirstOrDefaultAsync(s => s.ShareholderId == shareholderId);

                if (shareholder == null)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Shareholder mit ID {shareholderId} wurde nicht gefunden."
                    };
                }

                // 2. Prüfe ob Share existiert und lade mit Company-Daten
                var share = await _context.Shares
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.ShareId == shareId);

                if (share == null)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Share mit ID {shareId} wurde nicht gefunden."
                    };
                }

                // 3. Prüfe ob Broker existiert
                var broker = await _context.Brokers. FindAsync(brokerId);
                if (broker == null)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Broker mit ID {brokerId} wurde nicht gefunden."
                    };
                }

                // 4. Prüfe Verfügbarkeit
                if (share.AvailableQuantity < quantity)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        Message = $"Nicht genügend Shares verfügbar.  Verfügbar: {share.AvailableQuantity}, Angefordert: {quantity}."
                    };
                }

                // 5. Reduziere verfügbare Shares
                share. AvailableQuantity -= quantity;

                // 6. Aktualisiere oder erstelle Portfolio
                var existingPortfolio = shareholder. Portfolios
                    .FirstOrDefault(p => p.ShareId == shareId);

                Portfolio portfolio;
                if (existingPortfolio != null)
                {
                    // Portfolio existiert bereits - erhöhe die Menge
                    existingPortfolio.amount += quantity;
                    portfolio = existingPortfolio;
                }
                else
                {
                    // Erstelle neues Portfolio
                    portfolio = new Portfolio
                    {
                        ShareholderId = shareholderId,
                        ShareId = shareId,
                        amount = quantity
                    };
                    _context.Portfolios.Add(portfolio);
                    shareholder.Portfolios.Add(portfolio);
                }

                // 7. Erstelle Trade-Eintrag
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

                // 8. Aktualisiere Portfolio-Wert des Shareholders
                shareholder. PortfolioValue += quantity * share.Price;

                // 9. Speichere alle Änderungen
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 10. Lade vollständige Daten für Rückgabe
                await _context.Entry(portfolio)
                    .Reference(p => p.Share)
                    .LoadAsync();

                await _context.Entry(trade)
                    .Reference(t => t. Broker)
                    .LoadAsync();
                await _context.Entry(trade)
                    .Reference(t => t.Company)
                    .LoadAsync();

                return new PurchaseResult
                {
                    Success = true,
                    Message = $"Erfolgreich {quantity} Share(s) von {share.Company?. Name ??  "Unbekannt"} gekauft.",
                    Trade = trade,
                    Portfolio = portfolio
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PurchaseResult
                {
                    Success = false,
                    Message = $"Ein Fehler ist aufgetreten: {ex.Message}"
                };
            }
        }
    }
}