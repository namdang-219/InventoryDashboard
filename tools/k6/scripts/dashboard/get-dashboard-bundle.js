import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { config } from '../../config/environments.js';
import { getAuthToken } from '../../helpers/auth.js';

// Custom metrics for Dashboard Bundle
const dashboardDuration = new Trend('dashboard_bundle_duration_ms', true);
const dashboardSuccessRate = new Rate('dashboard_bundle_success_rate');
const dashboardTotalRequests = new Counter('dashboard_bundle_total_requests');

// Execution profiles
const profiles = {
  smoke: {
    vus: 1,
    duration: '20s',
  },
  load: {
    stages: [
      { duration: '20s', target: 10 }, // Ramp up to 10 VUs
      { duration: '40s', target: 20 }, // Scale to 20 VUs
      { duration: '30s', target: 20 }, // Sustain peak load
      { duration: '15s', target: 0 },  // Ramp down to 0
    ],
  },
  stress: {
    stages: [
      { duration: '30s', target: 25 },
      { duration: '45s', target: 50 },
      { duration: '45s', target: 100 },
      { duration: '30s', target: 0 },
    ],
  },
};

const selectedProfile = __ENV.PROFILE || 'load';

export const options = {
  scenarios: {
    dashboard_bundle: {
      executor: profiles[selectedProfile]?.stages ? 'ramping-vus' : 'constant-vus',
      ...(profiles[selectedProfile]?.stages
        ? { stages: profiles[selectedProfile].stages }
        : { vus: profiles[selectedProfile].vus, duration: profiles[selectedProfile].duration }),
    },
  },
  thresholds: {
    // 95% of requests must complete below 500ms, 99% below 1000ms
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    // HTTP failure rate must stay below 1%
    http_req_failed: ['rate<0.01'],
    // Custom success rate threshold
    dashboard_bundle_success_rate: ['rate>0.99'],
  },
};

/**
 * Setup runs once before test execution:
 * Obtains JWT Bearer token and discovers sample dealership IDs for parameterized filtering.
 */
export function setup() {
  const token = config.authToken || getAuthToken(config.baseUrl, config.admin.email, config.admin.password);

  const headers = {
    'Authorization': `Bearer ${token}`,
    'Accept': 'application/json',
  };

  // Pre-fetch dealerships to parameterize filtering during load test
  let dealershipIds = [];
  try {
    const res = http.get(`${config.baseUrl}/api/v1/dealerships`, { headers });
    if (res.status === 200) {
      const parsed = JSON.parse(res.body);
      const list = parsed.data || parsed;
      if (Array.isArray(list)) {
        dealershipIds = list.map((d) => d.id).filter(Boolean);
      }
    }
  } catch (err) {
    console.warn(`Could not pre-fetch dealerships: ${err.message}`);
  }

  return { token, dealershipIds };
}

/**
 * Default VU execution loop
 */
export default function (data) {
  const headers = {
    'Authorization': `Bearer ${data.token}`,
    'Accept': 'application/json',
  };

  // Scenario 1: Global dashboard (all dealerships, default pagination)
  group('Get Global Dashboard Bundle', () => {
    const url = `${config.baseUrl}/api/v1/dashboard?page=1&pageSize=20`;
    const res = http.get(url, { headers });

    dashboardTotalRequests.add(1);
    dashboardDuration.add(res.timings.duration);

    const isOk = check(res, {
      'status is 200': (r) => r.status === 200,
      'has data payload': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body && body.data !== undefined;
        } catch {
          return false;
        }
      },
      'has summary metrics': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body?.data?.summary && typeof body.data.summary.totalInventory === 'number';
        } catch {
          return false;
        }
      },
      'response time < 500ms': (r) => r.timings.duration < 500,
    });

    dashboardSuccessRate.add(isOk);
  });

  sleep(0.5);

  // Scenario 2: Parameterized dashboard with Dealership filter
  if (data.dealershipIds && data.dealershipIds.length > 0) {
    group('Get Dealership-Filtered Dashboard Bundle', () => {
      const randomDealershipId = data.dealershipIds[Math.floor(Math.random() * data.dealershipIds.length)];
      const url = `${config.baseUrl}/api/v1/dashboard?dealershipId=${randomDealershipId}&page=1&pageSize=20`;
      const res = http.get(url, { headers });

      dashboardTotalRequests.add(1);
      dashboardDuration.add(res.timings.duration);

      const isOk = check(res, {
        'filtered status is 200': (r) => r.status === 200,
        'filtered has data payload': (r) => {
          try {
            const body = JSON.parse(r.body);
            return body && body.data !== undefined;
          } catch {
            return false;
          }
        },
        'filtered response time < 500ms': (r) => r.timings.duration < 500,
      });

      dashboardSuccessRate.add(isOk);
    });
  }

  // Realistic think time between user actions
  sleep(1);
}
