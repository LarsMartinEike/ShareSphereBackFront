using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task PurchaseSharesAsync_ValidPurchase_ReturnsSuccess()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var exchange = new StockExchange
            {
                Name = "NYSE",
                Country = "USA",
                Currency = "USD"
            };
            context.StockExchanges.Add(exchange);
            await context.SaveChangesAsync();

            var company = new Company
            {
                Name = "Apple Inc.",
                TickerSymbol = "AAPL",
                ExchangeId = exchange.ExchangeId
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var share = new Share
            {
                CompanyId = company.CompanyId,
                Price = 150.50m,
                AvailableQuantity = 1000
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var shareholder = new Shareholder
            {
                Name = "John Doe",
                Email = "john@example.com",
                PortfolioValue = 10000m
            };
            context.Shareholders.Add(shareholder);
            await context.SaveChangesAsync();

            var broker = new Broker
            {
                Name = "Test Broker",
                Email = "broker@example.com"
            };
            context.Brokers.Add(broker);
            await context.SaveChangesAsync();

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                100,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Successfully purchased", result.Message);
            Assert.NotNull(result.Trade);
            Assert.NotNull(result.Portfolio);
            Assert.Equal(100, result.Portfolio.amount);
            Assert.Equal(TradeType.Buy, result.Trade.Type);

            // Verify share quantity was reduced
            var updatedShare = await context.Shares.FindAsync(share.ShareId);
            Assert.Equal(900, updatedShare!.AvailableQuantity);
        }

        [Fact]
        public async Task PurchaseSharesAsync_ExistingPortfolio_UpdatesAmount()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var exchange = new StockExchange
            {
                Name = "NYSE",
                Country = "USA",
                Currency = "USD"
            };
            context.StockExchanges.Add(exchange);
            await context.SaveChangesAsync();

            var company = new Company
            {
                Name = "Tesla",
                TickerSymbol = "TSLA",
                ExchangeId = exchange.ExchangeId
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var share = new Share
            {
                CompanyId = company.CompanyId,
                Price = 250.00m,
                AvailableQuantity = 1000
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var shareholder = new Shareholder
            {
                Name = "Jane Doe",
                Email = "jane@example.com",
                PortfolioValue = 20000m
            };
            context.Shareholders.Add(shareholder);
            await context.SaveChangesAsync();

            var broker = new Broker
            {
                Name = "Test Broker",
                Email = "broker@example.com"
            };
            context.Brokers.Add(broker);
            await context.SaveChangesAsync();

            // Create existing portfolio entry
            var portfolio = new Portfolio
            {
                ShareholderId = shareholder.ShareholderId,
                ShareId = share.ShareId,
                amount = 50
            };
            context.Portfolios.Add(portfolio);
            await context.SaveChangesAsync();

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                100,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Portfolio);
            Assert.Equal(150, result.Portfolio.amount); // 50 + 100

            // Verify only one portfolio entry exists
            var portfolioCount = await context.Portfolios
                .CountAsync(p => p.ShareholderId == shareholder.ShareholderId && p.ShareId == share.ShareId);
            Assert.Equal(1, portfolioCount);
        }

        [Fact]
        public async Task PurchaseSharesAsync_InsufficientShares_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var exchange = new StockExchange
            {
                Name = "NYSE",
                Country = "USA",
                Currency = "USD"
            };
            context.StockExchanges.Add(exchange);
            await context.SaveChangesAsync();

            var company = new Company
            {
                Name = "Amazon",
                TickerSymbol = "AMZN",
                ExchangeId = exchange.ExchangeId
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var share = new Share
            {
                CompanyId = company.CompanyId,
                Price = 130.00m,
                AvailableQuantity = 50
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var shareholder = new Shareholder
            {
                Name = "John Smith",
                Email = "smith@example.com",
                PortfolioValue = 10000m
            };
            context.Shareholders.Add(shareholder);
            await context.SaveChangesAsync();

            var broker = new Broker
            {
                Name = "Test Broker",
                Email = "broker@example.com"
            };
            context.Brokers.Add(broker);
            await context.SaveChangesAsync();

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                100, // Requesting more than available
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Insufficient shares available", result.Message);
            Assert.Null(result.Trade);
            Assert.Null(result.Portfolio);

            // Verify share quantity was not changed
            var updatedShare = await context.Shares.FindAsync(share.ShareId);
            Assert.Equal(50, updatedShare!.AvailableQuantity);

            // Verify no portfolio entry was created
            var portfolioCount = await context.Portfolios
                .CountAsync(p => p.ShareholderId == shareholder.ShareholderId);
            Assert.Equal(0, portfolioCount);
        }

        [Fact]
        public async Task PurchaseSharesAsync_InvalidQuantity_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            // Act
            var result = await service.PurchaseSharesAsync(1, 1, 0, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("must be greater than zero", result.Message);
        }

        [Fact]
        public async Task PurchaseSharesAsync_InvalidShareholder_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var exchange = new StockExchange
            {
                Name = "NYSE",
                Country = "USA",
                Currency = "USD"
            };
            context.StockExchanges.Add(exchange);
            await context.SaveChangesAsync();

            var company = new Company
            {
                Name = "Microsoft",
                TickerSymbol = "MSFT",
                ExchangeId = exchange.ExchangeId
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var share = new Share
            {
                CompanyId = company.CompanyId,
                Price = 350.00m,
                AvailableQuantity = 100
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var broker = new Broker
            {
                Name = "Test Broker",
                Email = "broker@example.com"
            };
            context.Brokers.Add(broker);
            await context.SaveChangesAsync();

            // Act
            var result = await service.PurchaseSharesAsync(
                999, // Non-existent shareholder
                share.ShareId,
                10,
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Shareholder with ID 999 not found", result.Message);
        }

        [Fact]
        public async Task PurchaseSharesAsync_InvalidShare_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var shareholder = new Shareholder
            {
                Name = "John Doe",
                Email = "john@example.com",
                PortfolioValue = 10000m
            };
            context.Shareholders.Add(shareholder);
            await context.SaveChangesAsync();

            var broker = new Broker
            {
                Name = "Test Broker",
                Email = "broker@example.com"
            };
            context.Brokers.Add(broker);
            await context.SaveChangesAsync();

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                999, // Non-existent share
                10,
                broker.BrokerId
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Share with ID 999 not found", result.Message);
        }

        [Fact]
        public async Task PurchaseSharesAsync_InvalidBroker_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var exchange = new StockExchange
            {
                Name = "NYSE",
                Country = "USA",
                Currency = "USD"
            };
            context.StockExchanges.Add(exchange);
            await context.SaveChangesAsync();

            var company = new Company
            {
                Name = "Google",
                TickerSymbol = "GOOGL",
                ExchangeId = exchange.ExchangeId
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var share = new Share
            {
                CompanyId = company.CompanyId,
                Price = 140.00m,
                AvailableQuantity = 100
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var shareholder = new Shareholder
            {
                Name = "John Doe",
                Email = "john@example.com",
                PortfolioValue = 10000m
            };
            context.Shareholders.Add(shareholder);
            await context.SaveChangesAsync();

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                10,
                999 // Non-existent broker
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Broker with ID 999 not found", result.Message);
        }

        [Fact]
        public async Task PurchaseSharesAsync_CreatesTradeRecord()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var exchange = new StockExchange
            {
                Name = "NYSE",
                Country = "USA",
                Currency = "USD"
            };
            context.StockExchanges.Add(exchange);
            await context.SaveChangesAsync();

            var company = new Company
            {
                Name = "Netflix",
                TickerSymbol = "NFLX",
                ExchangeId = exchange.ExchangeId
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var share = new Share
            {
                CompanyId = company.CompanyId,
                Price = 450.75m,
                AvailableQuantity = 500
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var shareholder = new Shareholder
            {
                Name = "Mary Johnson",
                Email = "mary@example.com",
                PortfolioValue = 50000m
            };
            context.Shareholders.Add(shareholder);
            await context.SaveChangesAsync();

            var broker = new Broker
            {
                Name = "Trade Broker",
                Email = "trade@example.com"
            };
            context.Brokers.Add(broker);
            await context.SaveChangesAsync();

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                25,
                broker.BrokerId
            );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Trade);
            Assert.Equal(TradeType.Buy, result.Trade.Type);
            Assert.Equal(25, result.Trade.Quantity);
            Assert.Equal(450.75m, result.Trade.UnitPrice);
            Assert.Equal(broker.BrokerId, result.Trade.BrokerId);
            Assert.Equal(shareholder.ShareholderId, result.Trade.ShareholderId);
            Assert.Equal(company.CompanyId, result.Trade.CompanyId);

            // Verify trade was saved to database
            var tradeInDb = await context.Trades.FindAsync(result.Trade.TradeId);
            Assert.NotNull(tradeInDb);
        }

        [Fact]
        public async Task PurchaseSharesAsync_TransactionRollback_OnError()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SharePurchaseService(context);

            var exchange = new StockExchange
            {
                Name = "NYSE",
                Country = "USA",
                Currency = "USD"
            };
            context.StockExchanges.Add(exchange);
            await context.SaveChangesAsync();

            var company = new Company
            {
                Name = "Oracle",
                TickerSymbol = "ORCL",
                ExchangeId = exchange.ExchangeId
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var share = new Share
            {
                CompanyId = company.CompanyId,
                Price = 90.00m,
                AvailableQuantity = 100
            };
            context.Shares.Add(share);
            await context.SaveChangesAsync();

            var shareholder = new Shareholder
            {
                Name = "Bob Wilson",
                Email = "bob@example.com",
                PortfolioValue = 5000m
            };
            context.Shareholders.Add(shareholder);
            await context.SaveChangesAsync();

            // Note: Not adding broker to cause validation failure

            // Act
            var result = await service.PurchaseSharesAsync(
                shareholder.ShareholderId,
                share.ShareId,
                50,
                999 // Non-existent broker
            );

            // Assert
            Assert.False(result.Success);

            // Verify share quantity was not changed (transaction rolled back)
            var updatedShare = await context.Shares.FindAsync(share.ShareId);
            Assert.Equal(100, updatedShare!.AvailableQuantity);

            // Verify no portfolio entry was created
            var portfolioCount = await context.Portfolios
                .CountAsync(p => p.ShareholderId == shareholder.ShareholderId);
            Assert.Equal(0, portfolioCount);

            // Verify no trade was created
            var tradeCount = await context.Trades.CountAsync();
            Assert.Equal(0, tradeCount);
        }
    }
}
