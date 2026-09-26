import http from 'k6/http';
import { check } from 'k6';

/**
 * Authenticates with the IID API and returns a Bearer JWT token.
 * 
 * @param {string} baseUrl - Base URL of the API (e.g. http://localhost:8080)
 * @param {string} email - User email (default: admin@iid.local)
 * @param {string} password - User password (default: P@ssw0rd!Admin)
 * @returns {string} Bearer JWT token
 */
export function getAuthToken(baseUrl, email, password) {
  const loginUrl = `${baseUrl}/api/v1/auth/login`;
  const payload = JSON.stringify({ email, password });
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
  };

  const response = http.post(loginUrl, payload, params);

  const isSuccess = check(response, {
    'auth login status is 200': (res) => res.status === 200,
    'auth login returns token': (res) => {
      try {
        const body = JSON.parse(res.body);
        return body && !!body.token;
      } catch {
        return false;
      }
    },
  });

  if (!isSuccess) {
    throw new Error(`Authentication failed: [${response.status}] ${response.body}`);
  }

  const json = JSON.parse(response.body);
  return json.token;
}
