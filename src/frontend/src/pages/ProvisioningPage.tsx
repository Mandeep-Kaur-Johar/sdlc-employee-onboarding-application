import { useState } from 'react';
import { provisioningApi } from '../services/onboardingApi';
import { toErrorMessage } from '../services/apiClient';
import { useAsync } from '../hooks/useAsync';
import { StatusBadge } from '../components/StatusBadge';
import { StatusMessage } from '../components/StatusMessage';
import type { ProvisioningRequest, ProvisioningStatus } from '../models/onboarding';

const STATUSES: ProvisioningStatus[] = ['Requested', 'InProgress', 'Completed', 'Failed'];

/**
 * Provisioning tracker. Status is visible to HR and can be updated by the
 * integrated systems through the same endpoint (User Story 2797, 2798).
 */
export function ProvisioningPage() {
  const { data, loading, error, reload } = useAsync<ProvisioningRequest[]>(
    () => provisioningApi.listOpen(),
    [],
  );
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const requests = data ?? [];

  const handleStatusChange = async (requestId: string, status: ProvisioningStatus) => {
    setBusyId(requestId);
    setActionError(null);
    try {
      await provisioningApi.updateStatus(requestId, { status, updatedBySystem: 'Onboarding Portal' });
      reload();
    } catch (caught: unknown) {
      setActionError(toErrorMessage(caught));
    } finally {
      setBusyId(null);
    }
  };

  return (
    <section aria-labelledby="provisioning-heading">
      <h1 id="provisioning-heading">IT provisioning tracker</h1>

      <StatusMessage
        loading={loading}
        error={error}
        empty={!loading && !error && requests.length === 0}
        emptyText="All provisioning requests are complete."
        loadingText="Loading provisioning requests…"
      />

      {actionError ? (
        <p role="alert" className="status status--error">
          {actionError}
        </p>
      ) : null}

      {requests.length > 0 ? (
        <table className="data-table">
          <caption className="visually-hidden">Open provisioning requests</caption>
          <thead>
            <tr>
              <th scope="col">System or equipment</th>
              <th scope="col">Type</th>
              <th scope="col">Required by</th>
              <th scope="col">Ticket</th>
              <th scope="col">Status</th>
              <th scope="col">Update</th>
            </tr>
          </thead>
          <tbody>
            {requests.map((request) => (
              <tr key={request.id}>
                <th scope="row">{request.systemName}</th>
                <td>{request.itemType}</td>
                <td>{request.requiredByDate}</td>
                <td>{request.externalTicketId ?? '—'}</td>
                <td>
                  <StatusBadge status={request.status} />
                </td>
                <td>
                  <label className="visually-hidden" htmlFor={`prov-${request.id}`}>
                    Update status for {request.systemName}
                  </label>
                  <select
                    id={`prov-${request.id}`}
                    value={request.status}
                    disabled={busyId === request.id}
                    onChange={(event) => handleStatusChange(request.id, event.target.value as ProvisioningStatus)}
                  >
                    {STATUSES.map((status) => (
                      <option key={status} value={status}>
                        {status}
                      </option>
                    ))}
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
