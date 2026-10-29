export function extractErrorMessage(err: unknown, fallback = 'An unexpected error occurred'): string {
  if (!err || typeof err !== 'object') return fallback;

  const anyErr = err as Record<string, unknown>;

  // Connection refused / Network error (status 0)
  if (anyErr['status'] === 0) {
    return 'Unable to reach backend API. Please verify server is running on http://localhost:8080.';
  }

  // Backend error body
  const errorBody = anyErr['error'] as Record<string, unknown> | string | undefined;

  if (typeof errorBody === 'string' && errorBody.trim().length > 0) {
    return errorBody;
  }

  if (errorBody && typeof errorBody === 'object') {
    // FastEndpoints / ASP.NET errors dictionary: { errors: { field: ["msg1", "msg2"] } }
    if (errorBody['errors'] && typeof errorBody['errors'] === 'object') {
      const messages: string[] = [];
      for (const val of Object.values(errorBody['errors'] as Record<string, unknown>)) {
        if (Array.isArray(val)) {
          messages.push(...val.filter(m => typeof m === 'string'));
        } else if (typeof val === 'string') {
          messages.push(val);
        }
      }
      if (messages.length > 0) {
        return messages.join('. ');
      }
    }

    if (typeof errorBody['message'] === 'string' && errorBody['message'].trim().length > 0) {
      return errorBody['message'];
    }

    if (typeof errorBody['title'] === 'string' && errorBody['title'].trim().length > 0) {
      return errorBody['title'];
    }
  }

  // HTTP status text
  if (typeof anyErr['status'] === 'number' && typeof anyErr['statusText'] === 'string') {
    return `Server returned ${anyErr['status']} ${anyErr['statusText']}`;
  }

  if (typeof anyErr['message'] === 'string' && anyErr['message'].trim().length > 0) {
    return anyErr['message'];
  }

  return fallback;
}
