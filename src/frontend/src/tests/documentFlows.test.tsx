import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DocumentUploadForm } from '../components/DocumentUploadForm';
import { DocumentReviewPanel } from '../components/DocumentReviewPanel';
import { makeDocument } from './testUtils';

const TYPES = ['PhotoIdentification', 'RightToWork'];

describe('DocumentUploadForm (User Story 2791)', () => {
  it('requires a file before uploading', async () => {
    const onUpload = vi.fn();
    render(<DocumentUploadForm documentTypes={TYPES} submitting={false} onUpload={onUpload} />);

    await userEvent.click(screen.getByRole('button', { name: 'Upload document' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Choose a file to upload.');
    expect(onUpload).not.toHaveBeenCalled();
  });

  it('rejects a disallowed file type', async () => {
    const onUpload = vi.fn();
    render(<DocumentUploadForm documentTypes={TYPES} submitting={false} onUpload={onUpload} />);

    const file = new File(['binary'], 'malware.exe', { type: 'application/x-msdownload' });
    await userEvent.upload(screen.getByLabelText(/File \(PDF, PNG or JPEG/), file);
    await userEvent.click(screen.getByRole('button', { name: 'Upload document' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Only PDF, PNG and JPEG files are accepted.');
    expect(onUpload).not.toHaveBeenCalled();
  });

  it('uploads a valid PDF with the selected document type', async () => {
    const onUpload = vi.fn().mockResolvedValue(undefined);
    render(<DocumentUploadForm documentTypes={TYPES} submitting={false} onUpload={onUpload} />);

    await userEvent.selectOptions(screen.getByLabelText('Document type'), 'RightToWork');
    const file = new File(['%PDF'], 'visa.pdf', { type: 'application/pdf' });
    await userEvent.upload(screen.getByLabelText(/File \(PDF, PNG or JPEG/), file);
    await userEvent.click(screen.getByRole('button', { name: 'Upload document' }));

    expect(onUpload).toHaveBeenCalledWith('RightToWork', file);
  });

  it('disables the submit button while uploading', () => {
    render(<DocumentUploadForm documentTypes={TYPES} submitting onUpload={vi.fn()} />);
    expect(screen.getByRole('button', { name: 'Uploading…' })).toBeDisabled();
  });
});

describe('DocumentReviewPanel (User Story 2792)', () => {
  it('approves a document without requiring comments', async () => {
    const onReview = vi.fn().mockResolvedValue(undefined);
    render(
      <DocumentReviewPanel
        document={makeDocument()}
        reviewerEmail="hr-compliance@contoso.example"
        busy={false}
        onReview={onReview}
      />,
    );

    await userEvent.click(screen.getByRole('button', { name: 'Approve' }));

    expect(onReview).toHaveBeenCalledWith('doc-1', 'Approved', '');
  });

  it('requires comments before rejecting so the employee can resubmit', async () => {
    const onReview = vi.fn();
    render(
      <DocumentReviewPanel
        document={makeDocument()}
        reviewerEmail="hr-compliance@contoso.example"
        busy={false}
        onReview={onReview}
      />,
    );

    await userEvent.click(screen.getByRole('button', { name: 'Reject' }));

    expect(screen.getByRole('alert')).toHaveTextContent(
      'Add a comment explaining why the document is rejected.',
    );
    expect(onReview).not.toHaveBeenCalled();
  });

  it('rejects a document with the reviewer comments attached', async () => {
    const onReview = vi.fn().mockResolvedValue(undefined);
    render(
      <DocumentReviewPanel
        document={makeDocument()}
        reviewerEmail="hr-compliance@contoso.example"
        busy={false}
        onReview={onReview}
      />,
    );

    await userEvent.type(screen.getByLabelText('Review comments'), 'The document is expired.');
    await userEvent.click(screen.getByRole('button', { name: 'Reject' }));

    expect(onReview).toHaveBeenCalledWith('doc-1', 'Rejected', 'The document is expired.');
  });
});
