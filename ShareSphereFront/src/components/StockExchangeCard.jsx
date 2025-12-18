import { Building2, MapPin, ChevronRight } from 'lucide-react';
import styles from '../styles/StockExchangeCard.module.css';

export default function StockExchangeCard({ exchange, onClick }) {
  return (
    <div className={styles.exchangeCard} onClick={onClick}>
      <div className={styles.cardHeader}>
        <Building2 className={styles.exchangeIcon} />
        <ChevronRight className={styles.chevronIcon} />
      </div>
      
      <h3 className={styles.exchangeName}>{exchange.name}</h3>
      <span className={styles.exchangeCode}>{exchange.code}</span>
      
      <div className={styles.exchangeLocation}>
        <MapPin size={14} />
        <span>{exchange.location}</span>
      </div>
      
      <p className={styles.exchangeDescription}>{exchange.description}</p>
    </div>
  );
}
