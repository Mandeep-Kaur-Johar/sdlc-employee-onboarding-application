import { useState } from 'react';
import { trainingApi } from '../services/onboardingApi';
import { toErrorMessage } from '../services/apiClient';
import { usePortal } from '../hooks/usePortal';
import { useAuth } from '../context/AuthContext';
import { StatusBadge } from '../components/StatusBadge';
import { StatusMessage } from '../components/StatusMessage';

/**
 * Orientation and training. Completion is recorded on the server and becomes
 * visible to HR and managers (User Story 2801).
 */
export function OrientationPage() {
  const { user } = useAuth();
  const { data, loading, error, reload } = usePortal(user?.email);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<string | null>(null);

  const assignments = data?.myTraining ?? [];

  const handleStart = async (assignmentId: string) => {
    setBusyId(assignmentId);
    setActionError(null);
    try {
      await trainingApi.start(assignmentId);
      reload();
    } catch (caught: unknown) {
      setActionError(toErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  };

  const handleComplete = async (assignmentId: string, courseName: string) => {
    setBusyId(assignmentId);
    setActionError(null);
    setConfirmation(null);
    try {
      await trainingApi.complete(assignmentId, 100);
      setConfirmation(`${courseName} was recorded as complete and is now visible to HR and your manager.`);
      reload();
    } catch (caught: unknown) {
      setActionError(toErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  };

  if (!user) {
    return (
      <section aria-labelledby="orientation-heading">
        <h1 id="orientation-heading">Orientation and training</h1>
        <p role="status" className="status status--empty">
          Sign in to view your orientation curriculum.
        </p>
      </section>
    );
  }

  return (
    <section aria-labelledby="orientation-heading">
      <h1 id="orientation-heading">Orientation and training</h1>

      <StatusMessage
        loading={loading}
        error={error}
        empty={!loading && !error && assignments.length === 0}
        emptyText="No orientation or training has been assigned to you."
        loadingText="Loading your orientation curriculum…"
      />

      {confirmation ? (
        <p role="status" className="status status--success">
          {confirmation}
        </p>
      ) : null}

      {actionError ? (
        <p role="alert" className="status status--error">
          {actionError}
        </p>
      ) : null}

      {assignments.length > 0 ? (
        <table className="data-table">
          <caption className="visually-hidden">Assigned orientation and training</caption>
          <thead>
            <tr>
              <th scope="col">Course</th>
              <th scope="col">Code</th>
              <th scope="col">Mandatory</th>
              <th scope="col">Due</th>
              <th scope="col">Status</th>
              <th scope="col">Score</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {assignments.map((assignment) => (
              <tr key={assignment.id}>
                <th scope="row">{assignment.courseName}</th>
                <td>{assignment.courseCode}</td>
                <td>{assignment.isMandatory ? 'Yes' : 'No'}</td>
                <td>{assignment.dueDate}</td>
                <td>
                  <StatusBadge status={assignment.status} />
                </td>
                <td>{assignment.scorePercentage != null ? `${assignment.scorePercentage}%` : '—'}</td>
                <td>
                  {assignment.status === 'NotStarted' ? (
                    <button type="button" disabled={busyId === assignment.id} onClick={() => handleStart(assignment.id)}>
                      Start
                    </button>
                  ) : null}
                  {assignment.status !== 'Completed' ? (
                    <button
                      type="button"
                      disabled={busyId === assignment.id}
                      onClick={() => handleComplete(assignment.id, assignment.courseName)}
                    >
                      Mark complete
                    </button>
                  ) : (
                    <span>Completed</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
