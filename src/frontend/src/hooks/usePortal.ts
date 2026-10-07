import { portalApi } from '../services/onboardingApi';
import { useAsync } from './useAsync';
import type { PortalView } from '../models/onboarding';

/** Loads the personalized onboarding portal (User Story 2800). */
export function usePortal(email: string | undefined) {
  return useAsync<PortalView>(() => portalApi.getMyPortal(email as string), [email], Boolean(email));
}
