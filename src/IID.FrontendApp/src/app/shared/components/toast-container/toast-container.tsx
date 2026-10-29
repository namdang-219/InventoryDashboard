import clsx from 'clsx';
import { useToast } from '../../../core/services/toast.service';
import './toast-container.scss';

export function ToastContainerComponent() {
  const { toasts, dismiss } = useToast();

  return (
    <div className="iid-toast-container toast-stack" aria-live="polite">
      {toasts.map(toast => (
        <div
          key={toast.id}
          className={clsx('toast-item', `toast-${toast.type}`)}
          onClick={() => dismiss(toast.id)}
        >
          <div className="toast-icon">
            {toast.type === 'success' ? (
              <span className="material-symbols-outlined">check_circle</span>
            ) : toast.type === 'error' ? (
              <span className="material-symbols-outlined">error</span>
            ) : toast.type === 'warning' ? (
              <span className="material-symbols-outlined">warning</span>
            ) : (
              <span className="material-symbols-outlined">info</span>
            )}
          </div>
          <div className="toast-body">
            <h4 className="toast-title">{toast.title}</h4>
            <p className="toast-message">{toast.message}</p>
          </div>
          <button
            type="button"
            className="toast-close"
            onClick={e => {
              e.stopPropagation();
              dismiss(toast.id);
            }}
          >
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>
      ))}
    </div>
  );
}
