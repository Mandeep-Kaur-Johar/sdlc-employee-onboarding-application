import type { OnboardingTask, TaskStatus } from '../models/onboarding';
import { StatusBadge } from './StatusBadge';

interface TaskTableProps {
  tasks: OnboardingTask[];
  onStatusChange?: (taskId: string, status: TaskStatus) => void;
  busyTaskId?: string | null;
}

const STATUSES: TaskStatus[] = ['Pending', 'InProgress', 'Completed', 'Blocked'];

/** Task list with inline status updates (User Story 2794, 2795). */
export function TaskTable({ tasks, onStatusChange, busyTaskId = null }: TaskTableProps) {
  if (tasks.length === 0) {
    return <p className="status status--empty">There are no onboarding tasks to show.</p>;
  }

  return (
    <table className="data-table">
      <caption className="visually-hidden">Onboarding tasks</caption>
      <thead>
        <tr>
          <th scope="col">Task</th>
          <th scope="col">Category</th>
          <th scope="col">Assigned to</th>
          <th scope="col">Due</th>
          <th scope="col">Status</th>
          {onStatusChange ? <th scope="col">Update</th> : null}
        </tr>
      </thead>
      <tbody>
        {tasks.map((task) => (
          <tr key={task.id} className={task.isOverdue ? 'row--overdue' : undefined}>
            <th scope="row">{task.title}</th>
            <td>{task.category}</td>
            <td>{task.assignedToEmail}</td>
            <td>{task.dueDate}</td>
            <td>
              <StatusBadge status={task.status} overdue={task.isOverdue} />
              {task.isEscalated ? <span className="badge badge--danger">Escalated</span> : null}
            </td>
            {onStatusChange ? (
              <td>
                <label className="visually-hidden" htmlFor={`status-${task.id}`}>
                  Update status for {task.title}
                </label>
                <select
                  id={`status-${task.id}`}
                  value={task.status}
                  disabled={busyTaskId === task.id}
                  onChange={(event) => onStatusChange(task.id, event.target.value as TaskStatus)}
                >
                  {STATUSES.map((status) => (
                    <option key={status} value={status}>
                      {status}
                    </option>
                  ))}
                </select>
              </td>
            ) : null}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
