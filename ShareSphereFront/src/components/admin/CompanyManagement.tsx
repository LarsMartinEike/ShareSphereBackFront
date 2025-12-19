import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, X, Loader2, AlertCircle } from 'lucide-react';
import { toast } from 'sonner';
import { ConfirmDialog } from '../ConfirmDialog';

const mockExchanges = [
  { id: 1, name: 'New York Stock Exchange', code: 'NYSE' },
  { id: 2, name: 'NASDAQ', code: 'NASDAQ' },
  { id: 3, name: 'London Stock Exchange', code: 'LSE' },
];

const initialCompanies = [
  { id: 101, name: 'Apple Inc.', ticker: 'AAPL', sector: 'Technology', description: 'Consumer electronics and software', exchangeId: 1, exchangeName: 'NYSE' },
  { id: 102, name: 'Microsoft Corporation', ticker: 'MSFT', sector: 'Technology', description: 'Software and cloud services', exchangeId: 1, exchangeName: 'NYSE' },
  { id: 201, name: 'Amazon.com Inc.', ticker: 'AMZN', sector: 'E-commerce', description: 'Online retail and cloud computing', exchangeId: 2, exchangeName: 'NASDAQ' },
  { id: 202, name: 'Tesla Inc.', ticker: 'TSLA', sector: 'Automotive', description: 'Electric vehicles and clean energy', exchangeId: 2, exchangeName: 'NASDAQ' },
];

interface Company {
  id: number;
  name: string;
  ticker: string;
  sector: string;
  description: string;
  exchangeId: number;
  exchangeName: string;
}

export function CompanyManagement() {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [exchanges, setExchanges] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingCompany, setEditingCompany] = useState<Company | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<Company | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    ticker: '',
    sector: '',
    description: '',
    exchangeId: '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      await new Promise(resolve => setTimeout(resolve, 500));
      setCompanies(initialCompanies);
      setExchanges(mockExchanges);
      setLoading(false);
    };

    fetchData();
  }, []);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Company name is required';
    }

    if (!formData.ticker.trim()) {
      newErrors.ticker = 'Ticker symbol is required';
    } else if (companies.some(c => c.ticker.toUpperCase() === formData.ticker.toUpperCase() && c.id !== editingCompany?.id)) {
      newErrors.ticker = 'A company with this ticker symbol already exists';
    }

    if (!formData.sector.trim()) {
      newErrors.sector = 'Sector is required';
    }

    if (!formData.description.trim()) {
      newErrors.description = 'Description is required';
    }

    if (!formData.exchangeId) {
      newErrors.exchangeId = 'Please select an exchange';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleOpenForm = (company?: Company) => {
    if (company) {
      setEditingCompany(company);
      setFormData({
        name: company.name,
        ticker: company.ticker,
        sector: company.sector,
        description: company.description,
        exchangeId: company.exchangeId.toString(),
      });
    } else {
      setEditingCompany(null);
      setFormData({ name: '', ticker: '', sector: '', description: '', exchangeId: '' });
    }
    setErrors({});
    setShowForm(true);
  };

  const handleCloseForm = () => {
    setShowForm(false);
    setEditingCompany(null);
    setFormData({ name: '', ticker: '', sector: '', description: '', exchangeId: '' });
    setErrors({});
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      toast.error('Please fix the errors in the form');
      return;
    }

    await new Promise(resolve => setTimeout(resolve, 500));

    const exchangeId = parseInt(formData.exchangeId);
    const exchange = exchanges.find(e => e.id === exchangeId);

    const companyData = {
      ...formData,
      ticker: formData.ticker.toUpperCase(),
      exchangeId,
      exchangeName: exchange?.code || '',
    };

    if (editingCompany) {
      setCompanies(prev => prev.map(c => c.id === editingCompany.id ? { ...c, ...companyData } : c));
      toast.success('Company updated successfully');
    } else {
      const newCompany = { id: Date.now(), ...companyData };
      setCompanies(prev => [...prev, newCompany]);
      toast.success('Company created successfully');
    }

    handleCloseForm();
  };

  const handleDelete = async (company: Company) => {
    await new Promise(resolve => setTimeout(resolve, 500));
    
    setCompanies(prev => prev.filter(c => c.id !== company.id));
    setDeleteConfirm(null);
    toast.success('Company deleted successfully');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[300px]">
        <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-gray-900">Company Management</h2>
          <p className="text-sm text-gray-600">Manage companies listed on exchanges</p>
        </div>
        <button
          onClick={() => handleOpenForm()}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
        >
          <Plus className="w-4 h-4" />
          Add Company
        </button>
      </div>

      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-gray-700">Company Name</th>
                <th className="px-6 py-3 text-left text-gray-700">Ticker</th>
                <th className="px-6 py-3 text-left text-gray-700">Sector</th>
                <th className="px-6 py-3 text-left text-gray-700">Exchange</th>
                <th className="px-6 py-3 text-left text-gray-700">Description</th>
                <th className="px-6 py-3 text-right text-gray-700">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {companies.map((company) => (
                <tr key={company.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4 text-gray-900">{company.name}</td>
                  <td className="px-6 py-4">
                    <span className="inline-block px-2 py-1 bg-gray-100 rounded text-sm text-gray-700">
                      {company.ticker}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-gray-600">{company.sector}</td>
                  <td className="px-6 py-4">
                    <span className="inline-block px-2 py-1 bg-blue-100 text-blue-700 rounded text-sm">
                      {company.exchangeName}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-gray-600">
                    <span className="line-clamp-1">{company.description}</span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        onClick={() => handleOpenForm(company)}
                        className="p-2 text-blue-600 hover:bg-blue-50 rounded-md transition-colors"
                        aria-label={`Edit ${company.name}`}
                      >
                        <Edit className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => setDeleteConfirm(company)}
                        className="p-2 text-red-600 hover:bg-red-50 rounded-md transition-colors"
                        aria-label={`Delete ${company.name}`}
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {showForm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6 space-y-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between">
              <h3 className="text-gray-900">{editingCompany ? 'Edit Company' : 'Add New Company'}</h3>
              <button
                onClick={handleCloseForm}
                className="p-1 text-gray-400 hover:text-gray-600 rounded-md transition-colors"
                aria-label="Close"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label htmlFor="name" className="block text-sm text-gray-700 mb-1">
                  Company Name <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  id="name"
                  value={formData.name}
                  onChange={(e) => {
                    setFormData(prev => ({ ...prev, name: e.target.value }));
                    setErrors(prev => ({ ...prev, name: '' }));
                  }}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors.name ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="e.g., Apple Inc."
                />
                {errors.name && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.name}
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="ticker" className="block text-sm text-gray-700 mb-1">
                  Ticker Symbol <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  id="ticker"
                  value={formData.ticker}
                  onChange={(e) => {
                    setFormData(prev => ({ ...prev, ticker: e.target.value.toUpperCase() }));
                    setErrors(prev => ({ ...prev, ticker: '' }));
                  }}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors.ticker ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="e.g., AAPL"
                  maxLength={10}
                />
                {errors.ticker && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.ticker}
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="sector" className="block text-sm text-gray-700 mb-1">
                  Sector <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  id="sector"
                  value={formData.sector}
                  onChange={(e) => {
                    setFormData(prev => ({ ...prev, sector: e.target.value }));
                    setErrors(prev => ({ ...prev, sector: '' }));
                  }}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors.sector ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="e.g., Technology"
                />
                {errors.sector && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.sector}
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="exchangeId" className="block text-sm text-gray-700 mb-1">
                  Stock Exchange <span className="text-red-500">*</span>
                </label>
                <select
                  id="exchangeId"
                  value={formData.exchangeId}
                  onChange={(e) => {
                    setFormData(prev => ({ ...prev, exchangeId: e.target.value }));
                    setErrors(prev => ({ ...prev, exchangeId: '' }));
                  }}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors.exchangeId ? 'border-red-500' : 'border-gray-300'
                  }`}
                >
                  <option value="">Select an exchange...</option>
                  {exchanges.map(exchange => (
                    <option key={exchange.id} value={exchange.id}>
                      {exchange.name} ({exchange.code})
                    </option>
                  ))}
                </select>
                {errors.exchangeId && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.exchangeId}
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="description" className="block text-sm text-gray-700 mb-1">
                  Description <span className="text-red-500">*</span>
                </label>
                <textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => {
                    setFormData(prev => ({ ...prev, description: e.target.value }));
                    setErrors(prev => ({ ...prev, description: '' }));
                  }}
                  rows={3}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors.description ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="Brief description of the company"
                />
                {errors.description && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.description}
                  </p>
                )}
              </div>

              <div className="flex gap-3 pt-4">
                <button
                  type="submit"
                  className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                >
                  {editingCompany ? 'Update Company' : 'Create Company'}
                </button>
                <button
                  type="button"
                  onClick={handleCloseForm}
                  className="px-4 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteConfirm && (
        <ConfirmDialog
          title="Delete Company"
          message={`Are you sure you want to delete "${deleteConfirm.name}"? This will also affect all associated shares.`}
          confirmLabel="Delete"
          cancelLabel="Cancel"
          onConfirm={() => handleDelete(deleteConfirm)}
          onCancel={() => setDeleteConfirm(null)}
          variant="danger"
        />
      )}
    </div>
  );
}
