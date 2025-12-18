import { Building2 } from 'lucide-react';
import styles from '../styles/EmptyState.module.css';

export default function EmptyState() {
  return (
    <div className={styles.emptyState}>
      <Building2 size={64} className={styles.emptyIcon} />
      <h3>No Stock Exchanges Available</h3>
      <p>There are currently no stock exchanges to display.</p>
    </div>
  );
}
