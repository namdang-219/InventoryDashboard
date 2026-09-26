// Environment configuration for k6 load tests
export const config = {
  baseUrl: __ENV.BASE_URL || 'http://localhost:8080',
  admin: {
    email: __ENV.ADMIN_EMAIL || 'admin@iid.local',
    password: __ENV.ADMIN_PASSWORD || 'P@ssw0rd!Admin',
  },
  saler: {
    email: __ENV.SALER_EMAIL || 'saler@iid.local',
    password: __ENV.SALER_PASSWORD || 'P@ssw0rd!Saler',
  },
  authToken: __ENV.AUTH_TOKEN || null,
};
