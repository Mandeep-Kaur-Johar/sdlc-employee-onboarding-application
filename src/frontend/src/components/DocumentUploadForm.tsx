import { useState } from 'react';
import type { FormEvent } from 'react';

const MAX_UPLOAD_BYTES = 10 * 1024 * 1024;
const ALLOWED_TYPES = ['application/pdf', 'image/png', 'image/jpeg', 'image/jpg'];

interface DocumentUploadFormProps {
  documentTypes: string[];
  submitting: boolean;
  onUpload: (documentType: string, file: File) => Promise<void> | void;
}

/**
 * Upload form with client-side validation mirroring the server rules
 * (User Story 2791).
 */
export function DocumentUploadForm({ documentTypes, submitting, onUpload }: DocumentUploadFormProps) {
  const [documentType, setDocumentType] = useState(documentTypes[0] ?? '');
  const [file, setFile] = useState<File | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setValidationError(null);

    if (!documentType) {
      setValidationError('Select the document type you are uploading.');
      return;
    }

    if (!file) {
      setValidationError('Choose a file to upload.');
      return;
    }

    if (file.size > MAX_UPLOAD_BYTES) {
      setValidationError('The file exceeds the 10 MB limit.');
      return;
    }

    if (!ALLOWED_TYPES.includes(file.type)) {
      setValidationError('Only PDF, PNG and JPEG files are accepted.');
      return;
    }

    await onUpload(documentType, file);
    setFile(null);
  };

  return (
    <form className="form" onSubmit={handleSubmit} aria-label="Upload onboarding document">
      <div className="form__field">
        <label htmlFor="documentType">Document type</label>
        <select
          id="documentType"
          value={documentType}
          onChange={(event) => setDocumentType(event.target.value)}
        >
          {documentTypes.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
      </div>

      <div className="form__field">
        <label htmlFor="documentFile">File (PDF, PNG or JPEG, max 10 MB)</label>
        <input
          id="documentFile"
          type="file"
          accept=".pdf,.png,.jpg,.jpeg"
          onChange={(event) => setFile(event.target.files?.[0] ?? null)}
        />
      </div>

      {validationError ? (
        <p role="alert" className="status status--error">
          {validationError}
        </p>
      ) : null}

      <button type="submit" disabled={submitting}>
        {submitting ? 'Uploading…' : 'Upload document'}
      </button>
    </form>
  );
}
