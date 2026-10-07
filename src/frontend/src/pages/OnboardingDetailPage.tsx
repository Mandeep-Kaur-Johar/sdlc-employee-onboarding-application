import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useOnboardingDetail } from '../hooks/useOnboarding';
import { tasksApi } from '../services/onboardingApi';
import { toErrorMessage } from '../services/apiClient';
import { MilestoneList } from '../components/MilestoneList';
import { ProgressBar } from '../components/ProgressBar';
import { StatusBadge } from '../components/StatusBadge';
import { StatusMessage } from '../components/StatusMessage';
import { TaskTable } from '../components/TaskTable';
import type { TaskStatus } from '../models/onboarding';

/**
 * Full onboarding record view with milestones, overdue tasks and completion
 * percentage (User Story 2789).
 */
export function OnboardingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data, loading, error, reload } = useOnboardingDetail(id);
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

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

  if (loading || error || !data) {
    return (
      <section aria-labelledby="detail-heading">
        <h1 id="detail-heading">Onboarding record</h1>
        <StatusMessage
          loading={loading}
          error={error}
          empty={!loading && !error && !data}
          emptyText="This onboarding record could not be found."
          loadingText="Loading onboarding record…"
        />
      </section>
    );
  }

  const { record, progress, tasks, documents, provisioningRequests, trainingAssignments } = data;

  return (
    <section aria-labelledby="detail-heading">
      <h1 id="detail-heading">{record.fullName}</h1>

      <dl className="card__meta">
        <dt>Role</dt>
        <dd>{record.role}</dd>
        <dt>Department</dt>
        <dd>{record.department}</dd>
        <dt>Location</dt>
        <dd>{record.location}</dd>
        <dt>Start date</dt>
        <dd>{record.startDate}</dd>
        <dt>Status</dt>
        <dd>
          <StatusBadge status={record.status} />
        </dd>
      </dl>

      <ProgressBar value={progress.completionPercentage} label="Overall onboarding completion" />

      <ul className="metrics">
        <li>
          Tasks completed: {progress.completedTasks} / {progress.totalTasks}
        </li>
        <li className={progress.overdueTasks > 0 ? 'metrics__alert' : undefined}>
          Overdue tasks: {progress.overdueTasks}
        </li>
        <li>
          Documents approved: {progress.approvedDocuments} (pending {progress.pendingDocuments}, rejected{' '}
          {progress.rejectedDocuments})
        </li>
        <li>
          Provisioning completed: {progress.completedProvisioning} (pending {progress.pendingProvisioning})
        </li>
        <li>
          Training completed: {progress.completedTraining} / {progress.totalTraining}
        </li>
      </ul>

      <h2>Milestones</h2>
      <MilestoneList milestones={progress.milestones} />

      {actionError ? (
        <p role="alert" className="status status--error">
          {actionError}
        </p>
      ) : null}

      <h2>Tasks</h2>
      <TaskTable tasks={tasks} onStatusChange={handleStatusChange} busyTaskId={busyTaskId} />

      <h2>Documents</h2>
      {documents.length === 0 ? (
        <p className="status status--empty">No documents have been submitted yet.</p>
      ) : (
        <ul className="list">
          {documents.map((doc) => (
            <li key={doc.id}>
              {doc.documentType} — {doc.fileName} <StatusBadge status={doc.status} />
            </li>
          ))}
        </ul>
      )}

      <h2>Provisioning</h2>
      {provisioningRequests.length === 0 ? (
        <p className="status status--empty">No provisioning requests have been raised.</p>
      ) : (
        <ul className="list">
          {provisioningRequests.map((request) => (
            <li key={request.id}>
              {request.systemName} ({request.itemType}) <StatusBadge status={request.status} />
            </li>
          ))}
        </ul>
      )}

      <h2>Orientation and training</h2>
      {trainingAssignments.length === 0 ? (
        <p className="status status--empty">No training has been assigned.</p>
      ) : (
        <ul className="list">
          {trainingAssignments.map((assignment) => (
            <li key={assignment.id}>
              {assignment.courseName} ({assignment.courseCode}) <StatusBadge status={assignment.status} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
