import { useState } from 'react';
import { documentsApi } from '../services/onboardingApi';
import { toErrorMessage } from '../services/apiClient';
import { usePortal } from '../hooks/usePortal';
import { useAuth } from '../context/AuthContext';
import { DocumentUploadForm } from '../components/DocumentUploadForm';
import { MilestoneList } from '../components/MilestoneList';
import { ProgressBar } from '../components/ProgressBar';
import { StatusBadge } from '../components/StatusBadge';
import { StatusMessage } from '../components/StatusMessage';
import { TaskTable } from '../components/TaskTable';

/**
 * Personalized employee portal giving secure access to assigned tasks,
 * documents and onboarding content (User Story 2800), including secure
 * document upload (User Story 2791).
 */
export function PortalPage() {
  const { user } = useAuth();
  const { data, loading, error, reload } = usePortal(user?.email);
  const [uploading, setUploading] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<string | null>(null);

  if (!user) {
    return (
      <section aria-labelledby="portal-heading">
        <h1 id="portal-heading">Onboarding portal</h1>
        <p role="status" className="status status--empty">
          Sign in to view your personalized onboarding portal.
        </p>
      </section>
    );
  }

  const handleUpload = async (documentType: string, file: File) => {
    if (!data) {
      return;
    }

    setUploading(true);
    setActionError(null);
    setConfirmation(null);
    try {
      await documentsApi.upload({
        onboardingRecordId: data.record.id,
        documentType,
        uploadedByEmail: user.email,
        file,
      });
      setConfirmation(`${documentType} was uploaded securely and is awaiting HR review.`);
      reload();
    } catch (caught: unknown) {
      setActionError(toErrorMessage(caught));
    } finally {
      setUploading(false);
    }
  };

  return (
    <section aria-labelledby="portal-heading">
      <h1 id="portal-heading">Welcome, {user.displayName}</h1>

      <StatusMessage
        loading={loading}
        error={error}
        empty={!loading && !error && !data}
        emptyText="No onboarding record is associated with your account yet."
        loadingText="Loading your onboarding portal…"
      />

      {data ? (
        <>
          <ProgressBar value={data.progress.completionPercentage} label="Your onboarding completion" />

          <h2>Your milestones</h2>
          <MilestoneList milestones={data.progress.milestones} />

          <h2>Your tasks</h2>
          <TaskTable tasks={data.myTasks} />

          <h2>Your documents</h2>
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

          <DocumentUploadForm
            documentTypes={data.requiredDocumentTypes}
            submitting={uploading}
            onUpload={handleUpload}
          />

          {data.myDocuments.length === 0 ? (
            <p className="status status--empty">You have not uploaded any documents yet.</p>
          ) : (
            <ul className="list">
              {data.myDocuments.map((document) => (
                <li key={document.id}>
                  {document.documentType} — {document.fileName} <StatusBadge status={document.status} />
                  {document.status === 'Rejected' && document.latestReviewComments ? (
                    <span className="list__note"> Resubmit: {document.latestReviewComments}</span>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
        </>
      ) : null}
    </section>
  );
}
