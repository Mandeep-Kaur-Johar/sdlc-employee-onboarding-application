import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useOnboardingList } from '../hooks/useOnboarding';
import { ProgressBar } from '../components/ProgressBar';
import { StatusBadge } from '../components/StatusBadge';
import { StatusMessage } from '../components/StatusMessage';

/**
 * HR dashboard showing onboarding status, completion percentage and the ability
 * to drill into milestones and overdue tasks (User Story 2789).
 */
export function DashboardPage() {
  const [status, setStatus] = useState('');
  const [department, setDepartment] = useState('');
  const { data, loading, error } = useOnboardingList(status, department);

  const records = data ?? [];

  return (
    <section aria-labelledby="dashboard-heading">
      <h1 id="dashboard-heading">Onboarding dashboard</h1>

      <form className="filters" aria-label="Filter onboarding records" onSubmit={(e) => e.preventDefault()}>
        <div className="form__field">
          <label htmlFor="filter-status">Status</label>
          <select id="filter-status" value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="">All statuses</option>
            <option value="Initiated">Initiated</option>
            <option value="InProgress">In progress</option>
            <option value="ReadyForDayOne">Ready for day one</option>
            <option value="Completed">Completed</option>
          </select>
        </div>

        <div className="form__field">
          <label htmlFor="filter-department">Department</label>
          <input
            id="filter-department"
            type="text"
            value={department}
            placeholder="e.g. Engineering"
            onChange={(event) => setDepartment(event.target.value)}
          />
        </div>
      </form>

      <StatusMessage
        loading={loading}
        error={error}
        empty={!loading && !error && records.length === 0}
        emptyText="No onboarding records match the current filters."
        loadingText="Loading onboarding records…"
      />

      {records.length > 0 ? (
        <table className="data-table">
          <caption className="visually-hidden">Onboarding records</caption>
          <thead>
            <tr>
              <th scope="col">Employee</th>
              <th scope="col">Role</th>
              <th scope="col">Department</th>
              <th scope="col">Start date</th>
              <th scope="col">Status</th>
              <th scope="col">Completion</th>
            </tr>
          </thead>
          <tbody>
            {records.map((record) => (
              <tr key={record.id}>
                <th scope="row">
                  <Link to={`/onboarding/${record.id}`}>{record.fullName}</Link>
                </th>
                <td>{record.role}</td>
                <td>{record.department}</td>
                <td>{record.startDate}</td>
                <td>
                  <StatusBadge status={record.status} />
                </td>
                <td>
                  <ProgressBar value={record.completionPercentage} label={`Completion for ${record.fullName}`} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
