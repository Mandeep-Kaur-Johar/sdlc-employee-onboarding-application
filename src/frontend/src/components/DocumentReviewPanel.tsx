import { useState } from 'react';
import type { DocumentRecord } from '../models/onboarding';
import { StatusBadge } from './StatusBadge';

interface DocumentReviewPanelProps {
  document: DocumentRecord;
  reviewerEmail: string;
  busy: boolean;
  onReview: (documentId: string, decision: 'Approved' | 'Rejected', comments: string) => Promise<void> | void;
}

/**
 * Approve or reject a submitted document. Comments are mandatory for a
 * rejection so the employee knows what to resubmit (User Story 2792).
 */
export function DocumentReviewPanel({ document, reviewerEmail, busy, onReview }: DocumentReviewPanelProps) {
  const [comments, setComments] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const submit = async (decision: 'Approved' | 'Rejected') => {
    setValidationError(null);

    if (decision === 'Rejected' && comments.trim().length === 0) {
      setValidationError('Add a comment explaining why the document is rejected.');
      return;
    }

    await onReview(document.id, decision, comments.trim());
    setComments('');
  };

  return (
    <article className="card" aria-label={`Review ${document.documentType}`}>
      <header className="card__header">
        <h3>{document.documentType}</h3>
        <StatusBadge status={document.status} />
      </header>

      <dl className="card__meta">
        <dt>File</dt>
        <dd>{document.fileName}</dd>
        <dt>Uploaded by</dt>
        <dd>{document.uploadedByEmail}</dd>
        <dt>Size</dt>
        <dd>{Math.max(1, Math.round(document.sizeBytes / 1024))} KB</dd>
      </dl>

      <div className="form__field">
        <label htmlFor={`comments-${document.id}`}>Review comments</label>
        <textarea
          id={`comments-${document.id}`}
          value={comments}
          rows={3}
          onChange={(event) => setComments(event.target.value)}
        />
      </div>

      {validationError ? (
        <p role="alert" className="status status--error">
          {validationError}
        </p>
      ) : null}

      <p className="card__footnote">Reviewing as {reviewerEmail}</p>

      <div className="card__actions">
        <button type="button" disabled={busy} onClick={() => submit('Approved')}>
          Approve
        </button>
        <button type="button" className="button--danger" disabled={busy} onClick={() => submit('Rejected')}>
          Reject
        </button>
      </div>
    </article>
  );
}
