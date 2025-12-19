import React, { useState } from 'react';
import { Calendar, Filter, ChevronLeft, ChevronRight, ArrowUpDown, ArrowDownUp } from 'lucide-react';
import { EmptyState } from './EmptyState';

interface TradeHistoryProps {
  trades: Array<{
    id: number;
    tradeType: 'Buy' | 'Sell';
    companyName: string;
    ticker: string;
    shareType: string;
    quantity: number;
    pricePerShare: number;
    totalAmount: number;
    brokerName: string;
    tradeDate: string;
    status: string;
  }>;
}

export function TradeHistory({ trades }: TradeHistoryProps) {
  const [filterType, setFilterType] = useState<'All' | 'Buy' | 'Sell'>('All');
  const [dateRange, setDateRange] = useState<'All' | 'Today' | 'Week' | 'Month'>('All');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(value);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  };

  const filterByDateRange = (trade: any) => {
    if (dateRange === 'All') return true;
    
    const tradeDate = new Date(trade.tradeDate);
    const now = new Date();
    
    if (dateRange === 'Today') {
      return tradeDate.toDateString() === now.toDateString();
    } else if (dateRange === 'Week') {
      const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
      return tradeDate >= weekAgo;
    } else if (dateRange === 'Month') {
      const monthAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
      return tradeDate >= monthAgo;
    }
    
    return true;
  };

  const filteredTrades = trades
    .filter(trade => filterType === 'All' || trade.tradeType === filterType)
    .filter(filterByDateRange);

  const totalPages = Math.ceil(filteredTrades.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const paginatedTrades = filteredTrades.slice(startIndex, startIndex + itemsPerPage);

  const handlePageChange = (page: number) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)));
  };

  if (trades.length === 0) {
    return (
      <EmptyState
        icon={Calendar}
        title="No trade history"
        description="You haven't made any trades yet. Your completed transactions will appear here."
        action={{
          label: 'Start Trading',
          onClick: () => window.location.href = '/trade',
        }}
      />
    );
  }

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-wrap gap-4 items-center">
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-gray-600" />
            <span className="text-sm text-gray-700">Filter:</span>
          </div>

          <div className="flex gap-2">
            {(['All', 'Buy', 'Sell'] as const).map((type) => (
              <button
                key={type}
                onClick={() => {
                  setFilterType(type);
                  setCurrentPage(1);
                }}
                className={`px-3 py-1 rounded-md text-sm transition-colors ${
                  filterType === type
                    ? 'bg-blue-100 text-blue-700'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                }`}
              >
                {type}
              </button>
            ))}
          </div>

          <div className="flex items-center gap-2 ml-auto">
            <Calendar className="w-4 h-4 text-gray-600" />
            <select
              value={dateRange}
              onChange={(e) => {
                setDateRange(e.target.value as any);
                setCurrentPage(1);
              }}
              className="px-3 py-1 border border-gray-300 rounded-md text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="All">All Time</option>
              <option value="Today">Today</option>
              <option value="Week">Past Week</option>
              <option value="Month">Past Month</option>
            </select>
          </div>
        </div>

        {filteredTrades.length > 0 && (
          <div className="mt-2 text-sm text-gray-600">
            Showing {startIndex + 1}-{Math.min(startIndex + itemsPerPage, filteredTrades.length)} of {filteredTrades.length} trades
          </div>
        )}
      </div>

      {/* Trade History Table */}
      {filteredTrades.length === 0 ? (
        <div className="bg-white rounded-lg border border-gray-200 p-8">
          <EmptyState
            icon={Calendar}
            title="No trades found"
            description="No trades match your current filters. Try adjusting your filter criteria."
          />
        </div>
      ) : (
        <>
          <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3 text-left text-gray-700">Type</th>
                    <th className="px-6 py-3 text-left text-gray-700">Company</th>
                    <th className="px-6 py-3 text-left text-gray-700">Share Type</th>
                    <th className="px-6 py-3 text-left text-gray-700">Quantity</th>
                    <th className="px-6 py-3 text-left text-gray-700">Price per Share</th>
                    <th className="px-6 py-3 text-left text-gray-700">Total Amount</th>
                    <th className="px-6 py-3 text-left text-gray-700">Broker</th>
                    <th className="px-6 py-3 text-left text-gray-700">Date</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {paginatedTrades.map((trade) => (
                    <tr key={trade.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          {trade.tradeType === 'Buy' ? (
                            <ArrowDownUp className="w-4 h-4 text-green-600" />
                          ) : (
                            <ArrowUpDown className="w-4 h-4 text-red-600" />
                          )}
                          <span className={`px-2 py-1 rounded text-sm ${
                            trade.tradeType === 'Buy'
                              ? 'bg-green-100 text-green-700'
                              : 'bg-red-100 text-red-700'
                          }`}>
                            {trade.tradeType}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <div>
                          <div className="text-gray-900">{trade.companyName}</div>
                          <div className="text-sm text-gray-500">{trade.ticker}</div>
                        </div>
                      </td>
                      <td className="px-6 py-4 text-gray-600">{trade.shareType}</td>
                      <td className="px-6 py-4 text-gray-900">{trade.quantity.toLocaleString()}</td>
                      <td className="px-6 py-4 text-gray-900">{formatCurrency(trade.pricePerShare)}</td>
                      <td className="px-6 py-4 text-gray-900">{formatCurrency(trade.totalAmount)}</td>
                      <td className="px-6 py-4 text-gray-600">{trade.brokerName}</td>
                      <td className="px-6 py-4">
                        <div className="text-sm text-gray-600">{formatDate(trade.tradeDate)}</div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between bg-white rounded-lg border border-gray-200 p-4">
              <button
                onClick={() => handlePageChange(currentPage - 1)}
                disabled={currentPage === 1}
                className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ChevronLeft className="w-4 h-4" />
                Previous
              </button>

              <div className="flex gap-2">
                {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                  <button
                    key={page}
                    onClick={() => handlePageChange(page)}
                    className={`px-3 py-1 rounded-md transition-colors ${
                      currentPage === page
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                    }`}
                  >
                    {page}
                  </button>
                ))}
              </div>

              <button
                onClick={() => handlePageChange(currentPage + 1)}
                disabled={currentPage === totalPages}
                className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
