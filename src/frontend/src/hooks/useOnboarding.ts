import { onboardingApi } from '../services/onboardingApi';
import { useAsync } from './useAsync';
import type { OnboardingDetail, OnboardingRecord } from '../models/onboarding';

/** Loads the HR onboarding dashboard list (User Story 2789). */
export function useOnboardingList(status: string, department: string) {
  return useAsync<OnboardingRecord[]>(
    () => onboardingApi.list(status, department),
    [status, department],
  );
}

/** Loads a single onboarding record with full detail (User Story 2789). */
export function useOnboardingDetail(id: string | undefined) {
  return useAsync<OnboardingDetail>(
    () => onboardingApi.getDetail(id as string),
    [id],
    Boolean(id),
  );
}
