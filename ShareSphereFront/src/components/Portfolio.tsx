import React, { useState, useEffect } from 'react';
import { Loader2, TrendingUp, TrendingDown, DollarSign } from 'lucide-react';
import { HoldingsTable } from './HoldingsTable';
import { TradeHistory } from './TradeHistory';
import { EmptyState } from './EmptyState';

// Mock API data structures
// API Endpoint: GET /api/portfolio/summary
const mockPortfolioSummary = {
  totalValue: 125750.50,
  totalShares: 245,
  changeAmount: 3250.75,
  changePercentage: 2.65,
};

// API Endpoint: GET /api/portfolio/holdings
const mockHoldings = [
  {
    id: 1,
    shareId: 1001,
    companyName: 'Apple Inc.',
    ticker: 'AAPL',
    shareType: 'Common Stock',
    quantity: 50,
    purchasePrice: 165.20,
    currentPrice: 175.50,
    totalValue: 8775.00,
  },
  {
    id: 2,
    shareId: 1003,
    companyName: 'Microsoft Corporation',
    ticker: 'MSFT',
    shareType: 'Common Stock',
    quantity: 75,
    purchasePrice: 350.80,
    currentPrice: 380.25,
    totalValue: 28518.75,
  },
  {
    id: 3,
    shareId: 1005,
    companyName: 'Amazon.com Inc.',
    ticker: 'AMZN',
    shareType: 'Common Stock',
    quantity: 120,
    purchasePrice: 140.50,
    currentPrice: 145.30,
    totalValue: 17436.00,
  },
];

// API Endpoint: GET /api/portfolio/trades
const mockTrades = [
  {
    id: 1,
    tradeType: 'Buy',
    companyName: 'Apple Inc.',
    ticker: 'AAPL',
    shareType: 'Common Stock',
    quantity: 50,
    pricePerShare: 165.20,
    totalAmount: 8260.00,
    brokerName: 'E*TRADE',
    tradeDate: '2025-12-10T14:30:00Z',
    status: 'Completed',
  },
  {
    id: 2,
    tradeType: 'Buy',
    companyName: 'Microsoft Corporation',
    ticker: 'MSFT',
    shareType: 'Common Stock',
    quantity: 75,
    pricePerShare: 350.80,
    totalAmount: 26310.00,
    brokerName: 'TD Ameritrade',
    tradeDate: '2025-12-12T10:15:00Z',
    status: 'Completed',
  },
  {
    id: 3,
    tradeType: 'Sell',
    companyName: 'Tesla Inc.',
    ticker: 'TSLA',
    shareType: 'Common Stock',
    quantity: 25,
    pricePerShare: 238.50,
    totalAmount: 5962.50,
    brokerName: 'Robinhood',
    tradeDate: '2025-12-13T16:45:00Z',
    status: 'Completed',
  },
  {
    id: 4,
    tradeType: 'Buy',
    companyName: 'Amazon.com Inc.',
    ticker: 'AMZN',
    shareType: 'Common Stock',
    quantity: 120,
    pricePerShare: 140.50,
    totalAmount: 16860.00,
    brokerName: 'E*TRADE',
    tradeDate: '2025-12-14T09:20:00Z',
    status: 'Completed',
  },
];

export function Portfolio() {
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<any>(null);
  const [holdings, setHoldings] = useState<any[]>([]);
  const [trades, setTrades] = useState<any[]>([]);

  // Simulated API calls
  useEffect(() => {
    const fetchPortfolioData = async () => {
      setLoading(true);
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 800));
      
      setSummary(mockPortfolioSummary);
      setHoldings(mockHoldings);
      setTrades(mockTrades);
      setLoading(false);
    };

    fetchPortfolioData();
  }, []);

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(value);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
          <p className="text-gray-600">Loading portfolio...</p>
        </div>
      </div>
    );
  }

  const hasHoldings = holdings.length > 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-gray-900 mb-2">Portfolio</h1>
        <p className="text-gray-600">Track your holdings and investment performance</p>
      </div>

      {/* Portfolio Summary */}
      {hasHoldings ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-gray-600">Total Portfolio Value</span>
              <DollarSign className="w-5 h-5 text-blue-600" />
            </div>
            <div className="text-gray-900 mb-1">{formatCurrency(summary.totalValue)}</div>
            <div className={`flex items-center gap-1 text-sm ${summary.changeAmount >= 0 ? 'text-green-600' : 'text-red-600'}`}>
              {summary.changeAmount >= 0 ? (
                <TrendingUp className="w-4 h-4" />
              ) : (
                <TrendingDown className="w-4 h-4" />
              )}
              <span>
                {formatCurrency(Math.abs(summary.changeAmount))} ({Math.abs(summary.changePercentage).toFixed(2)}%)
              </span>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-gray-600">Total Shares Owned</span>
              <TrendingUp className="w-5 h-5 text-blue-600" />
            </div>
            <div className="text-gray-900">{summary.totalShares.toLocaleString()}</div>
            <div className="text-sm text-gray-500">Across {holdings.length} holdings</div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-gray-600">Total Trades</span>
              <TrendingUp className="w-5 h-5 text-blue-600" />
            </div>
            <div className="text-gray-900">{trades.length}</div>
            <div className="text-sm text-gray-500">All time</div>
          </div>
        </div>
      ) : null}

      {/* Holdings Table */}
      <div>
        <h2 className="text-gray-900 mb-4">Holdings</h2>
        {hasHoldings ? (
          <HoldingsTable holdings={holdings} />
        ) : (
          <EmptyState
            icon={TrendingUp}
            title="No holdings yet"
            description="You haven't purchased any shares yet. Start trading to build your portfolio."
            action={{
              label: 'Start Trading',
              onClick: () => window.location.href = '/trade',
            }}
          />
        )}
      </div>

      {/* Trade History */}
      <div>
        <h2 className="text-gray-900 mb-4">Trade History</h2>
        <TradeHistory trades={trades} />
      </div>
    </div>
  );
}
