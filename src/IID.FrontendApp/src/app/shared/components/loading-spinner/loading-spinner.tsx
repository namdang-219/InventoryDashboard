import clsx from 'clsx';
import './loading-spinner.scss';

export interface LoadingSpinnerProps {
  message?: string;
  size?: 'sm' | 'md' | 'lg';
}

export function LoadingSpinnerComponent({
  message = 'Loading data...',
  size = 'md'
}: LoadingSpinnerProps) {
  return (
    <div className={clsx('iid-loading-spinner', 'spinner-container', `size-${size}`)}>
      <div className="spinner-ring">
        <div className="ring-core" />
      </div>
      {message && <span className="spinner-message">{message}</span>}
    </div>
  );
}
