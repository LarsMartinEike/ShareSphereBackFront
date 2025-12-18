using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShareSphere.Api. Data;
using ShareSphere.Api.Models;
using ShareSphere.Api.Services;
using Xunit;

namespace ShareSphere.Api.Tests.Services
{
    public class SharePurchaseServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName:  Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => 
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        private async Task<(Shareholder shareholder, Share share, Broker broker)> SetupTestDataAsync(AppDbContext context)
        {
            // Erstelle StockExchange
            var stockExchange = new StockExchange
            {
                Name = "NYSE",
                Country = "New York"
            };
            context.StockExchanges.Add(stockExchange);

            // Erstelle Company
            var company = new Company
            {
                Name = "Test Company",
                TickerSymbol = "TEST",
                StockExchange = stockExchange
            };
            context.Companies.Add(company);

            // Erstelle Shareholder
            var shareholder = new Shareholder
            {
                Name = "Max Mustermann",
                Email = "max@example.com",
                PortfolioValue = 10000.00m
            };
            context.Shareholders.Add(shareholder);

            // Erstelle Share
            var share = new Share
            {
                Company = company,
                Price = 100.00m,
                AvailableQuantity = 50
            };
            context.Shares.Add(share);

            // Erstelle Broker
            var broker = new Broker
            {
                Name = "Test Broker",
                LicenseNumber = "LIC123",
                Email = "broker@example.com"
            };
            context. Brokers.Add(broker);

            await context.SaveChangesAsync();

            return (shareholder, share, broker);
        }

        #region Sell Tests

        [Fact]
        public async Task SellSharesAsync_ValidSell_ReturnsSuccess()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst kaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share. ShareId, 20, broker. BrokerId);

            // Lade aktualisierte Daten nach dem Kauf
            var updatedShare = await context.Shares.AsNoTracking().FirstOrDefaultAsync(s => s.ShareId == share.ShareId);
            int availableAfterPurchase = updatedShare! .AvailableQuantity;

            int sellQuantity = 10;

            // Act
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                sellQuantity,
                broker. BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert. NotNull(result.Trade);
            Assert.Equal(TradeType. Sell, result.Trade.Type);
            Assert.Equal(sellQuantity, result.Trade. Quantity);

            // Überprüfe, dass AvailableQuantity erhöht wurde
            var finalShare = await context.Shares. FindAsync(share.ShareId);
            Assert.Equal(availableAfterPurchase + sellQuantity, finalShare!.AvailableQuantity);
        }

        [Fact]
        public async Task SellSharesAsync_PartialSell_UpdatesPortfolio()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst 20 Shares kaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 20, broker.BrokerId);

            // Act - Nur 10 verkaufen
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Portfolio);
            Assert.Equal(10, result.Portfolio.amount); // 20 - 10 = 10 übrig

            // Überprüfe Portfolio in DB
            var portfolio = await context.Portfolios
                .FirstOrDefaultAsync(p => p. ShareholderId == shareholder. ShareholderId && p.ShareId == share.ShareId);
            Assert.NotNull(portfolio);
            Assert.Equal(10, portfolio.amount);
        }

        [Fact]
        public async Task SellSharesAsync_SellAll_DeletesPortfolio()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst 15 Shares kaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 15, broker.BrokerId);

            // Act - Alle 15 verkaufen
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                15,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert. Null(result.Portfolio); // Portfolio wurde gelöscht

            // Überprüfe dass Portfolio nicht mehr existiert
            var portfolioExists = await context. Portfolios
                .AnyAsync(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share.ShareId);
            Assert. False(portfolioExists);
        }

        [Fact]
        public async Task SellSharesAsync_InsufficientShares_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst 10 Shares kaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 10, broker.BrokerId);

            // Act - Versuche 15 zu verkaufen (mehr als vorhanden)
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                15,
                broker.BrokerId
            );

            // Assert
            Assert. False(result.Success);
            Assert.Contains("Nicht genügend Shares im Portfolio", result.Message);
            Assert.Null(result.Trade);
        }

        [Fact]
        public async Task SellSharesAsync_NoPortfolio_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Act - Versuche zu verkaufen ohne vorher gekauft zu haben
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share. ShareId,
                5,
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("besitzt keine Shares", result.Message);
            Assert.Null(result. Trade);
        }

        [Fact]
        public async Task SellSharesAsync_InvalidQuantity_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                0, // Ungültige Quantity
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("größer als 0", result.Message);
        }

        [Fact]
        public async Task SellSharesAsync_NegativeQuantity_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                -5,
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("größer als 0", result.Message);
        }

        [Fact]
        public async Task SellSharesAsync_UpdatesPortfolioValue()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst 20 Shares kaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 20, broker.BrokerId);

            var beforeShareholder = await context.Shareholders. AsNoTracking().FirstOrDefaultAsync(s => s.ShareholderId == shareholder.ShareholderId);
            decimal portfolioValueBeforeSell = beforeShareholder!. PortfolioValue;

            // Act - 10 verkaufen
            var result = await service.SellSharesAsync(
                shareholder. ShareholderId,
                share.ShareId,
                10,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);

            var afterShareholder = await context. Shareholders.FindAsync(shareholder.ShareholderId);
            Assert.Equal(portfolioValueBeforeSell - (10 * share.Price), afterShareholder!.PortfolioValue);
        }

        [Fact]
        public async Task SellSharesAsync_IncreasesAvailableQuantity()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst 20 Shares kaufen (reduziert AvailableQuantity)
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 20, broker.BrokerId);

            var shareBeforeSell = await context.Shares.AsNoTracking().FirstOrDefaultAsync(s => s.ShareId == share.ShareId);
            int availableBeforeSell = shareBeforeSell! .AvailableQuantity;

            // Act - 10 verkaufen
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                broker.BrokerId
            );

            // Assert
            Assert. True(result.Success);

            var shareAfterSell = await context.Shares.FindAsync(share.ShareId);
            Assert.Equal(availableBeforeSell + 10, shareAfterSell!.AvailableQuantity);
        }

        [Fact]
        public async Task SellSharesAsync_CreatesTradeWithCorrectType()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst kaufen
            await service.PurchaseSharesAsync(shareholder. ShareholderId, share.ShareId, 20, broker.BrokerId);

            // Act - Verkaufen
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                broker. BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert. NotNull(result.Trade);
            Assert.Equal(TradeType.Sell, result.Trade. Type);
            Assert.Equal(shareholder.ShareholderId, result.Trade.ShareholderId);
            Assert.Equal(share.CompanyId, result.Trade.CompanyId);
            Assert.Equal(broker.BrokerId, result.Trade.BrokerId);
            Assert.Equal(10, result. Trade.Quantity);
            Assert.Equal(share.Price, result.Trade.UnitPrice);

            // Überprüfe dass Trade in DB gespeichert wurde
            var trade = await context. Trades. FindAsync(result.Trade.TradeId);
            Assert.NotNull(trade);
            Assert.Equal(TradeType.Sell, trade. Type);
        }

        [Fact]
        public async Task SellSharesAsync_ShareholderNotFound_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (_, share, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service.SellSharesAsync(
                999, // Nicht existierende Shareholder-ID
                share.ShareId,
                10,
                broker. BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert. Contains("Shareholder", result.Message);
            Assert. Contains("nicht gefunden", result.Message);
        }

        [Fact]
        public async Task SellSharesAsync_ShareNotFound_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, _, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service. SellSharesAsync(
                shareholder.ShareholderId,
                999, // Nicht existierende Share-ID
                10,
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Share", result.Message);
            Assert.Contains("nicht gefunden", result.Message);
        }

        [Fact]
        public async Task SellSharesAsync_BrokerNotFound_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst kaufen mit gültigem Broker
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 20, broker.BrokerId);

            // Act - Verkaufen mit ungültigem Broker
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                999 // Nicht existierender Broker
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Broker", result.Message);
            Assert.Contains("nicht gefunden", result.Message);
        }

        [Fact]
        public async Task SellSharesAsync_MultipleSells_WorksCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst 30 Shares kaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 30, broker.BrokerId);

            // Act - Drei aufeinanderfolgende Verkäufe
            var result1 = await service.SellSharesAsync(shareholder.ShareholderId, share.ShareId, 5, broker.BrokerId);
            var result2 = await service.SellSharesAsync(shareholder.ShareholderId, share.ShareId, 10, broker.BrokerId);
            var result3 = await service.SellSharesAsync(shareholder.ShareholderId, share.ShareId, 8, broker.BrokerId);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.True(result3.Success);

            var portfolio = await context.Portfolios
                .FirstOrDefaultAsync(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share.ShareId);
            Assert.NotNull(portfolio);
            Assert.Equal(7, portfolio.amount); // 30 - 5 - 10 - 8 = 7
        }

        [Fact]
        public async Task SellSharesAsync_TransactionRollback_OnError()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Erst kaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 20, broker.BrokerId);

            var portfolioBefore = await context.Portfolios
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p. ShareholderId == shareholder. ShareholderId && p.ShareId == share.ShareId);
            int amountBefore = portfolioBefore!.amount;

            var shareBefore = await context.Shares.AsNoTracking().FirstOrDefaultAsync(s => s.ShareId == share.ShareId);
            int availableBefore = shareBefore!. AvailableQuantity;

            // Act - Versuche mit nicht existierendem Broker zu verkaufen
            var result = await service.SellSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                999 // Nicht existierender Broker
            );

            // Assert
            Assert.False(result.Success);

            // Überprüfe dass keine Änderungen vorgenommen wurden (Rollback erfolgreich)
            var portfolioAfter = await context.Portfolios
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share. ShareId);
            var shareAfter = await context.Shares.AsNoTracking().FirstOrDefaultAsync(s => s.ShareId == share.ShareId);

            Assert.Equal(amountBefore, portfolioAfter! .amount);
            Assert.Equal(availableBefore, shareAfter!.AvailableQuantity);
        }

        [Fact]
        public async Task SellSharesAsync_BuyAndSellCycle_MaintainsDataIntegrity()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            int initialAvailable = share.AvailableQuantity;
            decimal initialPortfolioValue = shareholder.PortfolioValue;

            // Act - Kaufen und wieder verkaufen
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 20, broker.BrokerId);
            await service.SellSharesAsync(shareholder.ShareholderId, share.ShareId, 20, broker.BrokerId);

            // Assert
            var finalShare = await context.Shares.FindAsync(share.ShareId);
            var finalShareholder = await context.Shareholders.FindAsync(shareholder.ShareholderId);

            Assert.Equal(initialAvailable, finalShare! .AvailableQuantity);
            Assert.Equal(initialPortfolioValue, finalShareholder!.PortfolioValue);

            // Portfolio sollte nicht mehr existieren
            var portfolioExists = await context.Portfolios
                .AnyAsync(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share.ShareId);
            Assert.False(portfolioExists);
        }

        #endregion

        // Existing Purchase Tests würden hier weiter folgen...
    }
}