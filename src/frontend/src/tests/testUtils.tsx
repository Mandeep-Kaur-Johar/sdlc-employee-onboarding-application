import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../context/AuthContext';
import type {
  DocumentRecord,
  Milestone,
  OnboardingProgress,
  OnboardingTask,
  PortalView,
  TrainingAssignment,
} from '../models/onboarding';

export function renderWithProviders(ui: ReactElement, initialRoute = '/') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <AuthProvider>{ui}</AuthProvider>
    </MemoryRouter>,
  );
}

export function makeMilestone(overrides: Partial<Milestone> = {}): Milestone {
  return {
    id: 'milestone-1',
    name: 'Documents Verified',
    status: 'Pending',
    targetDate: '2026-11-01',
    completedAtUtc: null,
    sortOrder: 2,
    ...overrides,
  };
}

export function makeTask(overrides: Partial<OnboardingTask> = {}): OnboardingTask {
  return {
    id: 'task-1',
    onboardingRecordId: 'record-1',
    title: 'Sign employment contract',
    description: 'Review and sign the contract.',
    category: 'Documentation',
    assignedToEmail: 'ada.lovelace@contoso.example',
    assigneeRole: 'Employee',
    dueDate: '2026-11-01',
    status: 'Pending',
    isOverdue: false,
    isEscalated: false,
    completedAtUtc: null,
    ...overrides,
  };
}

export function makeDocument(overrides: Partial<DocumentRecord> = {}): DocumentRecord {
  return {
    id: 'doc-1',
    onboardingRecordId: 'record-1',
    documentType: 'RightToWork',
    fileName: 'visa.pdf',
    contentType: 'application/pdf',
    sizeBytes: 2048,
    status: 'Submitted',
    uploadedByEmail: 'ada.lovelace@contoso.example',
    uploadedAtUtc: '2026-10-01T10:00:00Z',
    latestReviewComments: null,
    latestReviewerEmail: null,
    latestReviewedAtUtc: null,
    ...overrides,
  };
}

export function makeTraining(overrides: Partial<TrainingAssignment> = {}): TrainingAssignment {
  return {
    id: 'training-1',
    onboardingRecordId: 'record-1',
    courseCode: 'ORI-101',
    courseName: 'Company Orientation',
    isMandatory: true,
    contentUrl: null,
    status: 'NotStarted',
    scorePercentage: null,
    dueDate: '2026-11-05',
    completedAtUtc: null,
    ...overrides,
  };
}

export function makeProgress(overrides: Partial<OnboardingProgress> = {}): OnboardingProgress {
  return {
    onboardingRecordId: 'record-1',
    employeeName: 'Ada Lovelace',
    status: 'InProgress',
    startDate: '2026-11-03',
    completionPercentage: 40,
    totalTasks: 8,
    completedTasks: 3,
    overdueTasks: 2,
    pendingDocuments: 1,
    approvedDocuments: 2,
    rejectedDocuments: 1,
    pendingProvisioning: 2,
    completedProvisioning: 3,
    completedTraining: 1,
    totalTraining: 4,
    milestones: [makeMilestone()],
    ...overrides,
  };
}

export function makePortal(overrides: Partial<PortalView> = {}): PortalView {
  return {
    record: {
      id: 'record-1',
      candidateEmail: 'ada.lovelace@contoso.example',
      firstName: 'Ada',
      lastName: 'Lovelace',
      fullName: 'Ada Lovelace',
      role: 'Software Engineer',
      department: 'Engineering',
      location: 'Remote',
      startDate: '2026-11-03',
      managerEmail: 'grace.hopper@contoso.example',
      offerReference: 'OFFER-10045',
      status: 'InProgress',
      completionPercentage: 40,
      createdAtUtc: '2026-10-01T09:00:00Z',
      updatedAtUtc: '2026-10-02T09:00:00Z',
    },
    progress: makeProgress(),
    myTasks: [makeTask()],
    myDocuments: [makeDocument()],
    myTraining: [makeTraining()],
    requiredDocumentTypes: ['PhotoIdentification', 'RightToWork', 'SignedContract'],
    ...overrides,
  };
}
