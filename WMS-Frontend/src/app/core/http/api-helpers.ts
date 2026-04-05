import { HttpErrorResponse, HttpParams } from '@angular/common/http';
import { ApiErrorResponse, ApiErrorState } from '../models/api.types';

type QueryValue = string | number | boolean | null | undefined;

export function buildHttpParams<T extends object>(values: T): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(values as Record<string, QueryValue>)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    params = params.set(key, String(value));
  }

  return params;
}

export function toApiErrorState(error: unknown): ApiErrorState {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return {
        status: error.status,
        message: 'The API could not be reached. Make sure the backend is running.',
        errors: {},
      };
    }

    const apiError = isApiErrorResponse(error.error) ? error.error : undefined;

    return {
      status: error.status,
      code: apiError?.code,
      traceId: apiError?.traceId,
      message: apiError?.message ?? error.message ?? 'The request failed.',
      errors: normalizeErrors(apiError?.errors),
    };
  }

  return {
    message: 'Something went wrong while processing the request.',
    errors: {},
  };
}

export function getFieldErrors(
  error: ApiErrorState | null,
  ...fieldNames: readonly string[]
): readonly string[] {
  if (!error) {
    return [];
  }

  const messages = fieldNames.flatMap((fieldName) => error.errors[fieldName] ?? []);
  return Array.from(new Set(messages));
}

function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  return typeof value === 'object' && value !== null;
}

function normalizeErrors(
  errors: Record<string, readonly string[]> | undefined,
): Record<string, readonly string[]> {
  if (!errors) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(errors).map(([key, value]) => [key, Array.isArray(value) ? value : []]),
  );
}
