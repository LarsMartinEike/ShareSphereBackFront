import React, { useState, useEffect } from 'react';
import { Building2, TrendingUp, ChevronRight, Loader2 } from 'lucide-react';
import { ExchangeCard } from './ExchangeCard';
import { CompanyList } from './CompanyList';
import { ShareList } from './ShareList';
import { EmptyState } from './EmptyState';
import { apiFetch } from '../api/client';


// Mock API data structures matching expected backend responses
// API Endpoint: GET /api/exchanges

// API Endpoint: GET /api/exchanges/{exchangeId}/companies

// API Endpoint: GET /api/companies/{companyId}/shares


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
    try {
      setLoading(true);
      const data = await apiFetch<any[]>('/api/stockexchanges');
      setExchanges(data);
    } catch (err) {
      console.error(err);
      setExchanges([]);
    } finally {
      setLoading(false);
    }
  };

  fetchExchanges();
}, []);

  // Simulated API call: GET /api/exchanges/{exchangeId}/companies
const handleExchangeSelect = async (exchange: any) => {
  try {
    setSelectedExchange(exchange);
    setSelectedCompany(null);
    setCompanies([]);
    setShares([]);
    setLoadingCompanies(true);

    const exchangeData = await apiFetch<any>(
      `/api/stockexchanges/${exchange.exchangeId}`
    );

    setCompanies(exchangeData.companies);
  } catch (error) {
    console.error(error);
    setCompanies([]);
  } finally {
    setLoadingCompanies(false);
  }
};

  // Simulated API call: GET /api/companies/{companyId}/shares
const handleCompanySelect = async (company: any) => {
  try {
    setSelectedCompany(company);
    setShares([]);
    setLoadingShares(true);

    const shares = await apiFetch<any>(
      `/api/shares/company/${company.companyId}`
    );

    setShares(shares);
  } catch (error) {
    console.error(error);
    setShares([]);
  } finally {
    setLoadingShares(false);
  }
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
