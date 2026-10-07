interface StatusMessageProps {
  loading?: boolean;
  error?: string | null;
  empty?: boolean;
  emptyText?: string;
  loadingText?: string;
}

/**
 * Accessible loading / error / empty state used by every data-driven view.
 * Returns null when there is content to render.
 */
export function StatusMessage({
  loading = false,
  error = null,
  empty = false,
  emptyText = 'There is nothing to show yet.',
  loadingText = 'Loading…',
}: StatusMessageProps) {
  if (loading) {
    return (
      <p role="status" aria-live="polite" className="status status--loading">
        {loadingText}
      </p>
    );
  }

  if (error) {
    return (
      <p role="alert" className="status status--error">
        {error}
      </p>
    );
  }

  if (empty) {
    return (
      <p role="status" className="status status--empty">
        {emptyText}
      </p>
    );
  }

  return null;
}
