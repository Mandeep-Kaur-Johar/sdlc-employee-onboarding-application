import { describe, expect, it, beforeEach } from 'vitest';
import { apiClient, toErrorMessage, TOKEN_STORAGE_KEY } from '../services/apiClient';

beforeEach(() => {
  localStorage.clear();
});

describe('apiClient', () => {
  it('attaches the bearer token when one is stored', () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'abc.def.ghi');

    const handler = apiClient.interceptors.request as unknown as {
      handlers: Array<{ fulfilled: (config: { headers: Record<string, string> }) => { headers: Record<string, string> } }>;
    };

    const config = handler.handlers[0].fulfilled({ headers: {} });
    expect(config.headers.Authorization).toBe('Bearer abc.def.ghi');
  });

  it('omits the Authorization header when no token is stored', () => {
    const handler = apiClient.interceptors.request as unknown as {
      handlers: Array<{ fulfilled: (config: { headers: Record<string, string> }) => { headers: Record<string, string> } }>;
    };

    const config = handler.handlers[0].fulfilled({ headers: {} });
    expect(config.headers.Authorization).toBeUndefined();
  });
});

describe('toErrorMessage', () => {
  it('reads problem details from an axios error', () => {
    const error = {
      isAxiosError: true,
      message: 'Request failed',
      response: { data: { detail: 'The uploaded file exceeds the maximum size of 10 MB.' } },
    };

    expect(toErrorMessage(error)).toBe('The uploaded file exceeds the maximum size of 10 MB.');
  });

  it('falls back to the Error message', () => {
    expect(toErrorMessage(new Error('boom'))).toBe('boom');
  });

  it('falls back to a generic message for unknown values', () => {
    expect(toErrorMessage('oops')).toBe('An unexpected error occurred.');
  });
});
