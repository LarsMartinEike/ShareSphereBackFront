import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ArrowLeftRight, Loader2, AlertCircle, CheckCircle } from 'lucide-react';
import { toast } from 'sonner';

// Mock API data
// API Endpoint: GET /api/brokers
const mockBrokers = [
  { id: 1, name: 'E*TRADE', description: 'Leading online broker' },
  { id: 2, name: 'TD Ameritrade', description: 'Full-service brokerage' },
  { id: 3, name: 'Robinhood', description: 'Commission-free trading' },
  { id: 4, name: 'Charles Schwab', description: 'Comprehensive investment services' },
];

// API Endpoint: GET /api/shares (all available shares)
const mockAllShares = [
  { id: 1001, companyName: 'Apple Inc.', ticker: 'AAPL', shareType: 'Common Stock', availableQuantity: 1500, pricePerShare: 175.50 },
  { id: 1002, companyName: 'Apple Inc.', ticker: 'AAPL', shareType: 'Preferred Stock', availableQuantity: 500, pricePerShare: 185.75 },
  { id: 1003, companyName: 'Microsoft Corporation', ticker: 'MSFT', shareType: 'Common Stock', availableQuantity: 2000, pricePerShare: 380.25 },
  { id: 1004, companyName: 'Coca-Cola Company', ticker: 'KO', shareType: 'Common Stock', availableQuantity: 3500, pricePerShare: 62.40 },
  { id: 1005, companyName: 'Amazon.com Inc.', ticker: 'AMZN', shareType: 'Common Stock', availableQuantity: 1200, pricePerShare: 145.30 },
  { id: 1006, companyName: 'Tesla Inc.', ticker: 'TSLA', shareType: 'Common Stock', availableQuantity: 800, pricePerShare: 242.85 },
];

export function TradeForm() {
  const location = useLocation();
  const navigate = useNavigate();
  const preselectedData = location.state as { share?: any; company?: any } | null;

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [brokers, setBrokers] = useState<any[]>([]);
  const [shares, setShares] = useState<any[]>([]);
  
  const [formData, setFormData] = useState({
    brokerId: '',
    shareId: '',
    quantity: '',
    tradeType: 'Buy' as 'Buy' | 'Sell',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [showConfirmation, setShowConfirmation] = useState(false);

  // Simulated API call: GET /api/brokers and /api/shares
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      await new Promise(resolve => setTimeout(resolve, 600));
      
      setBrokers(mockBrokers);
      setShares(mockAllShares);
      
      // Pre-fill if coming from share list
      if (preselectedData?.share) {
        setFormData(prev => ({
          ...prev,
          shareId: preselectedData.share.id.toString(),
        }));
      }
      
      setLoading(false);
    };

    fetchData();
  }, [preselectedData]);

  const selectedShare = shares.find(s => s.id === parseInt(formData.shareId));

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.brokerId) {
      newErrors.brokerId = 'Please select a broker';
    }

    if (!formData.shareId) {
      newErrors.shareId = 'Please select a share';
    }

    if (!formData.quantity) {
      newErrors.quantity = 'Please enter quantity';
    } else {
      const qty = parseInt(formData.quantity);
      if (isNaN(qty) || qty <= 0) {
        newErrors.quantity = 'Quantity must be a positive number';
      } else if (!Number.isInteger(qty)) {
        newErrors.quantity = 'Quantity must be a whole number';
      } else if (selectedShare && qty > selectedShare.availableQuantity) {
        newErrors.quantity = `Only ${selectedShare.availableQuantity} shares available`;
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const calculateTotal = () => {
    if (!selectedShare || !formData.quantity) return 0;
    const qty = parseInt(formData.quantity);
    if (isNaN(qty)) return 0;
    return qty * selectedShare.pricePerShare;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      toast.error('Please fix the errors in the form');
      return;
    }

    setSubmitting(true);

    // Simulated API call: POST /api/trades
    const tradeData = {
      brokerId: parseInt(formData.brokerId),
      shareId: parseInt(formData.shareId),
      quantity: parseInt(formData.quantity),
      tradeType: formData.tradeType,
      pricePerShare: selectedShare!.pricePerShare,
      totalAmount: calculateTotal(),
    };

    try {
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // Simulate success response
      setShowConfirmation(true);
      toast.success(`${formData.tradeType} order executed successfully!`);
      
      // Reset form after delay
      setTimeout(() => {
        setShowConfirmation(false);
        setFormData({
          brokerId: '',
          shareId: '',
          quantity: '',
          tradeType: 'Buy',
        });
        setErrors({});
      }, 3000);
      
    } catch (error) {
      toast.error('Failed to execute trade. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

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
          <p className="text-gray-600">Loading trading options...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div>
        <h1 className="text-gray-900 mb-2">Execute Trade</h1>
        <p className="text-gray-600">Buy or sell shares through your preferred broker</p>
      </div>

      {showConfirmation && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 flex items-start gap-3">
          <CheckCircle className="w-5 h-5 text-green-600 mt-0.5" />
          <div>
            <h3 className="text-green-900">Trade Executed Successfully</h3>
            <p className="text-sm text-green-700 mt-1">
              Your {formData.tradeType.toLowerCase()} order for {formData.quantity} shares has been processed.
              Your portfolio has been updated.
            </p>
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit} className="bg-white rounded-lg border border-gray-200 p-6 space-y-6">
        {/* Trade Type Toggle */}
        <div>
          <label className="block text-sm text-gray-700 mb-2">
            Trade Type <span className="text-red-500">*</span>
          </label>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setFormData(prev => ({ ...prev, tradeType: 'Buy' }))}
              className={`flex-1 py-2 px-4 rounded-md transition-colors ${
                formData.tradeType === 'Buy'
                  ? 'bg-green-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              Buy
            </button>
            <button
              type="button"
              onClick={() => setFormData(prev => ({ ...prev, tradeType: 'Sell' }))}
              className={`flex-1 py-2 px-4 rounded-md transition-colors ${
                formData.tradeType === 'Sell'
                  ? 'bg-red-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              Sell
            </button>
          </div>
        </div>

        {/* Broker Selection */}
        <div>
          <label htmlFor="broker" className="block text-sm text-gray-700 mb-2">
            Select Broker <span className="text-red-500">*</span>
          </label>
          <select
            id="broker"
            value={formData.brokerId}
            onChange={(e) => {
              setFormData(prev => ({ ...prev, brokerId: e.target.value }));
              setErrors(prev => ({ ...prev, brokerId: '' }));
            }}
            className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
              errors.brokerId ? 'border-red-500' : 'border-gray-300'
            }`}
            aria-invalid={!!errors.brokerId}
            aria-describedby={errors.brokerId ? 'broker-error' : undefined}
          >
            <option value="">Choose a broker...</option>
            {brokers.map(broker => (
              <option key={broker.id} value={broker.id}>
                {broker.name} - {broker.description}
              </option>
            ))}
          </select>
          {errors.brokerId && (
            <p id="broker-error" className="mt-1 text-sm text-red-600 flex items-center gap-1">
              <AlertCircle className="w-4 h-4" />
              {errors.brokerId}
            </p>
          )}
        </div>

        {/* Share Selection */}
        <div>
          <label htmlFor="share" className="block text-sm text-gray-700 mb-2">
            Select Share <span className="text-red-500">*</span>
          </label>
          <select
            id="share"
            value={formData.shareId}
            onChange={(e) => {
              setFormData(prev => ({ ...prev, shareId: e.target.value }));
              setErrors(prev => ({ ...prev, shareId: '' }));
            }}
            className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
              errors.shareId ? 'border-red-500' : 'border-gray-300'
            }`}
            aria-invalid={!!errors.shareId}
            aria-describedby={errors.shareId ? 'share-error' : undefined}
          >
            <option value="">Choose a share...</option>
            {shares.map(share => (
              <option key={share.id} value={share.id}>
                {share.companyName} ({share.ticker}) - {share.shareType} - {formatCurrency(share.pricePerShare)}
              </option>
            ))}
          </select>
          {errors.shareId && (
            <p id="share-error" className="mt-1 text-sm text-red-600 flex items-center gap-1">
              <AlertCircle className="w-4 h-4" />
              {errors.shareId}
            </p>
          )}
        </div>

        {/* Share Details */}
        {selectedShare && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 space-y-2">
            <h3 className="text-sm text-blue-900">Share Details</h3>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-blue-700">Price per Share:</span>
                <div className="text-blue-900">{formatCurrency(selectedShare.pricePerShare)}</div>
              </div>
              <div>
                <span className="text-blue-700">Available Quantity:</span>
                <div className="text-blue-900">{selectedShare.availableQuantity.toLocaleString()} shares</div>
              </div>
            </div>
          </div>
        )}

        {/* Quantity Input */}
        <div>
          <label htmlFor="quantity" className="block text-sm text-gray-700 mb-2">
            Quantity <span className="text-red-500">*</span>
          </label>
          <input
            type="number"
            id="quantity"
            min="1"
            step="1"
            value={formData.quantity}
            onChange={(e) => {
              setFormData(prev => ({ ...prev, quantity: e.target.value }));
              setErrors(prev => ({ ...prev, quantity: '' }));
            }}
            placeholder="Enter number of shares"
            className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
              errors.quantity ? 'border-red-500' : 'border-gray-300'
            }`}
            aria-invalid={!!errors.quantity}
            aria-describedby={errors.quantity ? 'quantity-error' : undefined}
          />
          {errors.quantity && (
            <p id="quantity-error" className="mt-1 text-sm text-red-600 flex items-center gap-1">
              <AlertCircle className="w-4 h-4" />
              {errors.quantity}
            </p>
          )}
        </div>

        {/* Total Amount */}
        {selectedShare && formData.quantity && !errors.quantity && (
          <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
            <div className="flex items-center justify-between">
              <span className="text-gray-700">Total Amount:</span>
              <span className="text-gray-900">{formatCurrency(calculateTotal())}</span>
            </div>
          </div>
        )}

        {/* Submit Button */}
        <div className="flex gap-4">
          <button
            type="submit"
            disabled={submitting}
            className={`flex-1 flex items-center justify-center gap-2 px-6 py-3 rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 ${
              formData.tradeType === 'Buy'
                ? 'bg-green-600 hover:bg-green-700 focus:ring-green-500'
                : 'bg-red-600 hover:bg-red-700 focus:ring-red-500'
            } text-white disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {submitting ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Processing...
              </>
            ) : (
              <>
                <ArrowLeftRight className="w-5 h-5" />
                Execute {formData.tradeType}
              </>
            )}
          </button>
          <button
            type="button"
            onClick={() => navigate('/portfolio')}
            className="px-6 py-3 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
          >
            View Portfolio
          </button>
        </div>
      </form>

      {/* Information Notice */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <h3 className="text-sm text-yellow-900 mb-1">Important Information</h3>
        <ul className="text-sm text-yellow-700 space-y-1 list-disc list-inside">
          <li>All trades are executed at current market prices</li>
          <li>Ensure you have sufficient funds in your brokerage account</li>
          <li>Completed trades will appear in your portfolio immediately</li>
          <li>Trade confirmations will be sent to your registered email</li>
        </ul>
      </div>
    </div>
  );
}
