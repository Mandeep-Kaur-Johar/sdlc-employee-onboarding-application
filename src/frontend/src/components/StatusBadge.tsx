interface StatusBadgeProps {
  status: string;
  overdue?: boolean;
}

/** Colour-coded, text-labelled status badge. */
export function StatusBadge({ status, overdue = false }: StatusBadgeProps) {
  const tone = overdue
    ? 'danger'
    : status === 'Completed' || status === 'Approved'
      ? 'success'
      : status === 'Rejected' || status === 'Failed' || status === 'Blocked'
        ? 'danger'
        : status === 'InProgress'
          ? 'info'
          : 'neutral';

  return (
    <span className={`badge badge--${tone}`} data-testid="status-badge">
      {overdue ? `${status} · Overdue` : status}
    </span>
  );
}
