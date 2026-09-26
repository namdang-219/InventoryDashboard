import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { config } from '../../config/environments.js';
import { getAuthToken } from '../../helpers/auth.js';

// Custom metrics for Dashboard Bundle
const dashboardDuration = new Trend('dashboard_bundle_duration_ms', true);
const dashboardSuccessRate = new Rate('dashboard_bundle_success_rate');
const dashboardTotalRequests = new Counter('dashboard_bundle_total_requests');

// Execution profiles (Constant Arrival Rate / Requests per second)
const profiles = {
  smoke: {
    executor: 'constant-arrival-rate',
    rate: Number(__ENV.RATE) || 100, // 100 req/s
    timeUnit: '1s',
    duration: __ENV.DURATION || '5m', // 5 minutes
    preAllocatedVUs: 50,
    maxVUs: 200,
  },
  load: {
    executor: 'constant-arrival-rate',
    rate: Number(__ENV.RATE) || 150, // 150 req/s
    timeUnit: '1s',
    duration: __ENV.DURATION || '5m', // 5 minutes
    preAllocatedVUs: 75,
    maxVUs: 300,
  },
  stress: {
    executor: 'constant-arrival-rate',
    rate: Number(__ENV.RATE) || 250, // 250 req/s
    timeUnit: '1s',
    duration: __ENV.DURATION || '5m', // 5 minutes
    preAllocatedVUs: 100,
    maxVUs: 500,
  },
};

const selectedProfile = __ENV.PROFILE || 'load';

export const options = {
  scenarios: {
    dashboard_bundle: profiles[selectedProfile] || profiles.load,
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
 * Default VU execution loop (1 iteration = 1 request)
 * Alternates between global dashboard and dealership-filtered dashboard.
 */
export default function (data) {
  const headers = {
    'Authorization': `Bearer ${data.token}`,
    'Accept': 'application/json',
  };

  // 50% request Global dashboard, 50% request Filtered dashboard
  const hasDealerships = data.dealershipIds && data.dealershipIds.length > 0;
  const isFiltered = hasDealerships && Math.random() < 0.5;

  const url = isFiltered
    ? `${config.baseUrl}/api/v1/dashboard?dealershipId=${data.dealershipIds[Math.floor(Math.random() * data.dealershipIds.length)]}&page=1&pageSize=20`
    : `${config.baseUrl}/api/v1/dashboard?page=1&pageSize=20`;

  const tag = isFiltered ? 'dashboard_filtered' : 'dashboard_global';
  const res = http.get(url, { headers, tags: { name: tag } });

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
}
