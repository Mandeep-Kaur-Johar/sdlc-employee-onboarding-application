import { apiClient } from './apiClient';
import type {
  DocumentCompliance,
  DocumentRecord,
  OnboardingDetail,
  OnboardingProgress,
  OnboardingRecord,
  OnboardingTask,
  PortalView,
  ProvisioningRequest,
  ProvisioningSummary,
  ReminderRunResult,
  TrainingAssignment,
} from '../models/onboarding';

/** Onboarding lifecycle (User Story 2788, 2789). */
export const onboardingApi = {
  async list(status?: string, department?: string): Promise<OnboardingRecord[]> {
    const { data } = await apiClient.get<OnboardingRecord[]>('/onboarding', {
      params: { status: status || undefined, department: department || undefined },
    });
    return data;
  },

  async getDetail(id: string): Promise<OnboardingDetail> {
    const { data } = await apiClient.get<OnboardingDetail>(`/onboarding/${id}`);
    return data;
  },

  async getProgress(id: string): Promise<OnboardingProgress> {
    const { data } = await apiClient.get<OnboardingProgress>(`/onboarding/${id}/progress`);
    return data;
  },

  async createFromAcceptedOffer(payload: {
    candidateEmail: string;
    firstName: string;
    lastName: string;
    role: string;
    department: string;
    location: string;
    startDate: string;
    managerEmail?: string;
    offerReference?: string;
  }): Promise<OnboardingRecord> {
    const { data } = await apiClient.post<OnboardingRecord>('/onboarding/offer-accepted', payload);
    return data;
  },
};

/** Document collection and compliance (User Story 2791, 2792). */
export const documentsApi = {
  async getRequiredTypes(): Promise<string[]> {
    const { data } = await apiClient.get<string[]>('/documents/required-types');
    return data;
  },

  async getCompliance(onboardingRecordId: string): Promise<DocumentCompliance> {
    const { data } = await apiClient.get<DocumentCompliance>(`/documents/onboarding/${onboardingRecordId}`);
    return data;
  },

  async upload(params: {
    onboardingRecordId: string;
    documentType: string;
    uploadedByEmail: string;
    file: File;
  }): Promise<DocumentRecord> {
    const form = new FormData();
    form.append('onboardingRecordId', params.onboardingRecordId);
    form.append('documentType', params.documentType);
    form.append('uploadedByEmail', params.uploadedByEmail);
    form.append('file', params.file);

    const { data } = await apiClient.post<DocumentRecord>('/documents/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return data;
  },

  async getPendingReview(): Promise<DocumentRecord[]> {
    const { data } = await apiClient.get<DocumentRecord[]>('/documents/pending-review');
    return data;
  },

  async review(
    documentId: string,
    payload: { decision: 'Approved' | 'Rejected'; comments?: string; reviewerEmail: string },
  ): Promise<DocumentRecord> {
    const { data } = await apiClient.post<DocumentRecord>(`/documents/${documentId}/review`, payload);
    return data;
  },
};

/** Task automation and notifications (User Story 2794, 2795). */
export const tasksApi = {
  async list(params: {
    onboardingRecordId?: string;
    assignedToEmail?: string;
    status?: string;
    overdueOnly?: boolean;
  }): Promise<OnboardingTask[]> {
    const { data } = await apiClient.get<OnboardingTask[]>('/tasks', { params });
    return data;
  },

  async generate(onboardingRecordId: string): Promise<OnboardingTask[]> {
    const { data } = await apiClient.post<OnboardingTask[]>(`/tasks/generate/${onboardingRecordId}`);
    return data;
  },

  async updateStatus(taskId: string, status: string): Promise<OnboardingTask> {
    const { data } = await apiClient.put<OnboardingTask>(`/tasks/${taskId}/status`, { status });
    return data;
  },
};

export const notificationsApi = {
  async runReminders(): Promise<ReminderRunResult> {
    const { data } = await apiClient.post<ReminderRunResult>('/notifications/run-reminders');
    return data;
  },
};

/** IT provisioning (User Story 2797, 2798). */
export const provisioningApi = {
  async getSummary(onboardingRecordId: string): Promise<ProvisioningSummary> {
    const { data } = await apiClient.get<ProvisioningSummary>(`/provisioning/onboarding/${onboardingRecordId}`);
    return data;
  },

  async listOpen(): Promise<ProvisioningRequest[]> {
    const { data } = await apiClient.get<ProvisioningRequest[]>('/provisioning/open');
    return data;
  },

  async createDefaults(onboardingRecordId: string): Promise<ProvisioningRequest[]> {
    const { data } = await apiClient.post<ProvisioningRequest[]>(`/provisioning/requests/${onboardingRecordId}`);
    return data;
  },

  async updateStatus(
    requestId: string,
    payload: { status: string; externalTicketId?: string; updatedBySystem?: string },
  ): Promise<ProvisioningRequest> {
    const { data } = await apiClient.put<ProvisioningRequest>(`/provisioning/${requestId}/status`, payload);
    return data;
  },
};

/** Orientation and training (User Story 2801). */
export const trainingApi = {
  async getForOnboarding(onboardingRecordId: string): Promise<TrainingAssignment[]> {
    const { data } = await apiClient.get<TrainingAssignment[]>(`/training/onboarding/${onboardingRecordId}`);
    return data;
  },

  async start(assignmentId: string): Promise<TrainingAssignment> {
    const { data } = await apiClient.put<TrainingAssignment>(`/training/${assignmentId}/start`);
    return data;
  },

  async complete(assignmentId: string, scorePercentage?: number): Promise<TrainingAssignment> {
    const { data } = await apiClient.put<TrainingAssignment>(`/training/${assignmentId}/complete`, {
      scorePercentage: scorePercentage ?? null,
    });
    return data;
  },
};

/** Personalized employee portal (User Story 2800). */
export const portalApi = {
  async getMyPortal(email: string): Promise<PortalView> {
    const { data } = await apiClient.get<PortalView>('/portal/me', { params: { email } });
    return data;
  },
};
