import React from 'react';
import { Building2, MapPin, ChevronRight } from 'lucide-react';

interface ExchangeCardProps {
  exchange: {
    exchangeId: number;
    name: string;
    currency: string;
    country: string;
    description: string;
  };
  onSelect: () => void;
}

export function ExchangeCard({ exchange, onSelect }: ExchangeCardProps) {
  return (
    <button
      onClick={onSelect}
      className="bg-white rounded-lg border border-gray-200 p-6 text-left hover:border-blue-300 hover:shadow-md transition-all focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 group"
      aria-label={`View companies on ${exchange.name}`}
    >
      <div className="flex items-start justify-between mb-4">
        <div className="p-2 bg-blue-50 rounded-lg">
          <Building2 className="w-6 h-6 text-blue-600" />
        </div>
        <ChevronRight className="w-5 h-5 text-gray-400 group-hover:text-blue-600 transition-colors" />
      </div>
      
      <div className="space-y-2">
        <h3 className="text-gray-900">{exchange.name}</h3>
        <div className="inline-block px-2 py-1 bg-gray-100 rounded text-sm text-gray-700">
          {exchange.currency}
        </div>
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <MapPin className="w-4 h-4" />
          <span>{exchange.country}</span>
        </div>
        <p className="text-sm text-gray-600 line-clamp-2">{exchange.description}</p>
      </div>
    </button>
  );
}
