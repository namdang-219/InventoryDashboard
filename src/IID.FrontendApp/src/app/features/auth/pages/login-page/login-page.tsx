import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../../../core/services/auth.service';
import { useDealerships } from '../../../../core/services/dealership.service';
import { toastService } from '../../../../core/services/toast.service';
import './login-page.scss';

const EMAIL_REGEX =
  /^(?=.{1,254}$)(?=.{1,64}@)[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;

export function LoginPageComponent() {
  const { login } = useAuth();
  const { loadDealerships } = useDealerships();
  const navigate = useNavigate();

  const [email, setEmail] = useState('admin@iid.local');
  const [password, setPassword] = useState('P@ssw0rd!Admin');
  const [emailTouched, setEmailTouched] = useState(false);
  const [passwordTouched, setPasswordTouched] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const isEmailInvalid = email.length === 0 || !EMAIL_REGEX.test(email);
  const isPasswordInvalid = password.length < 4;
  const isFormInvalid = isEmailInvalid || isPasswordInvalid;

  const fillDemo = (role: 'admin' | 'sales' | 'saler' | 'viewer'): void => {
    if (role === 'admin') {
      setEmail('admin@iid.local');
      setPassword('P@ssw0rd!Admin');
      toastService.info('Quick Fill', 'Loaded Manager account credentials.');
    } else {
      setEmail('saler@iid.local');
      setPassword('P@ssw0rd!Saler');
      toastService.info('Quick Fill', 'Loaded Sales account credentials.');
    }
  };

  const onSubmit = async (e: FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    if (isFormInvalid) {
      setEmailTouched(true);
      setPasswordTouched(true);
      return;
    }

    setIsLoading(true);
    setErrorMessage(null);

    try {
      await login({ email, password });
      setIsLoading(false);
      loadDealerships();
      navigate('/dashboard');
    } catch (err: unknown) {
      setIsLoading(false);
      const apiMessage = (err as { error?: { message?: string } } | null)?.error?.message;
      setErrorMessage(
        apiMessage || 'Invalid credentials or API server unreachable. Please verify API is running.'
      );
    }
  };

  return (
    <div className="iid-login-page login-wrapper">
      {/* Left Showcase Hero */}
      <div className="hero-panel">
        <div className="hero-glow"></div>
        <div className="hero-content">
          <div className="hero-brand">
            <div className="logo-mark">
              <span className="material-symbols-outlined logo-icon">electric_car</span>
            </div>
            <span className="brand-text">IID Enterprise</span>
          </div>

          <div className="hero-headline">
            <h2>Intelligent Inventory Dashboard</h2>
            <p>
              Real-time vehicle lifecycle tracking, automated aging alerts, and demand telemetry powered by
              Event-Driven Architecture.
            </p>
          </div>

          <div className="feature-pills">
            <div className="pill">
              <span className="material-symbols-outlined">bolt</span>
              <span>SignalR Real-Time Sync</span>
            </div>
            <div className="pill">
              <span className="material-symbols-outlined">analytics</span>
              <span>Automated Aging Matrix</span>
            </div>
            <div className="pill">
              <span className="material-symbols-outlined">security</span>
              <span>Optimistic Concurrency</span>
            </div>
          </div>
        </div>
      </div>

      {/* Right Form Card */}
      <div className="form-panel">
        <div className="form-card iid-card">
          <div className="card-header">
            <h3>Sign In</h3>
            <p>Access your intelligent inventory</p>
          </div>

          {/* Quick Demo Credentials Fill */}
          <div className="demo-pills">
            <span className="demo-label">Demo Logins:</span>
            <button type="button" className="demo-btn" onClick={() => fillDemo('admin')}>
              <span className="material-symbols-outlined">admin_panel_settings</span>
              <span>Manager</span>
            </button>
            <button type="button" className="demo-btn" onClick={() => fillDemo('sales')}>
              <span className="material-symbols-outlined">badge</span>
              <span>Sales</span>
            </button>
          </div>

          {errorMessage && (
            <div className="error-banner">
              <span className="material-symbols-outlined">error</span>
              <span>{errorMessage}</span>
            </div>
          )}

          <form onSubmit={onSubmit} className="login-form" noValidate>
            <div className="form-group">
              <label className="iid-form-label" htmlFor="loginEmail">
                Email Address
              </label>
              <input
                id="loginEmail"
                type="email"
                className="iid-input"
                value={email}
                onChange={e => setEmail(e.target.value)}
                onBlur={() => setEmailTouched(true)}
                placeholder="admin@iid.local"
                autoComplete="email"
              />
              {emailTouched && isEmailInvalid && (
                <span className="validation-error">Please enter a valid email address</span>
              )}
            </div>

            <div className="form-group">
              <label className="iid-form-label" htmlFor="loginPassword">
                Password
              </label>
              <input
                id="loginPassword"
                type="password"
                className="iid-input"
                value={password}
                onChange={e => setPassword(e.target.value)}
                onBlur={() => setPasswordTouched(true)}
                placeholder="••••••••"
                autoComplete="current-password"
              />
              {passwordTouched && isPasswordInvalid && (
                <span className="validation-error">Password is required</span>
              )}
            </div>

            <button
              type="submit"
              className="iid-btn iid-btn-primary submit-btn"
              disabled={isLoading || isFormInvalid}
            >
              {isLoading ? (
                <>
                  <span className="material-symbols-outlined spin-icon">progress_activity</span>
                  <span>Signing In...</span>
                </>
              ) : (
                <>
                  <span className="material-symbols-outlined">login</span>
                  <span>Authenticate</span>
                </>
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
