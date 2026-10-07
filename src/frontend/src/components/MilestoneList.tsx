import type { Milestone } from '../models/onboarding';
import { StatusBadge } from './StatusBadge';

/** Milestone timeline shown on the onboarding dashboard (User Story 2789). */
export function MilestoneList({ milestones }: { milestones: Milestone[] }) {
  if (milestones.length === 0) {
    return <p className="status status--empty">No milestones have been defined.</p>;
  }

  return (
    <ol className="milestones">
      {[...milestones]
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((milestone) => (
          <li key={milestone.id} className="milestones__item">
            <span className="milestones__name">{milestone.name}</span>
            <span className="milestones__date">Target {milestone.targetDate}</span>
            <StatusBadge status={milestone.status} />
          </li>
        ))}
    </ol>
  );
}
