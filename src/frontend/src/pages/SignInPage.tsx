import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { AppRole } from '../models/onboarding';

const ROLES: AppRole[] = ['Employee', 'HRCoordinator', 'HRSpecialist', 'ITAdmin', 'Manager'];

/**
 * Sign-in screen. The production deployment redirects to Microsoft Entra ID;
 * this screen stores the issued bearer token and the signed-in identity so the
 * portal can scope data to the current user (User Story 2800).
 */
export function SignInPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [role, setRole] = useState<AppRole>('Employee');
  const [token, setToken] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setValidationError(null);

    if (!email.includes('@')) {
      setValidationError('Enter a valid work email address.');
      return;
    }

    if (token.trim().length === 0) {
      setValidationError('Paste the bearer token issued by the identity provider.');
      return;
    }

    signIn({ email: email.trim(), displayName: displayName.trim() || email.trim(), roles: [role] }, token.trim());
    navigate(role === 'Employee' ? '/portal' : '/dashboard');
  };

  return (
    <section aria-labelledby="signin-heading">
      <h1 id="signin-heading">Sign in</h1>

      <form className="form" onSubmit={handleSubmit} aria-label="Sign in">
        <div className="form__field">
          <label htmlFor="signin-email">Work email</label>
          <input id="signin-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>

        <div className="form__field">
          <label htmlFor="signin-name">Display name</label>
          <input id="signin-name" type="text" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </div>

        <div className="form__field">
          <label htmlFor="signin-role">Role</label>
          <select id="signin-role" value={role} onChange={(e) => setRole(e.target.value as AppRole)}>
            {ROLES.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </div>

        <div className="form__field">
          <label htmlFor="signin-token">Bearer token</label>
          <input id="signin-token" type="password" value={token} onChange={(e) => setToken(e.target.value)} />
        </div>

        {validationError ? (
          <p role="alert" className="status status--error">
            {validationError}
          </p>
        ) : null}

        <button type="submit">Sign in</button>
      </form>
    </section>
  );
}
