import styles from '../styles/LoadingSpinner.module.css';

export default function LoadingSpinner() {
  return (
    <div className={styles.loadingContainer}>
      <div className={styles.spinner}></div>
      <p>Loading stock exchanges...</p>
    </div>
  );
}
