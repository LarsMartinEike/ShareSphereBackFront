import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { TrendingUp, Home, Briefcase, ArrowLeftRight, Settings } from 'lucide-react';

interface NavigationProps {
  userRole: 'user' | 'admin';
}

export function Navigation({ userRole }: NavigationProps) {
  const location = useLocation();

  const isActive = (path: string) => {
    return location.pathname === path;
  };

  const navLinks = [
    { path: '/', label: 'Dashboard', icon: Home },
    { path: '/portfolio', label: 'Portfolio', icon: Briefcase },
    { path: '/trade', label: 'Trade', icon: ArrowLeftRight },
  ];

  if (userRole === 'admin') {
    navLinks.push({ path: '/admin', label: 'Admin', icon: Settings });
  }

  return (
    <nav className="bg-white shadow-sm border-b border-gray-200">
      <div className="container mx-auto px-4 max-w-7xl">
        <div className="flex items-center justify-between h-16">
          <div className="flex items-center gap-2">
            <TrendingUp className="w-6 h-6 text-blue-600" />
            <span className="text-gray-900">ShareSphere</span>
          </div>
          
          <div className="flex gap-1">
            {navLinks.map(({ path, label, icon: Icon }) => (
              <Link
                key={path}
                to={path}
                className={`
                  flex items-center gap-2 px-4 py-2 rounded-md transition-colors
                  ${isActive(path)
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                  }
                `}
              >
                <Icon className="w-4 h-4" />
                <span>{label}</span>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </nav>
  );
}
