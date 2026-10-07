import axios, { AxiosError } from 'axios';

/**
 * Shared axios instance. The JWT bearer token is attached by a request
 * interceptor so no component needs to handle authentication headers
 * (User Story 2800).
 */
export const TOKEN_STORAGE_KEY = 'onboarding.accessToken';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

interface ProblemDetails {
  title?: string;
  detail?: string;
  message?: string;
}

/** Converts an unknown error into a user-presentable message. */
export function toErrorMessage(error: unknown): string {
  const axiosError = error as AxiosError<ProblemDetails>;
  if (axiosError?.isAxiosError) {
    const data = axiosError.response?.data;
    return data?.detail ?? data?.message ?? data?.title ?? axiosError.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred.';
}
