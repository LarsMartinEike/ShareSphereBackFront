import React, { useState } from 'react';
import { Settings, Building2, Users, Briefcase } from 'lucide-react';
import { BrokerManagement } from './admin/BrokerManagement';
import { ExchangeManagement } from './admin/ExchangeManagement';
import { CompanyManagement } from './admin/CompanyManagement';

type AdminTab = 'brokers' | 'exchanges' | 'companies';

export function AdminPanel() {
  const [activeTab, setActiveTab] = useState<AdminTab>('brokers');

  const tabs = [
    { id: 'brokers' as AdminTab, label: 'Brokers', icon: Users },
    { id: 'exchanges' as AdminTab, label: 'Exchanges', icon: Building2 },
    { id: 'companies' as AdminTab, label: 'Companies', icon: Briefcase },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Settings className="w-8 h-8 text-gray-900" />
        <div>
          <h1 className="text-gray-900">Admin Panel</h1>
          <p className="text-gray-600">Manage brokers, exchanges, and companies</p>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="bg-white rounded-lg border border-gray-200 p-1 inline-flex gap-1">
        {tabs.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            onClick={() => setActiveTab(id)}
            className={`flex items-center gap-2 px-4 py-2 rounded-md transition-colors ${
              activeTab === id
                ? 'bg-blue-600 text-white'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            <Icon className="w-4 h-4" />
            <span>{label}</span>
          </button>
        ))}
      </div>

      {/* Tab Content */}
      <div>
        {activeTab === 'brokers' && <BrokerManagement />}
        {activeTab === 'exchanges' && <ExchangeManagement />}
        {activeTab === 'companies' && <CompanyManagement />}
      </div>
    </div>
  );
}
