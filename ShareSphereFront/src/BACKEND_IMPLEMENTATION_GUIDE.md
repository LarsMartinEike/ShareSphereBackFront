# ShareSphere Backend Implementation Guide
## .NET WebAPI + Entity Framework Core + SQL Server

This document provides complete reference for implementing the ShareSphere backend to support the React frontend.

---

## Table of Contents
1. [Project Structure](#project-structure)
2. [Database Schema](#database-schema)
3. [Entity Models](#entity-models)
4. [DTOs (Data Transfer Objects)](#dtos-data-transfer-objects)
5. [API Endpoints Reference](#api-endpoints-reference)
6. [Controller Implementation](#controller-implementation)
7. [Service Layer](#service-layer)
8. [Validation Rules](#validation-rules)
9. [Error Handling](#error-handling)
10. [CORS Configuration](#cors-configuration)

---

## Project Structure

```
ShareSphere.API/
├── Controllers/
│   ├── ExchangesController.cs
│   ├── CompaniesController.cs
│   ├── SharesController.cs
│   ├── BrokersController.cs
│   ├── TradesController.cs
│   ├── PortfolioController.cs
│   └── AdminController.cs
├── Models/
│   ├── Entities/
│   │   ├── Exchange.cs
│   │   ├── Company.cs
│   │   ├── Share.cs
│   │   ├── Broker.cs
│   │   ├── Trade.cs
│   │   └── Shareholder.cs
│   └── DTOs/
│       ├── ExchangeDto.cs
│       ├── CompanyDto.cs
│       ├── ShareDto.cs
│       ├── TradeDto.cs
│       └── PortfolioDto.cs
├── Services/
│   ├── IExchangeService.cs
│   ├── ICompanyService.cs
│   ├── IShareService.cs
│   ├── ITradeService.cs
│   └── IPortfolioService.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Validators/
│   ├── TradeValidator.cs
│   └── AdminValidator.cs
└── Program.cs
```

---

## Database Schema

### ER Diagram (Text Representation)

```
Exchange (1) ----< (M) Company (1) ----< (M) Share
Broker (1) ----< (M) Trade >---- (M) Share
Shareholder (1) ----< (M) Trade
```

### Tables

#### 1. Exchanges
```sql
CREATE TABLE Exchanges (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Code NVARCHAR(10) NOT NULL UNIQUE,
    Location NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);
```

#### 2. Companies
```sql
CREATE TABLE Companies (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Ticker NVARCHAR(10) NOT NULL UNIQUE,
    Sector NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    ExchangeId INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_Companies_Exchanges FOREIGN KEY (ExchangeId) REFERENCES Exchanges(Id) ON DELETE CASCADE
);
```

#### 3. Shares
```sql
CREATE TABLE Shares (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CompanyId INT NOT NULL,
    ShareType NVARCHAR(50) NOT NULL, -- 'Common Stock', 'Preferred Stock', etc.
    Quantity INT NOT NULL CHECK (Quantity >= 0),
    PricePerShare DECIMAL(18,2) NOT NULL CHECK (PricePerShare > 0),
    LastUpdated DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_Shares_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE
);
```

#### 4. Brokers
```sql
CREATE TABLE Brokers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL UNIQUE,
    Description NVARCHAR(MAX),
    ContactEmail NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);
```

#### 5. Shareholders
```sql
CREATE TABLE Shareholders (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(200) NOT NULL UNIQUE,
    PhoneNumber NVARCHAR(20),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
```

#### 6. Trades
```sql
CREATE TABLE Trades (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ShareholderId INT NOT NULL,
    ShareId INT NOT NULL,
    BrokerId INT NOT NULL,
    TradeType NVARCHAR(10) NOT NULL CHECK (TradeType IN ('Buy', 'Sell')),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PricePerShare DECIMAL(18,2) NOT NULL CHECK (PricePerShare > 0),
    TotalAmount DECIMAL(18,2) NOT NULL,
    TradeDate DATETIME2 DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'Completed',
    CONSTRAINT FK_Trades_Shareholders FOREIGN KEY (ShareholderId) REFERENCES Shareholders(Id),
    CONSTRAINT FK_Trades_Shares FOREIGN KEY (ShareId) REFERENCES Shares(Id),
    CONSTRAINT FK_Trades_Brokers FOREIGN KEY (BrokerId) REFERENCES Brokers(Id)
);
```

#### 7. Holdings (Computed/View)
```sql
CREATE VIEW Holdings AS
SELECT 
    t.ShareholderId,
    s.Id AS ShareId,
    s.CompanyId,
    c.Name AS CompanyName,
    c.Ticker,
    s.ShareType,
    SUM(CASE WHEN t.TradeType = 'Buy' THEN t.Quantity ELSE -t.Quantity END) AS TotalQuantity,
    AVG(CASE WHEN t.TradeType = 'Buy' THEN t.PricePerShare ELSE NULL END) AS AveragePurchasePrice,
    s.PricePerShare AS CurrentPrice
FROM Trades t
INNER JOIN Shares s ON t.ShareId = s.Id
INNER JOIN Companies c ON s.CompanyId = c.Id
GROUP BY t.ShareholderId, s.Id, s.CompanyId, c.Name, c.Ticker, s.ShareType, s.PricePerShare
HAVING SUM(CASE WHEN t.TradeType = 'Buy' THEN t.Quantity ELSE -t.Quantity END) > 0;
```

---

## Entity Models

### Exchange.cs
```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSphere.API.Models.Entities
{
    public class Exchange
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<Company> Companies { get; set; }
    }
}
```

### Company.cs
```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSphere.API.Models.Entities
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string Ticker { get; set; }

        [Required]
        [MaxLength(100)]
        public string Sector { get; set; }

        public string Description { get; set; }

        [Required]
        public int ExchangeId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ExchangeId")]
        public virtual Exchange Exchange { get; set; }
        public virtual ICollection<Share> Shares { get; set; }
    }
}
```

### Share.cs
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSphere.API.Models.Entities
{
    public class Share
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ShareType { get; set; } // "Common Stock", "Preferred Stock"

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal PricePerShare { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; }
    }
}
```

### Broker.cs
```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace ShareSphere.API.Models.Entities
{
    public class Broker
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string ContactEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### Shareholder.cs
```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace ShareSphere.API.Models.Entities
{
    public class Shareholder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }

        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### Trade.cs
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSphere.API.Models.Entities
{
    public class Trade
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShareholderId { get; set; }

        [Required]
        public int ShareId { get; set; }

        [Required]
        public int BrokerId { get; set; }

        [Required]
        [MaxLength(10)]
        public string TradeType { get; set; } // "Buy" or "Sell"

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerShare { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime TradeDate { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Completed";

        // Navigation properties
        [ForeignKey("ShareholderId")]
        public virtual Shareholder Shareholder { get; set; }

        [ForeignKey("ShareId")]
        public virtual Share Share { get; set; }

        [ForeignKey("BrokerId")]
        public virtual Broker Broker { get; set; }
    }
}
```

---

## DTOs (Data Transfer Objects)

### ExchangeDto.cs
```csharp
namespace ShareSphere.API.Models.DTOs
{
    public class ExchangeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
    }

    public class CreateExchangeDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; }

        public string Description { get; set; }
    }

    public class UpdateExchangeDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; }

        public string Description { get; set; }
    }
}
```

### CompanyDto.cs
```csharp
namespace ShareSphere.API.Models.DTOs
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Ticker { get; set; }
        public string Sector { get; set; }
        public string Description { get; set; }
        public int ExchangeId { get; set; }
        public string ExchangeName { get; set; }
    }

    public class CreateCompanyDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string Ticker { get; set; }

        [Required]
        [MaxLength(100)]
        public string Sector { get; set; }

        public string Description { get; set; }

        [Required]
        public int ExchangeId { get; set; }
    }

    public class UpdateCompanyDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string Ticker { get; set; }

        [Required]
        [MaxLength(100)]
        public string Sector { get; set; }

        public string Description { get; set; }

        [Required]
        public int ExchangeId { get; set; }
    }
}
```

### ShareDto.cs
```csharp
namespace ShareSphere.API.Models.DTOs
{
    public class ShareDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Ticker { get; set; }
        public string ShareType { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerShare { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class CreateShareDto
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ShareType { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PricePerShare { get; set; }
    }

    public class UpdateShareDto
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PricePerShare { get; set; }
    }
}
```

### BrokerDto.cs
```csharp
namespace ShareSphere.API.Models.DTOs
{
    public class BrokerDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
    }

    public class CreateBrokerDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string ContactEmail { get; set; }
    }

    public class UpdateBrokerDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string ContactEmail { get; set; }
    }
}
```

### TradeDto.cs
```csharp
namespace ShareSphere.API.Models.DTOs
{
    public class TradeDto
    {
        public int Id { get; set; }
        public string TradeType { get; set; }
        public string CompanyName { get; set; }
        public string Ticker { get; set; }
        public string ShareType { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerShare { get; set; }
        public decimal TotalAmount { get; set; }
        public string BrokerName { get; set; }
        public DateTime TradeDate { get; set; }
        public string Status { get; set; }
    }

    public class CreateTradeDto
    {
        [Required]
        public int ShareholderId { get; set; }

        [Required]
        public int ShareId { get; set; }

        [Required]
        public int BrokerId { get; set; }

        [Required]
        public string TradeType { get; set; } // "Buy" or "Sell"

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class TradeResponseDto
    {
        public int Id { get; set; }
        public string TradeType { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerShare { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TradeDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
```

### PortfolioDto.cs
```csharp
namespace ShareSphere.API.Models.DTOs
{
    public class PortfolioSummaryDto
    {
        public decimal TotalValue { get; set; }
        public int TotalShares { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercentage { get; set; }
    }

    public class HoldingDto
    {
        public int Id { get; set; }
        public int ShareId { get; set; }
        public string CompanyName { get; set; }
        public string Ticker { get; set; }
        public string ShareType { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue { get; set; }
    }
}
```

---

## API Endpoints Reference

### 1. Exchanges

#### GET /api/exchanges
**Purpose:** Get all stock exchanges  
**Frontend Usage:** Dashboard.tsx - Initial load  
**Response:**
```json
[
  {
    "id": 1,
    "name": "New York Stock Exchange",
    "code": "NYSE",
    "location": "New York, USA",
    "description": "The largest stock exchange in the world"
  }
]
```

#### GET /api/exchanges/{id}
**Purpose:** Get single exchange by ID  
**Response:**
```json
{
  "id": 1,
  "name": "New York Stock Exchange",
  "code": "NYSE",
  "location": "New York, USA",
  "description": "The largest stock exchange in the world"
}
```

#### GET /api/exchanges/{id}/companies
**Purpose:** Get all companies for an exchange  
**Frontend Usage:** Dashboard.tsx - When user selects exchange  
**Response:**
```json
[
  {
    "id": 101,
    "name": "Apple Inc.",
    "ticker": "AAPL",
    "sector": "Technology",
    "description": "Consumer electronics and software",
    "exchangeId": 1,
    "exchangeName": "NYSE"
  }
]
```

---

### 2. Companies

#### GET /api/companies
**Purpose:** Get all companies  
**Response:**
```json
[
  {
    "id": 101,
    "name": "Apple Inc.",
    "ticker": "AAPL",
    "sector": "Technology",
    "description": "Consumer electronics and software",
    "exchangeId": 1,
    "exchangeName": "NYSE"
  }
]
```

#### GET /api/companies/{id}
**Purpose:** Get single company  
**Response:**
```json
{
  "id": 101,
  "name": "Apple Inc.",
  "ticker": "AAPL",
  "sector": "Technology",
  "description": "Consumer electronics and software",
  "exchangeId": 1,
  "exchangeName": "NYSE"
}
```

#### GET /api/companies/{id}/shares
**Purpose:** Get all shares for a company  
**Frontend Usage:** Dashboard.tsx - When user selects company  
**Response:**
```json
[
  {
    "id": 1001,
    "companyId": 101,
    "companyName": "Apple Inc.",
    "ticker": "AAPL",
    "shareType": "Common Stock",
    "quantity": 1500,
    "pricePerShare": 175.50,
    "lastUpdated": "2025-12-15T10:30:00Z"
  }
]
```

---

### 3. Shares

#### GET /api/shares
**Purpose:** Get all available shares  
**Frontend Usage:** TradeForm.tsx - Populate share dropdown  
**Response:**
```json
[
  {
    "id": 1001,
    "companyId": 101,
    "companyName": "Apple Inc.",
    "ticker": "AAPL",
    "shareType": "Common Stock",
    "quantity": 1500,
    "pricePerShare": 175.50,
    "lastUpdated": "2025-12-15T10:30:00Z"
  }
]
```

#### GET /api/shares/{id}
**Purpose:** Get single share details  
**Response:**
```json
{
  "id": 1001,
  "companyId": 101,
  "companyName": "Apple Inc.",
  "ticker": "AAPL",
  "shareType": "Common Stock",
  "quantity": 1500,
  "pricePerShare": 175.50,
  "lastUpdated": "2025-12-15T10:30:00Z"
}
```

---

### 4. Brokers

#### GET /api/brokers
**Purpose:** Get all brokers  
**Frontend Usage:** TradeForm.tsx - Populate broker dropdown  
**Response:**
```json
[
  {
    "id": 1,
    "name": "E*TRADE",
    "description": "Leading online broker",
    "contactEmail": "support@etrade.com"
  }
]
```

---

### 5. Trades

#### POST /api/trades
**Purpose:** Execute a trade (Buy/Sell)  
**Frontend Usage:** TradeForm.tsx - On form submit  
**Request:**
```json
{
  "shareholderId": 1,
  "shareId": 1001,
  "brokerId": 1,
  "tradeType": "Buy",
  "quantity": 50
}
```
**Response:**
```json
{
  "id": 1,
  "tradeType": "Buy",
  "quantity": 50,
  "pricePerShare": 175.50,
  "totalAmount": 8775.00,
  "tradeDate": "2025-12-15T14:30:00Z",
  "status": "Completed",
  "message": "Trade executed successfully"
}
```

**Validation Rules:**
- Quantity must be > 0
- For Buy: Quantity must not exceed available shares
- For Sell: Shareholder must own enough shares
- Share must exist and be active
- Broker must exist and be active

#### GET /api/trades/shareholder/{shareholderId}
**Purpose:** Get all trades for a shareholder  
**Frontend Usage:** TradeHistory.tsx - Load trade history  
**Query Parameters:**
- `tradeType` (optional): "Buy" | "Sell" | "All"
- `startDate` (optional): ISO date string
- `endDate` (optional): ISO date string
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 10)

**Response:**
```json
{
  "trades": [
    {
      "id": 1,
      "tradeType": "Buy",
      "companyName": "Apple Inc.",
      "ticker": "AAPL",
      "shareType": "Common Stock",
      "quantity": 50,
      "pricePerShare": 165.20,
      "totalAmount": 8260.00,
      "brokerName": "E*TRADE",
      "tradeDate": "2025-12-10T14:30:00Z",
      "status": "Completed"
    }
  ],
  "totalCount": 25,
  "page": 1,
  "pageSize": 10
}
```

---

### 6. Portfolio

#### GET /api/portfolio/summary/{shareholderId}
**Purpose:** Get portfolio summary  
**Frontend Usage:** Portfolio.tsx - Summary cards  
**Response:**
```json
{
  "totalValue": 125750.50,
  "totalShares": 245,
  "changeAmount": 3250.75,
  "changePercentage": 2.65
}
```

**Calculation Logic:**
```csharp
// TotalValue = Sum of (CurrentPrice * Quantity) for all holdings
// TotalShares = Sum of Quantity for all holdings
// ChangeAmount = TotalValue - TotalInvestment (sum of purchase prices)
// ChangePercentage = (ChangeAmount / TotalInvestment) * 100
```

#### GET /api/portfolio/holdings/{shareholderId}
**Purpose:** Get all holdings  
**Frontend Usage:** Portfolio.tsx - Holdings table  
**Response:**
```json
[
  {
    "id": 1,
    "shareId": 1001,
    "companyName": "Apple Inc.",
    "ticker": "AAPL",
    "shareType": "Common Stock",
    "quantity": 50,
    "purchasePrice": 165.20,
    "currentPrice": 175.50,
    "totalValue": 8775.00
  }
]
```

**Calculation Logic:**
```csharp
// Query all Buy trades - Sell trades grouped by ShareId
// PurchasePrice = Average of all Buy prices
// CurrentPrice = Latest share price from Shares table
// TotalValue = CurrentPrice * Quantity
```

---

### 7. Admin Endpoints

#### Brokers Management

**GET /api/admin/brokers**
```json
[
  {
    "id": 1,
    "name": "E*TRADE",
    "description": "Leading online broker",
    "contactEmail": "support@etrade.com"
  }
]
```

**POST /api/admin/brokers**
**Request:**
```json
{
  "name": "E*TRADE",
  "description": "Leading online broker",
  "contactEmail": "support@etrade.com"
}
```
**Response:** 201 Created + BrokerDto

**PUT /api/admin/brokers/{id}**
**Request:**
```json
{
  "name": "E*TRADE",
  "description": "Updated description",
  "contactEmail": "support@etrade.com"
}
```
**Response:** 200 OK + BrokerDto

**DELETE /api/admin/brokers/{id}**
**Response:** 204 No Content

---

#### Exchanges Management

**GET /api/admin/exchanges**
**POST /api/admin/exchanges**
**PUT /api/admin/exchanges/{id}**
**DELETE /api/admin/exchanges/{id}**

Same pattern as Brokers, using ExchangeDto structures.

---

#### Companies Management

**GET /api/admin/companies**
**POST /api/admin/companies**
**PUT /api/admin/companies/{id}**
**DELETE /api/admin/companies/{id}**

Same pattern as Brokers, using CompanyDto structures.

---

## Controller Implementation

### ExchangesController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Data;
using ShareSphere.API.Models.DTOs;
using ShareSphere.API.Models.Entities;

namespace ShareSphere.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExchangesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/exchanges
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExchangeDto>>> GetExchanges()
        {
            var exchanges = await _context.Exchanges
                .Select(e => new ExchangeDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Code = e.Code,
                    Location = e.Location,
                    Description = e.Description
                })
                .ToListAsync();

            return Ok(exchanges);
        }

        // GET: api/exchanges/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExchangeDto>> GetExchange(int id)
        {
            var exchange = await _context.Exchanges
                .Where(e => e.Id == id)
                .Select(e => new ExchangeDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Code = e.Code,
                    Location = e.Location,
                    Description = e.Description
                })
                .FirstOrDefaultAsync();

            if (exchange == null)
            {
                return NotFound(new { message = $"Exchange with ID {id} not found" });
            }

            return Ok(exchange);
        }

        // GET: api/exchanges/5/companies
        [HttpGet("{id}/companies")]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetExchangeCompanies(int id)
        {
            var exchangeExists = await _context.Exchanges.AnyAsync(e => e.Id == id);
            if (!exchangeExists)
            {
                return NotFound(new { message = $"Exchange with ID {id} not found" });
            }

            var companies = await _context.Companies
                .Where(c => c.ExchangeId == id)
                .Include(c => c.Exchange)
                .Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Ticker = c.Ticker,
                    Sector = c.Sector,
                    Description = c.Description,
                    ExchangeId = c.ExchangeId,
                    ExchangeName = c.Exchange.Code
                })
                .ToListAsync();

            return Ok(companies);
        }
    }
}
```

### CompaniesController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Data;
using ShareSphere.API.Models.DTOs;

namespace ShareSphere.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompaniesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/companies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetCompanies()
        {
            var companies = await _context.Companies
                .Include(c => c.Exchange)
                .Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Ticker = c.Ticker,
                    Sector = c.Sector,
                    Description = c.Description,
                    ExchangeId = c.ExchangeId,
                    ExchangeName = c.Exchange.Code
                })
                .ToListAsync();

            return Ok(companies);
        }

        // GET: api/companies/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyDto>> GetCompany(int id)
        {
            var company = await _context.Companies
                .Include(c => c.Exchange)
                .Where(c => c.Id == id)
                .Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Ticker = c.Ticker,
                    Sector = c.Sector,
                    Description = c.Description,
                    ExchangeId = c.ExchangeId,
                    ExchangeName = c.Exchange.Code
                })
                .FirstOrDefaultAsync();

            if (company == null)
            {
                return NotFound(new { message = $"Company with ID {id} not found" });
            }

            return Ok(company);
        }

        // GET: api/companies/5/shares
        [HttpGet("{id}/shares")]
        public async Task<ActionResult<IEnumerable<ShareDto>>> GetCompanyShares(int id)
        {
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == id);
            if (!companyExists)
            {
                return NotFound(new { message = $"Company with ID {id} not found" });
            }

            var shares = await _context.Shares
                .Where(s => s.CompanyId == id)
                .Include(s => s.Company)
                .Select(s => new ShareDto
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    CompanyName = s.Company.Name,
                    Ticker = s.Company.Ticker,
                    ShareType = s.ShareType,
                    Quantity = s.Quantity,
                    PricePerShare = s.PricePerShare,
                    LastUpdated = s.LastUpdated
                })
                .ToListAsync();

            return Ok(shares);
        }
    }
}
```

### SharesController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Data;
using ShareSphere.API.Models.DTOs;

namespace ShareSphere.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SharesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SharesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/shares
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShareDto>>> GetShares()
        {
            var shares = await _context.Shares
                .Include(s => s.Company)
                .Select(s => new ShareDto
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    CompanyName = s.Company.Name,
                    Ticker = s.Company.Ticker,
                    ShareType = s.ShareType,
                    Quantity = s.Quantity,
                    PricePerShare = s.PricePerShare,
                    LastUpdated = s.LastUpdated
                })
                .ToListAsync();

            return Ok(shares);
        }

        // GET: api/shares/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ShareDto>> GetShare(int id)
        {
            var share = await _context.Shares
                .Include(s => s.Company)
                .Where(s => s.Id == id)
                .Select(s => new ShareDto
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    CompanyName = s.Company.Name,
                    Ticker = s.Company.Ticker,
                    ShareType = s.ShareType,
                    Quantity = s.Quantity,
                    PricePerShare = s.PricePerShare,
                    LastUpdated = s.LastUpdated
                })
                .FirstOrDefaultAsync();

            if (share == null)
            {
                return NotFound(new { message = $"Share with ID {id} not found" });
            }

            return Ok(share);
        }
    }
}
```

### TradesController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Data;
using ShareSphere.API.Models.DTOs;
using ShareSphere.API.Models.Entities;

namespace ShareSphere.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TradesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TradesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/trades
        [HttpPost]
        public async Task<ActionResult<TradeResponseDto>> ExecuteTrade(CreateTradeDto tradeDto)
        {
            // Validate share exists
            var share = await _context.Shares
                .Include(s => s.Company)
                .FirstOrDefaultAsync(s => s.Id == tradeDto.ShareId);

            if (share == null)
            {
                return BadRequest(new { message = "Invalid share ID" });
            }

            // Validate broker exists
            var brokerExists = await _context.Brokers.AnyAsync(b => b.Id == tradeDto.BrokerId);
            if (!brokerExists)
            {
                return BadRequest(new { message = "Invalid broker ID" });
            }

            // Validate shareholder exists
            var shareholderExists = await _context.Shareholders.AnyAsync(s => s.Id == tradeDto.ShareholderId);
            if (!shareholderExists)
            {
                return BadRequest(new { message = "Invalid shareholder ID" });
            }

            // Validate trade type
            if (tradeDto.TradeType != "Buy" && tradeDto.TradeType != "Sell")
            {
                return BadRequest(new { message = "Trade type must be 'Buy' or 'Sell'" });
            }

            // For Buy: Check available quantity
            if (tradeDto.TradeType == "Buy" && tradeDto.Quantity > share.Quantity)
            {
                return BadRequest(new { message = $"Only {share.Quantity} shares available" });
            }

            // For Sell: Check shareholder holdings
            if (tradeDto.TradeType == "Sell")
            {
                var holdings = await GetShareholderHoldings(tradeDto.ShareholderId, tradeDto.ShareId);
                if (holdings < tradeDto.Quantity)
                {
                    return BadRequest(new { message = $"Insufficient shares. You own {holdings} shares" });
                }
            }

            // Calculate total amount
            var totalAmount = share.PricePerShare * tradeDto.Quantity;

            // Create trade
            var trade = new Trade
            {
                ShareholderId = tradeDto.ShareholderId,
                ShareId = tradeDto.ShareId,
                BrokerId = tradeDto.BrokerId,
                TradeType = tradeDto.TradeType,
                Quantity = tradeDto.Quantity,
                PricePerShare = share.PricePerShare,
                TotalAmount = totalAmount,
                TradeDate = DateTime.UtcNow,
                Status = "Completed"
            };

            _context.Trades.Add(trade);

            // Update share quantity
            if (tradeDto.TradeType == "Buy")
            {
                share.Quantity -= tradeDto.Quantity;
            }
            else // Sell
            {
                share.Quantity += tradeDto.Quantity;
            }
            share.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new TradeResponseDto
            {
                Id = trade.Id,
                TradeType = trade.TradeType,
                Quantity = trade.Quantity,
                PricePerShare = trade.PricePerShare,
                TotalAmount = trade.TotalAmount,
                TradeDate = trade.TradeDate,
                Status = trade.Status,
                Message = "Trade executed successfully"
            };

            return CreatedAtAction(nameof(GetTrade), new { id = trade.Id }, response);
        }

        // GET: api/trades/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TradeDto>> GetTrade(int id)
        {
            var trade = await _context.Trades
                .Include(t => t.Share)
                    .ThenInclude(s => s.Company)
                .Include(t => t.Broker)
                .Where(t => t.Id == id)
                .Select(t => new TradeDto
                {
                    Id = t.Id,
                    TradeType = t.TradeType,
                    CompanyName = t.Share.Company.Name,
                    Ticker = t.Share.Company.Ticker,
                    ShareType = t.Share.ShareType,
                    Quantity = t.Quantity,
                    PricePerShare = t.PricePerShare,
                    TotalAmount = t.TotalAmount,
                    BrokerName = t.Broker.Name,
                    TradeDate = t.TradeDate,
                    Status = t.Status
                })
                .FirstOrDefaultAsync();

            if (trade == null)
            {
                return NotFound();
            }

            return Ok(trade);
        }

        // GET: api/trades/shareholder/1?tradeType=All&startDate=2025-01-01&page=1&pageSize=10
        [HttpGet("shareholder/{shareholderId}")]
        public async Task<ActionResult<object>> GetShareholderTrades(
            int shareholderId,
            [FromQuery] string tradeType = "All",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Trades
                .Include(t => t.Share)
                    .ThenInclude(s => s.Company)
                .Include(t => t.Broker)
                .Where(t => t.ShareholderId == shareholderId);

            // Filter by trade type
            if (tradeType != "All")
            {
                query = query.Where(t => t.TradeType == tradeType);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(t => t.TradeDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.TradeDate <= endDate.Value);
            }

            var totalCount = await query.CountAsync();

            var trades = await query
                .OrderByDescending(t => t.TradeDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TradeDto
                {
                    Id = t.Id,
                    TradeType = t.TradeType,
                    CompanyName = t.Share.Company.Name,
                    Ticker = t.Share.Company.Ticker,
                    ShareType = t.Share.ShareType,
                    Quantity = t.Quantity,
                    PricePerShare = t.PricePerShare,
                    TotalAmount = t.TotalAmount,
                    BrokerName = t.Broker.Name,
                    TradeDate = t.TradeDate,
                    Status = t.Status
                })
                .ToListAsync();

            return Ok(new
            {
                trades,
                totalCount,
                page,
                pageSize
            });
        }

        private async Task<int> GetShareholderHoldings(int shareholderId, int shareId)
        {
            var buyQuantity = await _context.Trades
                .Where(t => t.ShareholderId == shareholderId 
                    && t.ShareId == shareId 
                    && t.TradeType == "Buy")
                .SumAsync(t => (int?)t.Quantity) ?? 0;

            var sellQuantity = await _context.Trades
                .Where(t => t.ShareholderId == shareholderId 
                    && t.ShareId == shareId 
                    && t.TradeType == "Sell")
                .SumAsync(t => (int?)t.Quantity) ?? 0;

            return buyQuantity - sellQuantity;
        }
    }
}
```

### PortfolioController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Data;
using ShareSphere.API.Models.DTOs;

namespace ShareSphere.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PortfolioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/portfolio/summary/1
        [HttpGet("summary/{shareholderId}")]
        public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolioSummary(int shareholderId)
        {
            var holdings = await GetHoldingsData(shareholderId);

            if (!holdings.Any())
            {
                return Ok(new PortfolioSummaryDto
                {
                    TotalValue = 0,
                    TotalShares = 0,
                    ChangeAmount = 0,
                    ChangePercentage = 0
                });
            }

            var totalValue = holdings.Sum(h => h.CurrentPrice * h.Quantity);
            var totalInvestment = holdings.Sum(h => h.PurchasePrice * h.Quantity);
            var totalShares = holdings.Sum(h => h.Quantity);
            var changeAmount = totalValue - totalInvestment;
            var changePercentage = totalInvestment > 0 ? (changeAmount / totalInvestment) * 100 : 0;

            return Ok(new PortfolioSummaryDto
            {
                TotalValue = totalValue,
                TotalShares = totalShares,
                ChangeAmount = changeAmount,
                ChangePercentage = changePercentage
            });
        }

        // GET: api/portfolio/holdings/1
        [HttpGet("holdings/{shareholderId}")]
        public async Task<ActionResult<IEnumerable<HoldingDto>>> GetHoldings(int shareholderId)
        {
            var holdings = await GetHoldingsData(shareholderId);

            var holdingDtos = holdings.Select((h, index) => new HoldingDto
            {
                Id = index + 1,
                ShareId = h.ShareId,
                CompanyName = h.CompanyName,
                Ticker = h.Ticker,
                ShareType = h.ShareType,
                Quantity = h.Quantity,
                PurchasePrice = h.PurchasePrice,
                CurrentPrice = h.CurrentPrice,
                TotalValue = h.CurrentPrice * h.Quantity
            }).ToList();

            return Ok(holdingDtos);
        }

        private async Task<List<HoldingData>> GetHoldingsData(int shareholderId)
        {
            var trades = await _context.Trades
                .Include(t => t.Share)
                    .ThenInclude(s => s.Company)
                .Where(t => t.ShareholderId == shareholderId)
                .GroupBy(t => new
                {
                    t.ShareId,
                    t.Share.Company.Name,
                    t.Share.Company.Ticker,
                    t.Share.ShareType,
                    t.Share.PricePerShare
                })
                .Select(g => new HoldingData
                {
                    ShareId = g.Key.ShareId,
                    CompanyName = g.Key.Name,
                    Ticker = g.Key.Ticker,
                    ShareType = g.Key.ShareType,
                    Quantity = g.Where(t => t.TradeType == "Buy").Sum(t => t.Quantity) -
                               g.Where(t => t.TradeType == "Sell").Sum(t => t.Quantity),
                    PurchasePrice = g.Where(t => t.TradeType == "Buy").Average(t => t.PricePerShare),
                    CurrentPrice = g.Key.PricePerShare
                })
                .Where(h => h.Quantity > 0)
                .ToListAsync();

            return trades;
        }

        private class HoldingData
        {
            public int ShareId { get; set; }
            public string CompanyName { get; set; }
            public string Ticker { get; set; }
            public string ShareType { get; set; }
            public int Quantity { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal CurrentPrice { get; set; }
        }
    }
}
```

### AdminController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Data;
using ShareSphere.API.Models.DTOs;
using ShareSphere.API.Models.Entities;

namespace ShareSphere.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Brokers

        // GET: api/admin/brokers
        [HttpGet("brokers")]
        public async Task<ActionResult<IEnumerable<BrokerDto>>> GetBrokers()
        {
            var brokers = await _context.Brokers
                .Select(b => new BrokerDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    ContactEmail = b.ContactEmail
                })
                .ToListAsync();

            return Ok(brokers);
        }

        // POST: api/admin/brokers
        [HttpPost("brokers")]
        public async Task<ActionResult<BrokerDto>> CreateBroker(CreateBrokerDto dto)
        {
            // Check for duplicate name
            if (await _context.Brokers.AnyAsync(b => b.Name == dto.Name))
            {
                return BadRequest(new { message = "A broker with this name already exists" });
            }

            var broker = new Broker
            {
                Name = dto.Name,
                Description = dto.Description,
                ContactEmail = dto.ContactEmail,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Brokers.Add(broker);
            await _context.SaveChangesAsync();

            var result = new BrokerDto
            {
                Id = broker.Id,
                Name = broker.Name,
                Description = broker.Description,
                ContactEmail = broker.ContactEmail
            };

            return CreatedAtAction(nameof(GetBrokers), new { id = broker.Id }, result);
        }

        // PUT: api/admin/brokers/5
        [HttpPut("brokers/{id}")]
        public async Task<ActionResult<BrokerDto>> UpdateBroker(int id, UpdateBrokerDto dto)
        {
            var broker = await _context.Brokers.FindAsync(id);
            if (broker == null)
            {
                return NotFound(new { message = $"Broker with ID {id} not found" });
            }

            // Check for duplicate name (excluding current broker)
            if (await _context.Brokers.AnyAsync(b => b.Name == dto.Name && b.Id != id))
            {
                return BadRequest(new { message = "A broker with this name already exists" });
            }

            broker.Name = dto.Name;
            broker.Description = dto.Description;
            broker.ContactEmail = dto.ContactEmail;
            broker.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new BrokerDto
            {
                Id = broker.Id,
                Name = broker.Name,
                Description = broker.Description,
                ContactEmail = broker.ContactEmail
            };

            return Ok(result);
        }

        // DELETE: api/admin/brokers/5
        [HttpDelete("brokers/{id}")]
        public async Task<IActionResult> DeleteBroker(int id)
        {
            var broker = await _context.Brokers.FindAsync(id);
            if (broker == null)
            {
                return NotFound(new { message = $"Broker with ID {id} not found" });
            }

            _context.Brokers.Remove(broker);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Exchanges

        // GET: api/admin/exchanges
        [HttpGet("exchanges")]
        public async Task<ActionResult<IEnumerable<ExchangeDto>>> GetExchanges()
        {
            var exchanges = await _context.Exchanges
                .Select(e => new ExchangeDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Code = e.Code,
                    Location = e.Location,
                    Description = e.Description
                })
                .ToListAsync();

            return Ok(exchanges);
        }

        // POST: api/admin/exchanges
        [HttpPost("exchanges")]
        public async Task<ActionResult<ExchangeDto>> CreateExchange(CreateExchangeDto dto)
        {
            // Check for duplicate code
            if (await _context.Exchanges.AnyAsync(e => e.Code == dto.Code))
            {
                return BadRequest(new { message = "An exchange with this code already exists" });
            }

            var exchange = new Exchange
            {
                Name = dto.Name,
                Code = dto.Code.ToUpper(),
                Location = dto.Location,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Exchanges.Add(exchange);
            await _context.SaveChangesAsync();

            var result = new ExchangeDto
            {
                Id = exchange.Id,
                Name = exchange.Name,
                Code = exchange.Code,
                Location = exchange.Location,
                Description = exchange.Description
            };

            return CreatedAtAction(nameof(GetExchanges), new { id = exchange.Id }, result);
        }

        // PUT: api/admin/exchanges/5
        [HttpPut("exchanges/{id}")]
        public async Task<ActionResult<ExchangeDto>> UpdateExchange(int id, UpdateExchangeDto dto)
        {
            var exchange = await _context.Exchanges.FindAsync(id);
            if (exchange == null)
            {
                return NotFound(new { message = $"Exchange with ID {id} not found" });
            }

            // Check for duplicate code (excluding current exchange)
            if (await _context.Exchanges.AnyAsync(e => e.Code == dto.Code && e.Id != id))
            {
                return BadRequest(new { message = "An exchange with this code already exists" });
            }

            exchange.Name = dto.Name;
            exchange.Code = dto.Code.ToUpper();
            exchange.Location = dto.Location;
            exchange.Description = dto.Description;
            exchange.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new ExchangeDto
            {
                Id = exchange.Id,
                Name = exchange.Name,
                Code = exchange.Code,
                Location = exchange.Location,
                Description = exchange.Description
            };

            return Ok(result);
        }

        // DELETE: api/admin/exchanges/5
        [HttpDelete("exchanges/{id}")]
        public async Task<IActionResult> DeleteExchange(int id)
        {
            var exchange = await _context.Exchanges.FindAsync(id);
            if (exchange == null)
            {
                return NotFound(new { message = $"Exchange with ID {id} not found" });
            }

            _context.Exchanges.Remove(exchange);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Companies

        // GET: api/admin/companies
        [HttpGet("companies")]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetCompanies()
        {
            var companies = await _context.Companies
                .Include(c => c.Exchange)
                .Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Ticker = c.Ticker,
                    Sector = c.Sector,
                    Description = c.Description,
                    ExchangeId = c.ExchangeId,
                    ExchangeName = c.Exchange.Code
                })
                .ToListAsync();

            return Ok(companies);
        }

        // POST: api/admin/companies
        [HttpPost("companies")]
        public async Task<ActionResult<CompanyDto>> CreateCompany(CreateCompanyDto dto)
        {
            // Validate exchange exists
            var exchange = await _context.Exchanges.FindAsync(dto.ExchangeId);
            if (exchange == null)
            {
                return BadRequest(new { message = "Invalid exchange ID" });
            }

            // Check for duplicate ticker
            if (await _context.Companies.AnyAsync(c => c.Ticker == dto.Ticker))
            {
                return BadRequest(new { message = "A company with this ticker already exists" });
            }

            var company = new Company
            {
                Name = dto.Name,
                Ticker = dto.Ticker.ToUpper(),
                Sector = dto.Sector,
                Description = dto.Description,
                ExchangeId = dto.ExchangeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var result = new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Ticker = company.Ticker,
                Sector = company.Sector,
                Description = company.Description,
                ExchangeId = company.ExchangeId,
                ExchangeName = exchange.Code
            };

            return CreatedAtAction(nameof(GetCompanies), new { id = company.Id }, result);
        }

        // PUT: api/admin/companies/5
        [HttpPut("companies/{id}")]
        public async Task<ActionResult<CompanyDto>> UpdateCompany(int id, UpdateCompanyDto dto)
        {
            var company = await _context.Companies.Include(c => c.Exchange).FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
            {
                return NotFound(new { message = $"Company with ID {id} not found" });
            }

            // Validate exchange exists
            var exchange = await _context.Exchanges.FindAsync(dto.ExchangeId);
            if (exchange == null)
            {
                return BadRequest(new { message = "Invalid exchange ID" });
            }

            // Check for duplicate ticker (excluding current company)
            if (await _context.Companies.AnyAsync(c => c.Ticker == dto.Ticker && c.Id != id))
            {
                return BadRequest(new { message = "A company with this ticker already exists" });
            }

            company.Name = dto.Name;
            company.Ticker = dto.Ticker.ToUpper();
            company.Sector = dto.Sector;
            company.Description = dto.Description;
            company.ExchangeId = dto.ExchangeId;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Ticker = company.Ticker,
                Sector = company.Sector,
                Description = company.Description,
                ExchangeId = company.ExchangeId,
                ExchangeName = exchange.Code
            };

            return Ok(result);
        }

        // DELETE: api/admin/companies/5
        [HttpDelete("companies/{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound(new { message = $"Company with ID {id} not found" });
            }

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion
    }
}
```

---

## ApplicationDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Models.Entities;

namespace ShareSphere.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Exchange> Exchanges { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Share> Shares { get; set; }
        public DbSet<Broker> Brokers { get; set; }
        public DbSet<Shareholder> Shareholders { get; set; }
        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indexes for performance
            modelBuilder.Entity<Exchange>()
                .HasIndex(e => e.Code)
                .IsUnique();

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Ticker)
                .IsUnique();

            modelBuilder.Entity<Broker>()
                .HasIndex(b => b.Name)
                .IsUnique();

            modelBuilder.Entity<Shareholder>()
                .HasIndex(s => s.Email)
                .IsUnique();

            // Configure cascade delete behavior
            modelBuilder.Entity<Company>()
                .HasOne(c => c.Exchange)
                .WithMany(e => e.Companies)
                .HasForeignKey(c => c.ExchangeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Share>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Shares)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed initial data (optional)
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Exchanges
            modelBuilder.Entity<Exchange>().HasData(
                new Exchange { Id = 1, Name = "New York Stock Exchange", Code = "NYSE", Location = "New York, USA", Description = "The largest stock exchange in the world", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Exchange { Id = 2, Name = "NASDAQ", Code = "NASDAQ", Location = "New York, USA", Description = "Technology-focused stock exchange", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Exchange { Id = 3, Name = "London Stock Exchange", Code = "LSE", Location = "London, UK", Description = "One of the oldest stock exchanges globally", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );

            // Seed Brokers
            modelBuilder.Entity<Broker>().HasData(
                new Broker { Id = 1, Name = "E*TRADE", Description = "Leading online broker", ContactEmail = "support@etrade.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Broker { Id = 2, Name = "TD Ameritrade", Description = "Full-service brokerage", ContactEmail = "help@tdameritrade.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Broker { Id = 3, Name = "Robinhood", Description = "Commission-free trading", ContactEmail = "support@robinhood.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );

            // Seed Companies
            modelBuilder.Entity<Company>().HasData(
                new Company { Id = 101, Name = "Apple Inc.", Ticker = "AAPL", Sector = "Technology", Description = "Consumer electronics and software", ExchangeId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Company { Id = 102, Name = "Microsoft Corporation", Ticker = "MSFT", Sector = "Technology", Description = "Software and cloud services", ExchangeId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Company { Id = 201, Name = "Amazon.com Inc.", Ticker = "AMZN", Sector = "E-commerce", Description = "Online retail and cloud computing", ExchangeId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );

            // Seed Shares
            modelBuilder.Entity<Share>().HasData(
                new Share { Id = 1001, CompanyId = 101, ShareType = "Common Stock", Quantity = 1500, PricePerShare = 175.50m, LastUpdated = DateTime.UtcNow },
                new Share { Id = 1002, CompanyId = 101, ShareType = "Preferred Stock", Quantity = 500, PricePerShare = 185.75m, LastUpdated = DateTime.UtcNow },
                new Share { Id = 1003, CompanyId = 102, ShareType = "Common Stock", Quantity = 2000, PricePerShare = 380.25m, LastUpdated = DateTime.UtcNow },
                new Share { Id = 1004, CompanyId = 201, ShareType = "Common Stock", Quantity = 1200, PricePerShare = 145.30m, LastUpdated = DateTime.UtcNow }
            );

            // Seed test Shareholder
            modelBuilder.Entity<Shareholder>().HasData(
                new Shareholder { Id = 1, FirstName = "Demo", LastName = "User", Email = "demo@sharesphere.com", PhoneNumber = "+1234567890", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}
```

---

## Program.cs Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using ShareSphere.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Vite/React dev servers
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ShareSphereDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Validation Rules Summary

### Trade Validation
1. **Quantity**: Must be > 0 and integer
2. **Buy Trades**: Available share quantity must be >= requested quantity
3. **Sell Trades**: Shareholder holdings must be >= requested quantity
4. **Share**: Must exist and be active
5. **Broker**: Must exist
6. **Shareholder**: Must exist
7. **TradeType**: Must be "Buy" or "Sell"

### Admin Validation
1. **Exchange Code**: Must be unique
2. **Company Ticker**: Must be unique
3. **Broker Name**: Must be unique
4. **Email**: Must be valid email format
5. **All Text Fields**: Required unless specified optional

---

## Error Handling

### Standard Error Response Format
```json
{
  "message": "Error description",
  "errors": {
    "fieldName": ["Error message 1", "Error message 2"]
  }
}
```

### HTTP Status Codes
- `200 OK`: Successful GET/PUT requests
- `201 Created`: Successful POST requests
- `204 No Content`: Successful DELETE requests
- `400 Bad Request`: Validation errors
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server errors

---

## Entity Framework Migrations

### Initial Migration
```bash
# In Package Manager Console
Add-Migration InitialCreate
Update-Database

# Or with dotnet CLI
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Subsequent Migrations
```bash
Add-Migration AddNewFeature
Update-Database
```

---

## Testing with Postman

### Import Collection Structure
```
ShareSphere API/
├── Exchanges/
│   ├── GET All Exchanges
│   ├── GET Exchange by ID
│   └── GET Exchange Companies
├── Companies/
│   ├── GET All Companies
│   ├── GET Company by ID
│   └── GET Company Shares
├── Shares/
│   ├── GET All Shares
│   └── GET Share by ID
├── Brokers/
│   └── GET All Brokers
├── Trades/
│   ├── POST Execute Trade
│   ├── GET Trade by ID
│   └── GET Shareholder Trades
├── Portfolio/
│   ├── GET Portfolio Summary
│   └── GET Holdings
└── Admin/
    ├── Brokers CRUD
    ├── Exchanges CRUD
    └── Companies CRUD
```

---

## Performance Considerations

1. **Indexes**: Created on frequently queried fields (Code, Ticker, Email)
2. **Eager Loading**: Use `.Include()` for related data to avoid N+1 queries
3. **Pagination**: Implemented for trade history
4. **Async Operations**: All database operations are asynchronous
5. **Connection Pooling**: Enabled by default in EF Core

---

## Security Recommendations

1. **Authentication**: Implement JWT authentication for API endpoints
2. **Authorization**: Add role-based authorization for admin endpoints
3. **Input Validation**: Use Data Annotations and FluentValidation
4. **SQL Injection**: Use parameterized queries (EF Core handles this)
5. **HTTPS**: Enforce HTTPS in production
6. **Rate Limiting**: Implement to prevent abuse
7. **API Keys**: Consider for external integrations

---

## Deployment Checklist

- [ ] Update connection string for production database
- [ ] Remove or secure Swagger in production
- [ ] Configure proper CORS origins
- [ ] Enable HTTPS redirection
- [ ] Set up logging (Application Insights, Serilog)
- [ ] Configure error handling middleware
- [ ] Set up database backups
- [ ] Implement health checks
- [ ] Configure application settings per environment

---

## Additional Resources

- **Entity Framework Core**: https://docs.microsoft.com/ef/core/
- **ASP.NET Core**: https://docs.microsoft.com/aspnet/core/
- **SQL Server**: https://docs.microsoft.com/sql/
- **Postman**: https://www.postman.com/

---

**Document Version**: 1.0  
**Last Updated**: December 15, 2025  
**Author**: ShareSphere Development Team
