# ShareSphere Frontend

Stock Exchange Dashboard built with React and Vite, integrated with the ShareSphere.Api backend.

## Features

- ✅ View all available stock exchanges from backend API
- ✅ Clean, modern UI matching design specifications
- ✅ Responsive layout (mobile, tablet, desktop)
- ✅ Loading states and error handling
- ✅ Empty state when no exchanges are available
- ✅ Integrated with ShareSphere.Api backend

## Tech Stack

- **React 18+** - Frontend framework
- **Vite** - Build tool and dev server
- **Lucide React** - Icon library
- **CSS Modules** - Scoped component styling
- **ShareSphere.Api** - .NET backend API

## Backend Integration

### API Configuration
- **Backend URL**: `https://localhost:7274/api`
- **Endpoint**: `/stockexchanges`
- **Method**: GET
- **Auth**: Bearer token (optional, stored in localStorage)

### Backend Response Format
```json
[
  {
    "exchangeId": 1,
    "name": "New York Stock Exchange",
    "country": "United States",
    "currency": "USD"
  }
]
```

### Frontend Transformation
The API service transforms backend data for display:
- `exchangeId` → `id`
- `country` → `location`
- `currency` → `code`
- Generates `description` from country and currency

## Getting Started

### Prerequisites

1. **Backend API running**:
   ```bash
   cd ShareSphere.Api
   dotnet run
   ```
   Backend will run on `https://localhost:7274`

2. **Node.js 16+** installed

### Installation & Running

1. Navigate to frontend directory:
   ```bash
   cd ShareSphereFront
   ```

2. Install dependencies (if not already done):
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

4. Open your browser:
   ```
   http://localhost:5173
   ```

### Build for Production

```bash
npm run build
```

The production-ready files will be in the `dist` folder.

## Project Structure

```
ShareSphereFront/
├── src/
│   ├── pages/
│   │   └── StockExchanges.jsx       # Main dashboard page
│   ├── components/
│   │   ├── StockExchangeCard.jsx    # Exchange card component
│   │   ├── LoadingSpinner.jsx       # Loading state
│   │   ├── EmptyState.jsx           # Empty state
│   │   └── ErrorMessage.jsx         # Error state with retry
│   ├── services/
│   │   └── api.js                   # API service layer (backend integration)
│   ├── styles/
│   │   ├── StockExchanges.module.css
│   │   ├── StockExchangeCard.module.css
│   │   ├── LoadingSpinner.module.css
│   │   ├── EmptyState.module.css
│   │   └── ErrorMessage.module.css
│   ├── App.jsx                      # Root component
│   ├── App.css                      # App styles
│   ├── index.css                    # Global styles
│   └── main.jsx                     # Entry point
├── index.html
├── package.json
└── vite.config.js
```

## Design Specifications

### Color Palette
- **Primary Blue**: `#5B5FED`
- **Text Dark**: `#1A1A1A`
- **Text Gray**: `#6B7280`
- **Background**: `#F8F9FA`
- **Card White**: `#FFFFFF`
- **Border**: `#E5E7EB`

### Typography
- **Font Family**: System UI, Segoe UI, Roboto, Helvetica Neue
- **Page Title**: 36px, weight 700
- **Card Title**: 20px, weight 600
- **Body Text**: 14-16px, weight 400

## Features Implemented

✅ Stock exchange listing from backend API  
✅ API response transformation layer  
✅ Loading state with animated spinner  
✅ Empty state when no data  
✅ Error handling with retry button  
✅ Responsive grid layout  
✅ Hover effects on cards  
✅ Clean, modern design  
✅ CSS Modules for scoped styling  

## Troubleshooting

### CORS Issues
If you encounter CORS errors, ensure the backend `Program.cs` has CORS configured for `http://localhost:5173`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### SSL Certificate Issues
If you get SSL certificate errors, you may need to:
1. Trust the development certificate
2. Or temporarily modify the fetch to skip validation (development only)

### Backend Not Running
Ensure the backend API is running on `https://localhost:7274`:
```bash
cd ShareSphere.Api
dotnet run
```

## Future Enhancements

- Add navigation to exchange detail pages
- Implement search/filter functionality
- Add pagination for large datasets
- Implement full authentication flow
- Add routing with React Router
- Company listings per exchange

## License

Copyright © 2025 ShareSphere

