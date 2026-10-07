import { useState } from 'react';
import { notificationsApi, tasksApi } from '../services/onboardingApi';
import { toErrorMessage } from '../services/apiClient';
import { useAsync } from '../hooks/useAsync';
import { StatusMessage } from '../components/StatusMessage';
import { TaskTable } from '../components/TaskTable';
import type { OnboardingTask, TaskStatus } from '../models/onboarding';

/**
 * Cross-record task workspace with an overdue filter and a manual reminder and
 * escalation sweep (User Story 2794, 2795).
 */
export function TasksPage() {
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [assignee, setAssignee] = useState('');
  const { data, loading, error, reload } = useAsync<OnboardingTask[]>(
    () => tasksApi.list({ overdueOnly, assignedToEmail: assignee || undefined }),
    [overdueOnly, assignee],
  );
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [reminderSummary, setReminderSummary] = useState<string | null>(null);

  const tasks = data ?? [];

  const handleStatusChange = async (taskId: string, status: TaskStatus) => {
    setBusyTaskId(taskId);
    setActionError(null);
    try {
      await tasksApi.updateStatus(taskId, status);
      reload();
    } catch (caught: unknown) {
      setActionError(toErrorMessage(caught));
    } finally {
      setBusyTaskId(null);
    }
  };

  const handleRunReminders = async () => {
    setActionError(null);
    setReminderSummary(null);
    try {
      const result = await notificationsApi.runReminders();
      setReminderSummary(
        `${result.remindersSent} reminder(s) and ${result.escalationsSent} escalation(s) were sent.`,
      );
      reload();
    } catch (caught: unknown) {
      setActionError(toErrorMessage(caught));
    }
  };

  return (
    <section aria-labelledby="tasks-heading">
      <h1 id="tasks-heading">Onboarding tasks</h1>

      <form className="filters" aria-label="Filter tasks" onSubmit={(e) => e.preventDefault()}>
        <div className="form__field form__field--inline">
          <input
            id="filter-overdue"
            type="checkbox"
            checked={overdueOnly}
            onChange={(event) => setOverdueOnly(event.target.checked)}
          />
          <label htmlFor="filter-overdue">Overdue only</label>
        </div>

        <div className="form__field">
          <label htmlFor="filter-assignee">Assignee email</label>
          <input
            id="filter-assignee"
            type="email"
            value={assignee}
            onChange={(event) => setAssignee(event.target.value)}
          />
        </div>

        <button type="button" onClick={handleRunReminders}>
          Send reminders and escalations
        </button>
      </form>

      {reminderSummary ? (
        <p role="status" className="status status--success">
          {reminderSummary}
        </p>
      ) : null}

      {actionError ? (
        <p role="alert" className="status status--error">
          {actionError}
        </p>
      ) : null}

      <StatusMessage
        loading={loading}
        error={error}
        empty={!loading && !error && tasks.length === 0}
        emptyText="No tasks match the current filters."
        loadingText="Loading tasks…"
      />

      {tasks.length > 0 ? (
        <TaskTable tasks={tasks} onStatusChange={handleStatusChange} busyTaskId={busyTaskId} />
      ) : null}
    </section>
  );
}
