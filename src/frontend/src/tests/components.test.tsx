import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ProgressBar } from '../components/ProgressBar';
import { StatusBadge } from '../components/StatusBadge';
import { StatusMessage } from '../components/StatusMessage';
import { MilestoneList } from '../components/MilestoneList';
import { TaskTable } from '../components/TaskTable';
import { makeMilestone, makeTask } from './testUtils';

describe('ProgressBar (User Story 2789)', () => {
  it('renders the completion percentage with accessible attributes', () => {
    render(<ProgressBar value={42} label="Completion" />);
    const bar = screen.getByRole('progressbar', { name: 'Completion' });
    expect(bar).toHaveAttribute('aria-valuenow', '42');
    expect(screen.getByText('42%')).toBeInTheDocument();
  });

  it('clamps out-of-range values', () => {
    render(<ProgressBar value={140} label="Completion" />);
    expect(screen.getByRole('progressbar', { name: 'Completion' })).toHaveAttribute('aria-valuenow', '100');
  });
});

describe('StatusBadge', () => {
  it('flags overdue items', () => {
    render(<StatusBadge status="Pending" overdue />);
    expect(screen.getByTestId('status-badge')).toHaveTextContent('Pending · Overdue');
  });
});

describe('StatusMessage', () => {
  it('shows loading, error and empty states in priority order', () => {
    const { rerender } = render(<StatusMessage loading />);
    expect(screen.getByRole('status')).toHaveTextContent('Loading');

    rerender(<StatusMessage error="Service unavailable" />);
    expect(screen.getByRole('alert')).toHaveTextContent('Service unavailable');

    rerender(<StatusMessage empty emptyText="Nothing here" />);
    expect(screen.getByRole('status')).toHaveTextContent('Nothing here');
  });
});

describe('MilestoneList (User Story 2789)', () => {
  it('renders milestones in sort order', () => {
    render(
      <MilestoneList
        milestones={[
          makeMilestone({ id: 'm2', name: 'Day One Ready', sortOrder: 5 }),
          makeMilestone({ id: 'm1', name: 'Offer Accepted', sortOrder: 1, status: 'Completed' }),
        ]}
      />,
    );

    const items = screen.getAllByRole('listitem');
    expect(items[0]).toHaveTextContent('Offer Accepted');
    expect(items[1]).toHaveTextContent('Day One Ready');
  });
});

describe('TaskTable (User Story 2794, 2795)', () => {
  it('shows an empty state when there are no tasks', () => {
    render(<TaskTable tasks={[]} />);
    expect(screen.getByText('There are no onboarding tasks to show.')).toBeInTheDocument();
  });

  it('marks overdue and escalated tasks', () => {
    render(<TaskTable tasks={[makeTask({ isOverdue: true, isEscalated: true })]} />);
    expect(screen.getByTestId('status-badge')).toHaveTextContent('Overdue');
    expect(screen.getByText('Escalated')).toBeInTheDocument();
  });

  it('raises a status change when the assignee updates a task', async () => {
    const onStatusChange = vi.fn();
    render(<TaskTable tasks={[makeTask()]} onStatusChange={onStatusChange} />);

    await userEvent.selectOptions(
      screen.getByLabelText('Update status for Sign employment contract'),
      'Completed',
    );

    expect(onStatusChange).toHaveBeenCalledWith('task-1', 'Completed');
  });
});
