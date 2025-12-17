using Microsoft.EntityFrameworkCore;
using ShareSphere.Api.Data;
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
            context.Companies. Add(company);

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

        [Fact]
        public async Task PurchaseSharesAsync_ValidPurchase_ReturnsSuccess()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            int purchaseQuantity = 10;
            decimal initialPortfolioValue = shareholder.PortfolioValue;
            int initialAvailableQuantity = share.AvailableQuantity;

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder. ShareholderId,
                share.ShareId,
                purchaseQuantity,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Trade);
            Assert.NotNull(result.Portfolio);
            Assert.Equal(purchaseQuantity, result.Trade.Quantity);
            
            // Überprüfe, dass AvailableQuantity reduziert wurde
            var updatedShare = await context.Shares.FindAsync(share.ShareId);
            Assert.Equal(initialAvailableQuantity - purchaseQuantity, updatedShare! .AvailableQuantity);
            
            // Überprüfe, dass PortfolioValue aktualisiert wurde
            var updatedShareholder = await context.Shareholders.FindAsync(shareholder.ShareholderId);
            Assert. Equal(initialPortfolioValue + (purchaseQuantity * share.Price), updatedShareholder!. PortfolioValue);
        }

        [Fact]
        public async Task PurchaseSharesAsync_FirstPurchase_CreatesNewPortfolio()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            int purchaseQuantity = 10;

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share. ShareId,
                purchaseQuantity,
                broker.BrokerId
            );

            // Assert
            Assert.True(result. Success);
            Assert.NotNull(result.Portfolio);
            Assert.Equal(shareholder.ShareholderId, result.Portfolio.ShareholderId);
            Assert.Equal(share.ShareId, result.Portfolio. ShareId);
            Assert.Equal(purchaseQuantity, result. Portfolio.amount);
            
            // Überprüfe, dass Portfolio in der Datenbank existiert
            var portfolioCount = await context.Portfolios
                .Where(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share.ShareId)
                .CountAsync();
            Assert.Equal(1, portfolioCount);
        }

        [Fact]
        public async Task PurchaseSharesAsync_SecondPurchase_UpdatesExistingPortfolio()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            
            // Erster Kauf
            int firstPurchase = 10;
            await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                firstPurchase,
                broker.BrokerId
            );

            // Act - Zweiter Kauf
            int secondPurchase = 15;
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                secondPurchase,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Portfolio);
            Assert.Equal(firstPurchase + secondPurchase, result.Portfolio.amount);
            
            // Überprüfe, dass nur ein Portfolio-Eintrag existiert
            var portfolioCount = await context.Portfolios
                .Where(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share.ShareId)
                .CountAsync();
            Assert. Equal(1, portfolioCount);
        }

        [Fact]
        public async Task PurchaseSharesAsync_CreatesTrade()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            int purchaseQuantity = 10;

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder. ShareholderId,
                share.ShareId,
                purchaseQuantity,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Trade);
            Assert.Equal(TradeType.Buy, result.Trade.Type);
            Assert.Equal(shareholder.ShareholderId, result.Trade.ShareholderId);
            Assert.Equal(share. CompanyId, result.Trade.CompanyId);
            Assert.Equal(broker.BrokerId, result.Trade.BrokerId);
            Assert.Equal(purchaseQuantity, result.Trade.Quantity);
            Assert.Equal(share.Price, result.Trade.UnitPrice);
            
            // Überprüfe, dass Trade in der Datenbank existiert
            var trade = await context.Trades. FindAsync(result.Trade.TradeId);
            Assert.NotNull(trade);
        }

        [Fact]
        public async Task PurchaseSharesAsync_InvalidQuantity_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share. ShareId,
                0, // Ungültige Quantity
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("größer als 0", result.Message);
            Assert. Null(result.Trade);
            Assert.Null(result.Portfolio);
        }

        [Fact]
        public async Task PurchaseSharesAsync_NegativeQuantity_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                -5, // Negative Quantity
                broker.BrokerId
            );

            // Assert
            Assert. False(result.Success);
            Assert.Contains("größer als 0", result.Message);
        }

        [Fact]
        public async Task PurchaseSharesAsync_ShareholderNotFound_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (_, share, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service.PurchaseSharesAsync(
                999, // Nicht existierende Shareholder-ID
                share.ShareId,
                10,
                broker. BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert. Contains("Shareholder", result.Message);
            Assert.Contains("nicht gefunden", result.Message);
            Assert.Null(result.Trade);
            Assert.Null(result.Portfolio);
        }

        [Fact]
        public async Task PurchaseSharesAsync_ShareNotFound_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, _, broker) = await SetupTestDataAsync(context);

            // Act
            var result = await service. PurchaseSharesAsync(
                shareholder.ShareholderId,
                999, // Nicht existierende Share-ID
                10,
                broker.BrokerId
            );

            // Assert
            Assert.False(result. Success);
            Assert.Contains("Share", result.Message);
            Assert.Contains("nicht gefunden", result.Message);
            Assert. Null(result.Trade);
            Assert.Null(result. Portfolio);
        }

        [Fact]
        public async Task PurchaseSharesAsync_BrokerNotFound_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, _) = await SetupTestDataAsync(context);

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                999 // Nicht existierende Broker-ID
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Broker", result.Message);
            Assert.Contains("nicht gefunden", result.Message);
            Assert.Null(result.Trade);
            Assert.Null(result.Portfolio);
        }

        [Fact]
        public async Task PurchaseSharesAsync_InsufficientShares_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            int availableQuantity = share.AvailableQuantity;

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                availableQuantity + 10, // Mehr als verfügbar
                broker. BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert. Contains("Nicht genügend", result.Message);
            Assert. Contains("verfügbar", result.Message);
            Assert.Null(result. Trade);
            Assert.Null(result.Portfolio);
            
            // Überprüfe, dass keine Änderungen vorgenommen wurden
            var updatedShare = await context. Shares.FindAsync(share. ShareId);
            Assert.Equal(availableQuantity, updatedShare!.AvailableQuantity);
        }

        [Fact]
        public async Task PurchaseSharesAsync_ExactAvailableQuantity_ReturnsSuccess()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            int availableQuantity = share.AvailableQuantity;

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                availableQuantity, // Exakt die verfügbare Menge
                broker.BrokerId
            );

            // Assert
            Assert. True(result.Success);
            Assert.NotNull(result.Trade);
            Assert.NotNull(result.Portfolio);
            
            // Überprüfe, dass AvailableQuantity jetzt 0 ist
            var updatedShare = await context.Shares. FindAsync(share.ShareId);
            Assert.Equal(0, updatedShare!.AvailableQuantity);
        }

        [Fact]
        public async Task PurchaseSharesAsync_MultiplePurchases_ReducesAvailableQuantityCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            int initialQuantity = share.AvailableQuantity;

            // Act - Drei aufeinanderfolgende Käufe
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 10, broker.BrokerId);
            await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 15, broker.BrokerId);
            var result = await service.PurchaseSharesAsync(shareholder.ShareholderId, share.ShareId, 5, broker.BrokerId);

            // Assert
            Assert.True(result.Success);
            var updatedShare = await context. Shares.FindAsync(share.ShareId);
            Assert.Equal(initialQuantity - 30, updatedShare!.AvailableQuantity);
        }

        [Fact]
        public async Task PurchaseSharesAsync_TransactionRollback_OnError()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share, broker) = await SetupTestDataAsync(context);
            int initialAvailableQuantity = share. AvailableQuantity;
            decimal initialPortfolioValue = shareholder.PortfolioValue;

            // Act - Versuche mit nicht existierendem Broker zu kaufen
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                999 // Nicht existierender Broker
            );

            // Assert
            Assert.False(result.Success);
            
            // Überprüfe, dass keine Änderungen vorgenommen wurden (Rollback erfolgreich)
            var updatedShare = await context.Shares.FindAsync(share.ShareId);
            var updatedShareholder = await context. Shareholders.FindAsync(shareholder.ShareholderId);
            
            Assert.Equal(initialAvailableQuantity, updatedShare! .AvailableQuantity);
            Assert.Equal(initialPortfolioValue, updatedShareholder!.PortfolioValue);
            
            // Überprüfe, dass kein Portfolio erstellt wurde
            var portfolioExists = await context.Portfolios
                .AnyAsync(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share.ShareId);
            Assert.False(portfolioExists);
        }

        [Fact]
        public async Task PurchaseSharesAsync_DifferentShares_CreatesMultiplePortfolios()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);
            var (shareholder, share1, broker) = await SetupTestDataAsync(context);
            
            // Erstelle zweiten Share
            var company2 = await context.Companies. FirstAsync();
            var share2 = new Share
            {
                Company = company2,
                Price = 50.00m,
                AvailableQuantity = 100
            };
            context.Shares.Add(share2);
            await context.SaveChangesAsync();

            // Act - Kaufe beide Shares
            var result1 = await service.PurchaseSharesAsync(shareholder.ShareholderId, share1.ShareId, 10, broker.BrokerId);
            var result2 = await service. PurchaseSharesAsync(shareholder.ShareholderId, share2.ShareId, 20, broker.BrokerId);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            
            // Überprüfe, dass zwei separate Portfolios existieren
            var portfolios = await context.Portfolios
                .Where(p => p. ShareholderId == shareholder. ShareholderId)
                .ToListAsync();
            
            Assert.Equal(2, portfolios.Count);
            Assert.Contains(portfolios, p => p.ShareId == share1.ShareId && p.amount == 10);
            Assert. Contains(portfolios, p => p.ShareId == share2.ShareId && p.amount == 20);
        }
    }
}