import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, X, Loader2, AlertCircle } from 'lucide-react';
import { toast } from 'sonner';
import { ConfirmDialog } from '../ConfirmDialog';

// Mock API data
// API Endpoint: GET /api/admin/brokers
const initialBrokers = [
  { id: 1, name: 'E*TRADE', description: 'Leading online broker', contactEmail: 'support@etrade.com' },
  { id: 2, name: 'TD Ameritrade', description: 'Full-service brokerage', contactEmail: 'help@tdameritrade.com' },
  { id: 3, name: 'Robinhood', description: 'Commission-free trading', contactEmail: 'support@robinhood.com' },
];

interface Broker {
  id: number;
  name: string;
  description: string;
  contactEmail: string;
}

export function BrokerManagement() {
  const [brokers, setBrokers] = useState<Broker[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingBroker, setEditingBroker] = useState<Broker | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<Broker | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    description: '',
    contactEmail: '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Simulated API call: GET /api/admin/brokers
  useEffect(() => {
    const fetchBrokers = async () => {
      setLoading(true);
      await new Promise(resolve => setTimeout(resolve, 500));
      setBrokers(initialBrokers);
      setLoading(false);
    };

    fetchBrokers();
  }, []);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Broker name is required';
    } else if (brokers.some(b => b.name.toLowerCase() === formData.name.toLowerCase() && b.id !== editingBroker?.id)) {
      newErrors.name = 'A broker with this name already exists';
    }

    if (!formData.description.trim()) {
      newErrors.description = 'Description is required';
    }

    if (!formData.contactEmail.trim()) {
      newErrors.contactEmail = 'Contact email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.contactEmail)) {
      newErrors.contactEmail = 'Please enter a valid email address';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleOpenForm = (broker?: Broker) => {
    if (broker) {
      setEditingBroker(broker);
      setFormData({
        name: broker.name,
        description: broker.description,
        contactEmail: broker.contactEmail,
      });
    } else {
      setEditingBroker(null);
      setFormData({ name: '', description: '', contactEmail: '' });
    }
    setErrors({});
    setShowForm(true);
  };

  const handleCloseForm = () => {
    setShowForm(false);
    setEditingBroker(null);
    setFormData({ name: '', description: '', contactEmail: '' });
    setErrors({});
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      toast.error('Please fix the errors in the form');
      return;
    }

    // Simulated API call: POST /api/admin/brokers or PUT /api/admin/brokers/{id}
    await new Promise(resolve => setTimeout(resolve, 500));

    if (editingBroker) {
      setBrokers(prev => prev.map(b => b.id === editingBroker.id ? { ...b, ...formData } : b));
      toast.success('Broker updated successfully');
    } else {
      const newBroker = { id: Date.now(), ...formData };
      setBrokers(prev => [...prev, newBroker]);
      toast.success('Broker created successfully');
    }

    handleCloseForm();
  };

  const handleDelete = async (broker: Broker) => {
    // Simulated API call: DELETE /api/admin/brokers/{id}
    await new Promise(resolve => setTimeout(resolve, 500));
    
    setBrokers(prev => prev.filter(b => b.id !== broker.id));
    setDeleteConfirm(null);
    toast.success('Broker deleted successfully');
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
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-gray-900">Broker Management</h2>
          <p className="text-sm text-gray-600">Manage registered brokers in the system</p>
        </div>
        <button
          onClick={() => handleOpenForm()}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
        >
          <Plus className="w-4 h-4" />
          Add Broker
        </button>
      </div>

      {/* Brokers Table */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-gray-700">Name</th>
                <th className="px-6 py-3 text-left text-gray-700">Description</th>
                <th className="px-6 py-3 text-left text-gray-700">Contact Email</th>
                <th className="px-6 py-3 text-right text-gray-700">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {brokers.map((broker) => (
                <tr key={broker.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4 text-gray-900">{broker.name}</td>
                  <td className="px-6 py-4 text-gray-600">{broker.description}</td>
                  <td className="px-6 py-4 text-gray-600">{broker.contactEmail}</td>
                  <td className="px-6 py-4">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        onClick={() => handleOpenForm(broker)}
                        className="p-2 text-blue-600 hover:bg-blue-50 rounded-md transition-colors"
                        aria-label={`Edit ${broker.name}`}
                      >
                        <Edit className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => setDeleteConfirm(broker)}
                        className="p-2 text-red-600 hover:bg-red-50 rounded-md transition-colors"
                        aria-label={`Delete ${broker.name}`}
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

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-gray-900">{editingBroker ? 'Edit Broker' : 'Add New Broker'}</h3>
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
                  Broker Name <span className="text-red-500">*</span>
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
                  placeholder="e.g., E*TRADE"
                />
                {errors.name && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.name}
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
                  placeholder="Brief description of the broker"
                />
                {errors.description && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.description}
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="contactEmail" className="block text-sm text-gray-700 mb-1">
                  Contact Email <span className="text-red-500">*</span>
                </label>
                <input
                  type="email"
                  id="contactEmail"
                  value={formData.contactEmail}
                  onChange={(e) => {
                    setFormData(prev => ({ ...prev, contactEmail: e.target.value }));
                    setErrors(prev => ({ ...prev, contactEmail: '' }));
                  }}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors.contactEmail ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="support@broker.com"
                />
                {errors.contactEmail && (
                  <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
                    <AlertCircle className="w-4 h-4" />
                    {errors.contactEmail}
                  </p>
                )}
              </div>

              <div className="flex gap-3 pt-4">
                <button
                  type="submit"
                  className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                >
                  {editingBroker ? 'Update Broker' : 'Create Broker'}
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

      {/* Delete Confirmation Dialog */}
      {deleteConfirm && (
        <ConfirmDialog
          title="Delete Broker"
          message={`Are you sure you want to delete "${deleteConfirm.name}"? This action cannot be undone.`}
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
