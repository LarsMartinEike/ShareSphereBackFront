namespace ShareSphere.Api.Models. Dtos
{
    /// <summary>
    /// Portfolio-Übersicht mit allen Shares
    /// </summary>
    public record ShareholderPortfolioDto
    {
        public int ShareholderId { get; init; }
        public string ShareholderName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public decimal TotalPortfolioValue { get; init; }
        public List<OwnedShareDto> OwnedShares { get; init; } = new();
        public int TotalSharesCount { get; init; }
    }

    /// <summary>
    /// Einzelner Share im Portfolio
    /// </summary>
    public record OwnedShareDto
    {
        public int ShareId { get; init; }
        public int CompanyId { get; init; }
        public string CompanyName { get; init; } = string.Empty;
        public string TickerSymbol { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal CurrentPricePerShare { get; init; }
        public decimal TotalValue { get; init; }
        public string StockExchange { get; init; } = string.Empty;
    }
}