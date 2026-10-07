import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders, makePortal, makeTask } from './testUtils';
import { TOKEN_STORAGE_KEY } from '../services/apiClient';
import { DashboardPage } from '../pages/DashboardPage';
import { TasksPage } from '../pages/TasksPage';
import { PortalPage } from '../pages/PortalPage';
import { onboardingApi, tasksApi, notificationsApi, portalApi } from '../services/onboardingApi';

const RECORD = makePortal().record;

beforeEach(() => {
  localStorage.clear();
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('DashboardPage (User Story 2789)', () => {
  it('shows a loading state and then the onboarding records with completion', async () => {
    vi.spyOn(onboardingApi, 'list').mockResolvedValue([RECORD]);

    renderWithProviders(<DashboardPage />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading onboarding records…');

    await waitFor(() => expect(screen.getByRole('link', { name: 'Ada Lovelace' })).toBeInTheDocument());
    expect(screen.getByRole('progressbar', { name: 'Completion for Ada Lovelace' })).toHaveAttribute(
      'aria-valuenow',
      '40',
    );
  });

  it('shows an empty state when there are no records', async () => {
    vi.spyOn(onboardingApi, 'list').mockResolvedValue([]);

    renderWithProviders(<DashboardPage />);

    await waitFor(() =>
      expect(screen.getByText('No onboarding records match the current filters.')).toBeInTheDocument(),
    );
  });

  it('shows an error state when the API fails', async () => {
    vi.spyOn(onboardingApi, 'list').mockRejectedValue(new Error('Service unavailable'));

    renderWithProviders(<DashboardPage />);

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Service unavailable'));
  });
});

describe('TasksPage (User Story 2794, 2795)', () => {
  it('lists tasks and runs the reminder and escalation sweep', async () => {
    vi.spyOn(tasksApi, 'list').mockResolvedValue([makeTask({ isOverdue: true })]);
    const runReminders = vi.spyOn(notificationsApi, 'runReminders').mockResolvedValue({
      remindersSent: 3,
      escalationsSent: 1,
      executedAtUtc: '2026-10-07T12:00:00Z',
      notifications: [],
    });

    renderWithProviders(<TasksPage />);

    await waitFor(() => expect(screen.getByText('Sign employment contract')).toBeInTheDocument());

    await userEvent.click(screen.getByRole('button', { name: 'Send reminders and escalations' }));

    await waitFor(() =>
      expect(screen.getByText('3 reminder(s) and 1 escalation(s) were sent.')).toBeInTheDocument(),
    );
    expect(runReminders).toHaveBeenCalledTimes(1);
  });

  it('updates a task status through the API', async () => {
    vi.spyOn(tasksApi, 'list').mockResolvedValue([makeTask()]);
    const update = vi.spyOn(tasksApi, 'updateStatus').mockResolvedValue(makeTask({ status: 'Completed' }));

    renderWithProviders(<TasksPage />);

    await waitFor(() => expect(screen.getByText('Sign employment contract')).toBeInTheDocument());
    await userEvent.selectOptions(
      screen.getByLabelText('Update status for Sign employment contract'),
      'Completed',
    );

    await waitFor(() => expect(update).toHaveBeenCalledWith('task-1', 'Completed'));
  });
});

describe('PortalPage (User Story 2800, 2791)', () => {
  it('asks unauthenticated visitors to sign in', () => {
    renderWithProviders(<PortalPage />);
    expect(screen.getByText('Sign in to view your personalized onboarding portal.')).toBeInTheDocument();
  });

  it('shows the signed-in employee their own tasks, documents and upload form', async () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'test-token');
    localStorage.setItem(
      'onboarding.user',
      JSON.stringify({
        email: 'ada.lovelace@contoso.example',
        displayName: 'Ada Lovelace',
        roles: ['Employee'],
      }),
    );

    vi.spyOn(portalApi, 'getMyPortal').mockResolvedValue(makePortal());

    renderWithProviders(<PortalPage />);

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Welcome, Ada Lovelace' })).toBeInTheDocument(),
    );
    expect(screen.getByRole('form', { name: 'Upload onboarding document' })).toBeInTheDocument();
    expect(screen.getByText('Sign employment contract')).toBeInTheDocument();
    expect(screen.getByRole('progressbar', { name: 'Your onboarding completion' })).toHaveAttribute(
      'aria-valuenow',
      '40',
    );
  });
});
