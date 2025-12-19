import React, { useState, useEffect } from 'react';
import { Building2, TrendingUp, ChevronRight, Loader2 } from 'lucide-react';
import { ExchangeCard } from './ExchangeCard';
import { CompanyList } from './CompanyList';
import { ShareList } from './ShareList';
import { EmptyState } from './EmptyState';

// Mock API data structures matching expected backend responses
// API Endpoint: GET /api/exchanges
const mockExchanges = [
  { id: 1, name: 'New York Stock Exchange', code: 'NYSE', location: 'New York, USA', description: 'The largest stock exchange in the world' },
  { id: 2, name: 'NASDAQ', code: 'NASDAQ', location: 'New York, USA', description: 'Technology-focused stock exchange' },
  { id: 3, name: 'London Stock Exchange', code: 'LSE', location: 'London, UK', description: 'One of the oldest stock exchanges globally' },
];

// API Endpoint: GET /api/exchanges/{exchangeId}/companies
const mockCompanies: Record<number, any[]> = {
  1: [
    { id: 101, name: 'Apple Inc.', ticker: 'AAPL', sector: 'Technology', description: 'Consumer electronics and software' },
    { id: 102, name: 'Microsoft Corporation', ticker: 'MSFT', sector: 'Technology', description: 'Software and cloud services' },
    { id: 103, name: 'Coca-Cola Company', ticker: 'KO', sector: 'Consumer Goods', description: 'Beverage manufacturer' },
  ],
  2: [
    { id: 201, name: 'Amazon.com Inc.', ticker: 'AMZN', sector: 'E-commerce', description: 'Online retail and cloud computing' },
    { id: 202, name: 'Tesla Inc.', ticker: 'TSLA', sector: 'Automotive', description: 'Electric vehicles and clean energy' },
  ],
  3: [
    { id: 301, name: 'BP plc', ticker: 'BP', sector: 'Energy', description: 'Oil and gas company' },
    { id: 302, name: 'HSBC Holdings', ticker: 'HSBC', sector: 'Financial Services', description: 'Banking and financial services' },
  ],
};

// API Endpoint: GET /api/companies/{companyId}/shares
const mockShares: Record<number, any[]> = {
  101: [
    { id: 1001, companyId: 101, shareType: 'Common Stock', quantity: 1500, pricePerShare: 175.50, lastUpdated: '2025-12-15T10:30:00Z' },
    { id: 1002, companyId: 101, shareType: 'Preferred Stock', quantity: 500, pricePerShare: 185.75, lastUpdated: '2025-12-15T10:30:00Z' },
  ],
  102: [
    { id: 1003, companyId: 102, shareType: 'Common Stock', quantity: 2000, pricePerShare: 380.25, lastUpdated: '2025-12-15T10:28:00Z' },
  ],
  103: [
    { id: 1004, companyId: 103, shareType: 'Common Stock', quantity: 3500, pricePerShare: 62.40, lastUpdated: '2025-12-15T10:25:00Z' },
  ],
  201: [
    { id: 1005, companyId: 201, shareType: 'Common Stock', quantity: 1200, pricePerShare: 145.30, lastUpdated: '2025-12-15T10:32:00Z' },
  ],
  202: [
    { id: 1006, companyId: 202, shareType: 'Common Stock', quantity: 800, pricePerShare: 242.85, lastUpdated: '2025-12-15T10:29:00Z' },
  ],
  301: [
    { id: 1007, companyId: 301, shareType: 'Common Stock', quantity: 5000, pricePerShare: 5.45, lastUpdated: '2025-12-15T10:20:00Z' },
  ],
  302: [
    { id: 1008, companyId: 302, shareType: 'Common Stock', quantity: 4200, pricePerShare: 7.82, lastUpdated: '2025-12-15T10:31:00Z' },
  ],
};

export function Dashboard() {
  const [loading, setLoading] = useState(true);
  const [exchanges, setExchanges] = useState<any[]>([]);
  const [selectedExchange, setSelectedExchange] = useState<any | null>(null);
  const [selectedCompany, setSelectedCompany] = useState<any | null>(null);
  const [companies, setCompanies] = useState<any[]>([]);
  const [shares, setShares] = useState<any[]>([]);
  const [loadingCompanies, setLoadingCompanies] = useState(false);
  const [loadingShares, setLoadingShares] = useState(false);

  // Simulated API call: GET /api/exchanges
  useEffect(() => {
    const fetchExchanges = async () => {
      setLoading(true);
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 800));
      setExchanges(mockExchanges);
      setLoading(false);
    };

    fetchExchanges();
  }, []);

  // Simulated API call: GET /api/exchanges/{exchangeId}/companies
  const handleExchangeSelect = async (exchange: any) => {
    setSelectedExchange(exchange);
    setSelectedCompany(null);
    setCompanies([]);
    setShares([]);
    setLoadingCompanies(true);

    // Simulate API delay
    await new Promise(resolve => setTimeout(resolve, 500));
    setCompanies(mockCompanies[exchange.id] || []);
    setLoadingCompanies(false);
  };

  // Simulated API call: GET /api/companies/{companyId}/shares
  const handleCompanySelect = async (company: any) => {
    setSelectedCompany(company);
    setShares([]);
    setLoadingShares(true);

    // Simulate API delay
    await new Promise(resolve => setTimeout(resolve, 500));
    setShares(mockShares[company.id] || []);
    setLoadingShares(false);
  };

  const handleBackToExchanges = () => {
    setSelectedExchange(null);
    setSelectedCompany(null);
    setCompanies([]);
    setShares([]);
  };

  const handleBackToCompanies = () => {
    setSelectedCompany(null);
    setShares([]);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
          <p className="text-gray-600">Loading exchanges...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb Navigation */}
      <div className="flex items-center gap-2 text-sm text-gray-600">
        <button
          onClick={handleBackToExchanges}
          className="hover:text-gray-900 transition-colors"
        >
          Exchanges
        </button>
        {selectedExchange && (
          <>
            <ChevronRight className="w-4 h-4" />
            <button
              onClick={handleBackToCompanies}
              className="hover:text-gray-900 transition-colors"
            >
              {selectedExchange.name}
            </button>
          </>
        )}
        {selectedCompany && (
          <>
            <ChevronRight className="w-4 h-4" />
            <span className="text-gray-900">{selectedCompany.name}</span>
          </>
        )}
      </div>

      {/* Exchanges View */}
      {!selectedExchange && (
        <div>
          <div className="mb-6">
            <h1 className="text-gray-900 mb-2">Stock Exchanges</h1>
            <p className="text-gray-600">Select an exchange to view available companies and shares</p>
          </div>

          {exchanges.length === 0 ? (
            <EmptyState
              icon={Building2}
              title="No exchanges available"
              description="There are currently no stock exchanges in the system."
            />
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {exchanges.map(exchange => (
                <ExchangeCard
                  key={exchange.id}
                  exchange={exchange}
                  onSelect={() => handleExchangeSelect(exchange)}
                />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Companies View */}
      {selectedExchange && !selectedCompany && (
        <div>
          <div className="mb-6">
            <h1 className="text-gray-900 mb-2">{selectedExchange.name}</h1>
            <p className="text-gray-600">{selectedExchange.description}</p>
          </div>

          {loadingCompanies ? (
            <div className="flex items-center justify-center min-h-[300px]">
              <div className="flex flex-col items-center gap-3">
                <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
                <p className="text-gray-600">Loading companies...</p>
              </div>
            </div>
          ) : companies.length === 0 ? (
            <EmptyState
              icon={Building2}
              title="No companies available"
              description={`There are currently no companies listed on ${selectedExchange.name}.`}
              action={{
                label: 'Back to Exchanges',
                onClick: handleBackToExchanges,
              }}
            />
          ) : (
            <CompanyList companies={companies} onSelect={handleCompanySelect} />
          )}
        </div>
      )}

      {/* Shares View */}
      {selectedCompany && (
        <div>
          <div className="mb-6">
            <h1 className="text-gray-900 mb-2">
              {selectedCompany.name} ({selectedCompany.ticker})
            </h1>
            <p className="text-gray-600">{selectedCompany.description}</p>
            <div className="flex gap-4 mt-2">
              <span className="text-sm text-gray-500">Sector: {selectedCompany.sector}</span>
            </div>
          </div>

          {loadingShares ? (
            <div className="flex items-center justify-center min-h-[300px]">
              <div className="flex flex-col items-center gap-3">
                <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
                <p className="text-gray-600">Loading shares...</p>
              </div>
            </div>
          ) : shares.length === 0 ? (
            <EmptyState
              icon={TrendingUp}
              title="No shares available"
              description={`There are currently no shares available for ${selectedCompany.name}.`}
              action={{
                label: 'Back to Companies',
                onClick: handleBackToCompanies,
              }}
            />
          ) : (
            <ShareList shares={shares} company={selectedCompany} />
          )}
        </div>
      )}
    </div>
  );
}
