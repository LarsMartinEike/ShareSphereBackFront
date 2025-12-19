import React, { useState } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Navigation } from './components/Navigation';
import { Dashboard } from './components/Dashboard';
import { Portfolio } from './components/Portfolio';
import { TradeForm } from './components/TradeForm';
import { AdminPanel } from './components/AdminPanel';
import { Toaster } from 'sonner';

export default function App() {
  // Toggle this between 'user' and 'admin' to see different views
  // In production, this would be determined by authentication
  const [userRole, setUserRole] = useState<'user' | 'admin'>('user');

  return (
    <Router>
      <div className="min-h-screen bg-gray-50">
        <Navigation userRole={userRole} />
        
        {/* Demo Role Switcher - Remove in production */}
        <div className="bg-yellow-50 border-b border-yellow-200 py-2">
          <div className="container mx-auto px-4 max-w-7xl flex items-center justify-between">
            <p className="text-sm text-yellow-900">
              <strong>Demo Mode:</strong> Switch between User and Admin views
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => setUserRole('user')}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  userRole === 'user'
                    ? 'bg-blue-600 text-white'
                    : 'bg-white text-gray-700 hover:bg-gray-100'
                }`}
              >
                User Mode
              </button>
              <button
                onClick={() => setUserRole('admin')}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  userRole === 'admin'
                    ? 'bg-blue-600 text-white'
                    : 'bg-white text-gray-700 hover:bg-gray-100'
                }`}
              >
                Admin Mode
              </button>
            </div>
          </div>
        </div>

        <main className="container mx-auto px-4 py-6 max-w-7xl">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/portfolio" element={<Portfolio />} />
            <Route path="/trade" element={<TradeForm />} />
            <Route path="/admin" element={userRole === 'admin' ? <AdminPanel /> : <Navigate to="/" />} />
          </Routes>
        </main>
        <Toaster position="top-right" richColors />
      </div>
    </Router>
  );
}