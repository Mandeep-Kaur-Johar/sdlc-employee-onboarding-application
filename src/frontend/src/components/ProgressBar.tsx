interface ProgressBarProps {
  value: number;
  label: string;
}

/** Completion percentage indicator used on the dashboard (User Story 2789). */
export function ProgressBar({ value, label }: ProgressBarProps) {
  const safeValue = Number.isFinite(value) ? Math.min(100, Math.max(0, Math.round(value))) : 0;

  return (
    <div className="progress">
      <div
        role="progressbar"
        aria-label={label}
        aria-valuenow={safeValue}
        aria-valuemin={0}
        aria-valuemax={100}
        className="progress__track"
      >
        <span className="progress__fill" style={{ width: `${safeValue}%` }} />
      </div>
      <span className="progress__value">{safeValue}%</span>
    </div>
  );
}
