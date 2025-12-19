# Frontend-Backend Integration Guide
## React + .NET WebAPI Integration for ShareSphere

This guide shows how the React frontend components connect to the .NET WebAPI backend.

---

## Table of Contents
1. [API Configuration](#api-configuration)
2. [Component-to-Endpoint Mapping](#component-to-endpoint-mapping)
3. [Data Flow Examples](#data-flow-examples)
4. [Error Handling](#error-handling)
5. [State Management](#state-management)

---

## API Configuration

### Creating an API Service Layer

Create `/src/services/api.ts`:

```typescript
import axios from 'axios';

// Base URL - update this for production
const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:7001/api';

// Create axios instance with default config
export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Request interceptor for adding auth tokens
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized - redirect to login
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

---

## Component-to-Endpoint Mapping

### Dashboard Component

**File**: `/components/Dashboard.tsx`

**API Calls**:

```typescript
// 1. Fetch all exchanges (on component mount)
const fetchExchanges = async () => {
  try {
    const response = await apiClient.get('/exchanges');
    setExchanges(response.data);
  } catch (error) {
    console.error('Failed to fetch exchanges:', error);
    toast.error('Failed to load exchanges');
  }
};

// 2. Fetch companies for selected exchange
const handleExchangeSelect = async (exchange: any) => {
  try {
    const response = await apiClient.get(`/exchanges/${exchange.id}/companies`);
    setCompanies(response.data);
  } catch (error) {
    console.error('Failed to fetch companies:', error);
    toast.error('Failed to load companies');
  }
};

// 3. Fetch shares for selected company
const handleCompanySelect = async (company: any) => {
  try {
    const response = await apiClient.get(`/companies/${company.id}/shares`);
    setShares(response.data);
  } catch (error) {
    console.error('Failed to fetch shares:', error);
    toast.error('Failed to load shares');
  }
};
```

**Backend Endpoints Used**:
- `GET /api/exchanges`
- `GET /api/exchanges/{id}/companies`
- `GET /api/companies/{id}/shares`

---

### Portfolio Component

**File**: `/components/Portfolio.tsx`

**API Calls**:

```typescript
// Assume current user ID is 1 (replace with actual auth user ID)
const CURRENT_USER_ID = 1;

// 1. Fetch portfolio summary
const fetchPortfolioSummary = async () => {
  try {
    const response = await apiClient.get(`/portfolio/summary/${CURRENT_USER_ID}`);
    setSummary(response.data);
  } catch (error) {
    console.error('Failed to fetch portfolio summary:', error);
    toast.error('Failed to load portfolio summary');
  }
};

// 2. Fetch holdings
const fetchHoldings = async () => {
  try {
    const response = await apiClient.get(`/portfolio/holdings/${CURRENT_USER_ID}`);
    setHoldings(response.data);
  } catch (error) {
    console.error('Failed to fetch holdings:', error);
    toast.error('Failed to load holdings');
  }
};

// 3. Fetch trade history with filters
const fetchTrades = async (filters: TradeFilters) => {
  try {
    const params = new URLSearchParams();
    if (filters.tradeType !== 'All') params.append('tradeType', filters.tradeType);
    if (filters.startDate) params.append('startDate', filters.startDate);
    if (filters.endDate) params.append('endDate', filters.endDate);
    params.append('page', filters.page.toString());
    params.append('pageSize', filters.pageSize.toString());

    const response = await apiClient.get(
      `/trades/shareholder/${CURRENT_USER_ID}?${params.toString()}`
    );
    setTrades(response.data.trades);
    setTotalCount(response.data.totalCount);
  } catch (error) {
    console.error('Failed to fetch trades:', error);
    toast.error('Failed to load trade history');
  }
};
```

**Backend Endpoints Used**:
- `GET /api/portfolio/summary/{shareholderId}`
- `GET /api/portfolio/holdings/{shareholderId}`
- `GET /api/trades/shareholder/{shareholderId}`

---

### Trade Form Component

**File**: `/components/TradeForm.tsx`

**API Calls**:

```typescript
// 1. Fetch brokers for dropdown
const fetchBrokers = async () => {
  try {
    const response = await apiClient.get('/brokers');
    setBrokers(response.data);
  } catch (error) {
    console.error('Failed to fetch brokers:', error);
    toast.error('Failed to load brokers');
  }
};

// 2. Fetch all available shares
const fetchShares = async () => {
  try {
    const response = await apiClient.get('/shares');
    setShares(response.data);
  } catch (error) {
    console.error('Failed to fetch shares:', error);
    toast.error('Failed to load shares');
  }
};

// 3. Execute trade
const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  
  try {
    const tradeData = {
      shareholderId: CURRENT_USER_ID,
      shareId: parseInt(formData.shareId),
      brokerId: parseInt(formData.brokerId),
      tradeType: formData.tradeType,
      quantity: parseInt(formData.quantity),
    };

    const response = await apiClient.post('/trades', tradeData);
    
    toast.success(`${formData.tradeType} order executed successfully!`);
    setShowConfirmation(true);
    
    // Optionally refresh portfolio
    // fetchPortfolio();
    
  } catch (error: any) {
    const errorMessage = error.response?.data?.message || 'Failed to execute trade';
    toast.error(errorMessage);
    
    // Handle specific validation errors
    if (error.response?.data?.errors) {
      setErrors(error.response.data.errors);
    }
  }
};
```

**Backend Endpoints Used**:
- `GET /api/brokers`
- `GET /api/shares`
- `POST /api/trades`

---

### Admin Panel Components

**File**: `/components/admin/BrokerManagement.tsx`

**API Calls**:

```typescript
// 1. Fetch all brokers
const fetchBrokers = async () => {
  try {
    const response = await apiClient.get('/admin/brokers');
    setBrokers(response.data);
  } catch (error) {
    console.error('Failed to fetch brokers:', error);
    toast.error('Failed to load brokers');
  }
};

// 2. Create broker
const createBroker = async (data: CreateBrokerDto) => {
  try {
    const response = await apiClient.post('/admin/brokers', data);
    setBrokers([...brokers, response.data]);
    toast.success('Broker created successfully');
  } catch (error: any) {
    const errorMessage = error.response?.data?.message || 'Failed to create broker';
    toast.error(errorMessage);
  }
};

// 3. Update broker
const updateBroker = async (id: number, data: UpdateBrokerDto) => {
  try {
    const response = await apiClient.put(`/admin/brokers/${id}`, data);
    setBrokers(brokers.map(b => b.id === id ? response.data : b));
    toast.success('Broker updated successfully');
  } catch (error: any) {
    const errorMessage = error.response?.data?.message || 'Failed to update broker';
    toast.error(errorMessage);
  }
};

// 4. Delete broker
const deleteBroker = async (id: number) => {
  try {
    await apiClient.delete(`/admin/brokers/${id}`);
    setBrokers(brokers.filter(b => b.id !== id));
    toast.success('Broker deleted successfully');
  } catch (error: any) {
    const errorMessage = error.response?.data?.message || 'Failed to delete broker';
    toast.error(errorMessage);
  }
};
```

**Similar patterns apply for**:
- `/components/admin/ExchangeManagement.tsx` → `/admin/exchanges` endpoints
- `/components/admin/CompanyManagement.tsx` → `/admin/companies` endpoints

---

## Data Flow Examples

### Example 1: User Executes a Trade

```
┌─────────────┐
│ TradeForm   │
│ Component   │
└──────┬──────┘
       │ 1. User fills form and clicks "Execute Buy"
       ▼
┌─────────────────────────────────────────────┐
│ Frontend Validation                         │
│ - Check all fields filled                   │
│ - Validate quantity > 0                     │
│ - Validate share selected                   │
└──────┬──────────────────────────────────────┘
       │ 2. POST /api/trades
       ▼
┌─────────────────────────────────────────────┐
│ .NET TradesController                       │
│ - Validate share exists                     │
│ - Check available quantity                  │
│ - Calculate total amount                    │
│ - Create trade record                       │
│ - Update share quantity                     │
│ - Save to database                          │
└──────┬──────────────────────────────────────┘
       │ 3. Return 201 Created + TradeResponseDto
       ▼
┌─────────────────────────────────────────────┐
│ Frontend Response Handling                  │
│ - Show success message                      │
│ - Display confirmation                      │
│ - Optionally refresh portfolio              │
└─────────────────────────────────────────────┘
```

### Example 2: Loading Dashboard Data

```
┌─────────────┐
│ Dashboard   │
│ Component   │
└──────┬──────┘
       │ 1. useEffect on mount
       ▼
┌─────────────────────────────────────────────┐
│ GET /api/exchanges                          │
└──────┬──────────────────────────────────────┘
       │ 2. Returns array of ExchangeDto
       ▼
┌─────────────────────────────────────────────┐
│ Display Exchange Cards                      │
└──────┬──────────────────────────────────────┘
       │ 3. User clicks NYSE
       ▼
┌─────────────────────────────────────────────┐
│ GET /api/exchanges/1/companies              │
└──────┬──────────────────────────────────────┘
       │ 4. Returns array of CompanyDto
       ▼
┌─────────────────────────────────────────────┐
│ Display Company List                        │
└──────┬──────────────────────────────────────┘
       │ 5. User clicks Apple
       ▼
┌─────────────────────────────────────────────┐
│ GET /api/companies/101/shares               │
└──────┬──────────────────────────────────────┘
       │ 6. Returns array of ShareDto
       ▼
┌─────────────────────────────────────────────┐
│ Display Share List with Trade buttons      │
└─────────────────────────────────────────────┘
```

---

## Error Handling

### Backend Error Response Format

```json
{
  "message": "Only 1500 shares available",
  "errors": {
    "quantity": ["Requested quantity exceeds available shares"]
  }
}
```

### Frontend Error Handling Pattern

```typescript
try {
  const response = await apiClient.post('/trades', tradeData);
  // Success handling
} catch (error: any) {
  // 1. Check for network errors
  if (!error.response) {
    toast.error('Network error. Please check your connection.');
    return;
  }

  // 2. Handle validation errors (400)
  if (error.response.status === 400) {
    const message = error.response.data?.message || 'Validation failed';
    toast.error(message);
    
    // Set field-specific errors
    if (error.response.data?.errors) {
      setFieldErrors(error.response.data.errors);
    }
    return;
  }

  // 3. Handle not found (404)
  if (error.response.status === 404) {
    toast.error('Resource not found');
    return;
  }

  // 4. Handle server errors (500)
  if (error.response.status === 500) {
    toast.error('Server error. Please try again later.');
    return;
  }

  // 5. Default error
  toast.error('An unexpected error occurred');
}
```

---

## State Management

### Option 1: React Context (Recommended for this app)

Create `/src/context/AuthContext.tsx`:

```typescript
import React, { createContext, useContext, useState, useEffect } from 'react';

interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: 'user' | 'admin';
}

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    // Load user from localStorage on mount
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }
  }, []);

  const login = async (email: string, password: string) => {
    // Call your login API endpoint
    const response = await apiClient.post('/auth/login', { email, password });
    const userData = response.data;
    
    setUser(userData.user);
    localStorage.setItem('authToken', userData.token);
    localStorage.setItem('user', JSON.stringify(userData.user));
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');
  };

  return (
    <AuthContext.Provider 
      value={{ 
        user, 
        login, 
        logout, 
        isAuthenticated: !!user 
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
```

Update `App.tsx`:

```typescript
import { AuthProvider, useAuth } from './context/AuthContext';

function App() {
  return (
    <AuthProvider>
      <Router>
        {/* ... rest of app */}
      </Router>
    </AuthProvider>
  );
}
```

Use in components:

```typescript
function Portfolio() {
  const { user } = useAuth();
  
  useEffect(() => {
    if (user) {
      fetchPortfolioData(user.id);
    }
  }, [user]);
}
```

---

## Environment Variables

Create `.env.local`:

```env
REACT_APP_API_URL=https://localhost:7001/api
REACT_APP_ENV=development
```

Create `.env.production`:

```env
REACT_APP_API_URL=https://api.sharesphere.com/api
REACT_APP_ENV=production
```

---

## TypeScript Type Definitions

Create `/src/types/api.types.ts`:

```typescript
// Match the DTOs from backend
export interface ExchangeDto {
  id: number;
  name: string;
  code: string;
  location: string;
  description: string;
}

export interface CompanyDto {
  id: number;
  name: string;
  ticker: string;
  sector: string;
  description: string;
  exchangeId: number;
  exchangeName: string;
}

export interface ShareDto {
  id: number;
  companyId: number;
  companyName: string;
  ticker: string;
  shareType: string;
  quantity: number;
  pricePerShare: number;
  lastUpdated: string;
}

export interface BrokerDto {
  id: number;
  name: string;
  description: string;
  contactEmail: string;
}

export interface TradeDto {
  id: number;
  tradeType: 'Buy' | 'Sell';
  companyName: string;
  ticker: string;
  shareType: string;
  quantity: number;
  pricePerShare: number;
  totalAmount: number;
  brokerName: string;
  tradeDate: string;
  status: string;
}

export interface CreateTradeDto {
  shareholderId: number;
  shareId: number;
  brokerId: number;
  tradeType: 'Buy' | 'Sell';
  quantity: number;
}

export interface PortfolioSummaryDto {
  totalValue: number;
  totalShares: number;
  changeAmount: number;
  changePercentage: number;
}

export interface HoldingDto {
  id: number;
  shareId: number;
  companyName: string;
  ticker: string;
  shareType: string;
  quantity: number;
  purchasePrice: number;
  currentPrice: number;
  totalValue: number;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
}
```

---

## Complete API Service Example

Create `/src/services/sharesphere.service.ts`:

```typescript
import { apiClient } from './api';
import {
  ExchangeDto,
  CompanyDto,
  ShareDto,
  BrokerDto,
  TradeDto,
  CreateTradeDto,
  PortfolioSummaryDto,
  HoldingDto,
} from '../types/api.types';

export const ShareSphereService = {
  // Exchanges
  getExchanges: () => 
    apiClient.get<ExchangeDto[]>('/exchanges'),
  
  getExchange: (id: number) => 
    apiClient.get<ExchangeDto>(`/exchanges/${id}`),
  
  getExchangeCompanies: (id: number) => 
    apiClient.get<CompanyDto[]>(`/exchanges/${id}/companies`),

  // Companies
  getCompanies: () => 
    apiClient.get<CompanyDto[]>('/companies'),
  
  getCompany: (id: number) => 
    apiClient.get<CompanyDto>(`/companies/${id}`),
  
  getCompanyShares: (id: number) => 
    apiClient.get<ShareDto[]>(`/companies/${id}/shares`),

  // Shares
  getShares: () => 
    apiClient.get<ShareDto[]>('/shares'),
  
  getShare: (id: number) => 
    apiClient.get<ShareDto>(`/shares/${id}`),

  // Brokers
  getBrokers: () => 
    apiClient.get<BrokerDto[]>('/brokers'),

  // Trades
  executeTrade: (data: CreateTradeDto) => 
    apiClient.post('/trades', data),
  
  getShareholderTrades: (
    shareholderId: number,
    filters?: {
      tradeType?: string;
      startDate?: string;
      endDate?: string;
      page?: number;
      pageSize?: number;
    }
  ) => apiClient.get(`/trades/shareholder/${shareholderId}`, { params: filters }),

  // Portfolio
  getPortfolioSummary: (shareholderId: number) => 
    apiClient.get<PortfolioSummaryDto>(`/portfolio/summary/${shareholderId}`),
  
  getHoldings: (shareholderId: number) => 
    apiClient.get<HoldingDto[]>(`/portfolio/holdings/${shareholderId}`),

  // Admin
  admin: {
    // Brokers
    getBrokers: () => apiClient.get('/admin/brokers'),
    createBroker: (data: any) => apiClient.post('/admin/brokers', data),
    updateBroker: (id: number, data: any) => apiClient.put(`/admin/brokers/${id}`, data),
    deleteBroker: (id: number) => apiClient.delete(`/admin/brokers/${id}`),

    // Exchanges
    getExchanges: () => apiClient.get('/admin/exchanges'),
    createExchange: (data: any) => apiClient.post('/admin/exchanges', data),
    updateExchange: (id: number, data: any) => apiClient.put(`/admin/exchanges/${id}`, data),
    deleteExchange: (id: number) => apiClient.delete(`/admin/exchanges/${id}`),

    // Companies
    getCompanies: () => apiClient.get('/admin/companies'),
    createCompany: (data: any) => apiClient.post('/admin/companies', data),
    updateCompany: (id: number, data: any) => apiClient.put(`/admin/companies/${id}`, data),
    deleteCompany: (id: number) => apiClient.delete(`/admin/companies/${id}`),
  },
};
```

Usage in components:

```typescript
import { ShareSphereService } from '../services/sharesphere.service';

function Dashboard() {
  useEffect(() => {
    const loadExchanges = async () => {
      try {
        const response = await ShareSphereService.getExchanges();
        setExchanges(response.data);
      } catch (error) {
        handleError(error);
      }
    };
    loadExchanges();
  }, []);
}
```

---

## Testing Backend Integration

### Sample Test with Mock Service Worker

Install MSW:
```bash
npm install msw --save-dev
```

Create `/src/mocks/handlers.ts`:

```typescript
import { rest } from 'msw';

export const handlers = [
  rest.get('https://localhost:7001/api/exchanges', (req, res, ctx) => {
    return res(
      ctx.status(200),
      ctx.json([
        {
          id: 1,
          name: 'New York Stock Exchange',
          code: 'NYSE',
          location: 'New York, USA',
          description: 'The largest stock exchange in the world',
        },
      ])
    );
  }),

  rest.post('https://localhost:7001/api/trades', (req, res, ctx) => {
    return res(
      ctx.status(201),
      ctx.json({
        id: 1,
        tradeType: 'Buy',
        quantity: 50,
        pricePerShare: 175.50,
        totalAmount: 8775.00,
        tradeDate: new Date().toISOString(),
        status: 'Completed',
        message: 'Trade executed successfully',
      })
    );
  }),
];
```

---

## Quick Reference

### Most Common API Calls

```typescript
// Load dashboard
ShareSphereService.getExchanges()
ShareSphereService.getExchangeCompanies(exchangeId)
ShareSphereService.getCompanyShares(companyId)

// Load portfolio
ShareSphereService.getPortfolioSummary(userId)
ShareSphereService.getHoldings(userId)
ShareSphereService.getShareholderTrades(userId, filters)

// Execute trade
ShareSphereService.executeTrade({
  shareholderId: userId,
  shareId: selectedShareId,
  brokerId: selectedBrokerId,
  tradeType: 'Buy',
  quantity: 50,
})

// Admin operations
ShareSphereService.admin.getBrokers()
ShareSphereService.admin.createBroker(data)
ShareSphereService.admin.updateBroker(id, data)
ShareSphereService.admin.deleteBroker(id)
```

---

**Document Version**: 1.0  
**Last Updated**: December 15, 2025  
**For**: ShareSphere Frontend Team
