import React from 'react';
import { Building2, ChevronRight } from 'lucide-react';

interface CompanyListProps {
  companies: Array<{
    id: number;
    name: string;
    ticker: string;
    sector: string;
    description: string;
  }>;
  onSelect: (company: any) => void;
}

export function CompanyList({ companies, onSelect }: CompanyListProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="px-6 py-3 text-left text-gray-700">Company</th>
              <th className="px-6 py-3 text-left text-gray-700">Ticker</th>
              <th className="px-6 py-3 text-left text-gray-700">Sector</th>
              <th className="px-6 py-3 text-left text-gray-700">Description</th>
              <th className="px-6 py-3"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {companies.map((company) => (
              <tr
                key={company.id}
                className="hover:bg-gray-50 transition-colors cursor-pointer group"
                onClick={() => onSelect(company)}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    onSelect(company);
                  }
                }}
                aria-label={`View shares for ${company.name}`}
              >
                <td className="px-6 py-4">
                  <div className="flex items-center gap-3">
                    <div className="p-2 bg-blue-50 rounded-lg">
                      <Building2 className="w-4 h-4 text-blue-600" />
                    </div>
                    <span className="text-gray-900">{company.name}</span>
                  </div>
                </td>
                <td className="px-6 py-4">
                  <span className="inline-block px-2 py-1 bg-gray-100 rounded text-sm text-gray-700">
                    {company.ticker}
                  </span>
                </td>
                <td className="px-6 py-4 text-gray-600">{company.sector}</td>
                <td className="px-6 py-4 text-gray-600">
                  <span className="line-clamp-1">{company.description}</span>
                </td>
                <td className="px-6 py-4">
                  <ChevronRight className="w-5 h-5 text-gray-400 group-hover:text-blue-600 transition-colors" />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
