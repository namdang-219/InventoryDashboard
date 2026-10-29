import './empty-state.scss';

export interface EmptyStateProps {
  icon?: string;
  title?: string;
  description?: string;
  actionLabel?: string | null;
  onActionClick?: () => void;
}

export function EmptyStateComponent({
  icon = 'inbox',
  title = 'No records found',
  description = 'There are no items to display at this time.',
  actionLabel = null,
  onActionClick
}: EmptyStateProps) {
  return (
    <div className="iid-empty-state empty-state">
      <div className="icon-bubble">
        <span className="material-symbols-outlined">{icon}</span>
      </div>
      <h3 className="title">{title}</h3>
      <p className="description">{description}</p>

      {actionLabel && (
        <button
          type="button"
          className="iid-btn iid-btn-primary iid-btn-sm"
          onClick={() => onActionClick?.()}
        >
          {actionLabel}
        </button>
      )}
    </div>
  );
}
