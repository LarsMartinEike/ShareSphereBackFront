import React from 'react';
import { TrendingUp, Clock } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

interface ShareListProps {
  shares: Array<{
    id: number;
    companyId: number;
    shareType: string;
    quantity: number;
    pricePerShare: number;
    lastUpdated: string;
  }>;
  company: {
    id: number;
    name: string;
    ticker: string;
  };
}

export function ShareList({ shares, company }: ShareListProps) {
  const navigate = useNavigate();

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(price);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  };

  const handleTradeClick = (share: any) => {
    // Navigate to trade form with pre-selected share
    navigate('/trade', { state: { share, company } });
  };

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-gray-700">Share Type</th>
                <th className="px-6 py-3 text-left text-gray-700">Price per Share</th>
                <th className="px-6 py-3 text-left text-gray-700">Available Quantity</th>
                <th className="px-6 py-3 text-left text-gray-700">Last Updated</th>
                <th className="px-6 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {shares.map((share) => (
                <tr key={share.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      <div className="p-2 bg-green-50 rounded-lg">
                        <TrendingUp className="w-4 h-4 text-green-600" />
                      </div>
                      <span className="text-gray-900">{share.shareType}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-gray-900">{formatPrice(share.pricePerShare)}</span>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-gray-600">{share.quantity.toLocaleString()} shares</span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2 text-sm text-gray-600">
                      <Clock className="w-4 h-4" />
                      <span>{formatDate(share.lastUpdated)}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <button
                      onClick={() => handleTradeClick(share)}
                      className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                    >
                      Trade
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-900">
          <strong>Note:</strong> Share prices are updated in real-time. Available quantities are subject to change based on market activity.
        </p>
      </div>
    </div>
  );
}
