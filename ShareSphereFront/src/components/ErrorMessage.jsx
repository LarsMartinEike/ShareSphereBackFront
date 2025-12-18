import { AlertCircle } from 'lucide-react';
import styles from '../styles/ErrorMessage.module.css';

export default function ErrorMessage({ message, onRetry }) {
  return (
    <div className={styles.errorContainer}>
      <AlertCircle size={64} className={styles.errorIcon} />
      <h3>Oops! Something went wrong</h3>
      <p>{message || 'Failed to load stock exchanges. Please try again.'}</p>
      {onRetry && (
        <button className={styles.retryButton} onClick={onRetry}>
          Try Again
        </button>
      )}
    </div>
  );
}
