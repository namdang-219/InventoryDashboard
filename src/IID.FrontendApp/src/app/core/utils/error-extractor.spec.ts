import { describe, expect, it } from 'vitest';
import { HttpError } from '../services/api.service';
import { extractErrorMessage } from './error-extractor';

describe('extractErrorMessage', () => {
  it('returns network unreachable message when status is 0', () => {
    const err = new HttpError({
      status: 0,
      statusText: '',
      error: null,
      message: 'Http failure response: 0'
    });

    expect(extractErrorMessage(err)).toBe(
      'Unable to reach backend API. Please verify server is running on http://localhost:8080.'
    );
  });

  it('returns plain string error body when non-empty', () => {
    const err = { status: 400, statusText: 'Bad Request', error: 'VIN format is invalid' };
    expect(extractErrorMessage(err)).toBe('VIN format is invalid');
  });

  it('flattens FastEndpoints / ASP.NET errors dictionary', () => {
    const err = {
      status: 400,
      statusText: 'Bad Request',
      error: {
        errors: {
          vin: ['VIN is required', 'VIN must be 17 characters'],
          price: 'Price must be greater than zero'
        }
      }
    };

    expect(extractErrorMessage(err)).toBe(
      'VIN is required. VIN must be 17 characters. Price must be greater than zero'
    );
  });

  it('extracts message from object error body', () => {
    const err = {
      status: 409,
      statusText: 'Conflict',
      error: { message: 'Dealership code already exists' }
    };

    expect(extractErrorMessage(err)).toBe('Dealership code already exists');
  });

  it('extracts title from ProblemDetails object error body when message is absent', () => {
    const err = {
      status: 422,
      statusText: 'Unprocessable Entity',
      error: { title: 'One or more validation errors occurred.' }
    };

    expect(extractErrorMessage(err)).toBe('One or more validation errors occurred.');
  });

  it('formats HTTP status and statusText when error body has no details', () => {
    const err = {
      status: 503,
      statusText: 'Service Unavailable',
      error: null
    };

    expect(extractErrorMessage(err)).toBe('Server returned 503 Service Unavailable');
  });

  it('uses top-level message on plain Error objects and falls back otherwise', () => {
    expect(extractErrorMessage(new Error('Unexpected failure'))).toBe('Unexpected failure');
    expect(extractErrorMessage(null)).toBe('An unexpected error occurred');
    expect(extractErrorMessage(undefined, 'Custom fallback')).toBe('Custom fallback');
    expect(extractErrorMessage('primitive', 'Custom fallback')).toBe('Custom fallback');
    expect(extractErrorMessage({}, 'Custom fallback')).toBe('Custom fallback');
  });
});
