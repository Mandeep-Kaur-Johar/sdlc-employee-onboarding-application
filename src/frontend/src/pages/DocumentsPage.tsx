import { useState } from 'react';
import { documentsApi } from '../services/onboardingApi';
import { toErrorMessage } from '../services/apiClient';
import { useAsync } from '../hooks/useAsync';
import { DocumentReviewPanel } from '../components/DocumentReviewPanel';
import { StatusMessage } from '../components/StatusMessage';
import { useAuth } from '../context/AuthContext';
import type { DocumentRecord } from '../models/onboarding';

/**
 * HR review queue. Documents can be approved or rejected and the employee is
 * notified about resubmission by the backend (User Story 2792).
 */
export function DocumentsPage() {
  const { user } = useAuth();
  const { data, loading, error, reload } = useAsync<DocumentRecord[]>(() => documentsApi.getPendingReview(), []);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<string | null>(null);

  const reviewerEmail = user?.email ?? 'hr-compliance@contoso.example';
  const pending = data ?? [];

  const handleReview = async (documentId: string, decision: 'Approved' | 'Rejected', comments: string) => {
    setBusy(true);
    setActionError(null);
    setConfirmation(null);
    try {
      const reviewed = await documentsApi.review(documentId, { decision, comments, reviewerEmail });
      setConfirmation(
        decision === 'Rejected'
          ? `${reviewed.documentType} was rejected and the employee was notified to resubmit.`
          : `${reviewed.documentType} was approved.`,
      );
      reload();
    } catch (caught: unknown) {
      setActionError(toErrorMessage(caught));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section aria-labelledby="documents-heading">
      <h1 id="documents-heading">Document review queue</h1>

      <StatusMessage
        loading={loading}
        error={error}
        empty={!loading && !error && pending.length === 0}
        emptyText="There are no documents awaiting review."
        loadingText="Loading submitted documents…"
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

      <div className="card-grid">
        {pending.map((document) => (
          <DocumentReviewPanel
            key={document.id}
            document={document}
            reviewerEmail={reviewerEmail}
            busy={busy}
            onReview={handleReview}
          />
        ))}
      </div>
    </section>
  );
}
