export type OnboardingStatus = 'Initiated' | 'InProgress' | 'ReadyForDayOne' | 'Completed';

export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Blocked';

export type DocumentStatus = 'Submitted' | 'Approved' | 'Rejected';

export type ProvisioningStatus = 'Requested' | 'InProgress' | 'Completed' | 'Failed';

export type TrainingStatus = 'NotStarted' | 'InProgress' | 'Completed';

export type AppRole = 'Employee' | 'HRCoordinator' | 'HRSpecialist' | 'ITAdmin' | 'Manager';

export interface OnboardingRecord {
  id: string;
  candidateEmail: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  department: string;
  location: string;
  startDate: string;
  managerEmail?: string | null;
  offerReference?: string | null;
  status: OnboardingStatus;
  completionPercentage: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Milestone {
  id: string;
  name: string;
  status: string;
  targetDate: string;
  completedAtUtc?: string | null;
  sortOrder: number;
}

export interface OnboardingProgress {
  onboardingRecordId: string;
  employeeName: string;
  status: OnboardingStatus;
  startDate: string;
  completionPercentage: number;
  totalTasks: number;
  completedTasks: number;
  overdueTasks: number;
  pendingDocuments: number;
  approvedDocuments: number;
  rejectedDocuments: number;
  pendingProvisioning: number;
  completedProvisioning: number;
  completedTraining: number;
  totalTraining: number;
  milestones: Milestone[];
}

export interface OnboardingTask {
  id: string;
  onboardingRecordId: string;
  title: string;
  description?: string | null;
  category: string;
  assignedToEmail: string;
  assigneeRole: AppRole;
  dueDate: string;
  status: TaskStatus;
  isOverdue: boolean;
  isEscalated: boolean;
  completedAtUtc?: string | null;
}

export interface DocumentRecord {
  id: string;
  onboardingRecordId: string;
  documentType: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: DocumentStatus;
  uploadedByEmail: string;
  uploadedAtUtc: string;
  latestReviewComments?: string | null;
  latestReviewerEmail?: string | null;
  latestReviewedAtUtc?: string | null;
}

export interface DocumentCompliance {
  onboardingRecordId: string;
  requiredCount: number;
  submittedCount: number;
  approvedCount: number;
  rejectedCount: number;
  completionPercentage: number;
  missingDocumentTypes: string[];
  documents: DocumentRecord[];
}

export interface ProvisioningRequest {
  id: string;
  onboardingRecordId: string;
  itemType: string;
  systemName: string;
  details?: string | null;
  externalTicketId?: string | null;
  status: ProvisioningStatus;
  requiredByDate: string;
  createdAtUtc: string;
  completedAtUtc?: string | null;
  updatedBySystem?: string | null;
}

export interface ProvisioningSummary {
  onboardingRecordId: string;
  totalRequests: number;
  completedRequests: number;
  failedRequests: number;
  completionPercentage: number;
  isDayOneReady: boolean;
  requests: ProvisioningRequest[];
}

export interface TrainingAssignment {
  id: string;
  onboardingRecordId: string;
  courseCode: string;
  courseName: string;
  isMandatory: boolean;
  contentUrl?: string | null;
  status: TrainingStatus;
  scorePercentage?: number | null;
  dueDate: string;
  completedAtUtc?: string | null;
}

export interface OnboardingDetail {
  record: OnboardingRecord;
  progress: OnboardingProgress;
  tasks: OnboardingTask[];
  documents: DocumentRecord[];
  provisioningRequests: ProvisioningRequest[];
  trainingAssignments: TrainingAssignment[];
}

export interface PortalView {
  record: OnboardingRecord;
  progress: OnboardingProgress;
  myTasks: OnboardingTask[];
  myDocuments: DocumentRecord[];
  myTraining: TrainingAssignment[];
  requiredDocumentTypes: string[];
}

export interface AppNotification {
  id: string;
  recipient: string;
  channel: string;
  kind: string;
  subject: string;
  body: string;
  sentAtUtc: string;
}

export interface ReminderRunResult {
  remindersSent: number;
  escalationsSent: number;
  executedAtUtc: string;
  notifications: AppNotification[];
}
