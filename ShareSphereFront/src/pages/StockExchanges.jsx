import { useState, useEffect } from 'react';
import { fetchStockExchanges } from '../services/api';
import StockExchangeCard from '../components/StockExchangeCard';
import LoadingSpinner from '../components/LoadingSpinner';
import EmptyState from '../components/EmptyState';
import ErrorMessage from '../components/ErrorMessage';
import styles from '../styles/StockExchanges.module.css';

export default function StockExchanges() {
  const [exchanges, setExchanges] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadExchanges();
  }, []);

  async function loadExchanges() {
    try {
      setLoading(true);
      setError(null);
      const data = await fetchStockExchanges();
      setExchanges(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  function handleExchangeClick(id) {
    console.log('Exchange clicked:', id);
    // TODO: Navigate to exchange details page
  }

  // Render different states
  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage message={error} onRetry={loadExchanges} />;
  if (exchanges.length === 0) return <EmptyState />;

  return (
    <div className={styles.stockExchangesPage}>
      <div className={styles.breadcrumb}>Exchanges</div>
      <h1>Stock Exchanges</h1>
      <p className={styles.subtitle}>Select an exchange to view available companies and shares</p>
      
      <div className={styles.exchangesGrid}>
        {exchanges.map(exchange => (
          <StockExchangeCard 
            key={exchange.id} 
            exchange={exchange}
            onClick={() => handleExchangeClick(exchange.id)}
          />
        ))}
      </div>
    </div>
  );
}
