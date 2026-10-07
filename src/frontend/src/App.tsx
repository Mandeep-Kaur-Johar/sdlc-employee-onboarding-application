import { NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import { DashboardPage } from './pages/DashboardPage';
import { DocumentsPage } from './pages/DocumentsPage';
import { OnboardingDetailPage } from './pages/OnboardingDetailPage';
import { OrientationPage } from './pages/OrientationPage';
import { PortalPage } from './pages/PortalPage';
import { ProvisioningPage } from './pages/ProvisioningPage';
import { SignInPage } from './pages/SignInPage';
import { TasksPage } from './pages/TasksPage';
import './styles.css';

export default function App() {
  const { user, signOut, hasRole } = useAuth();
  const isHr = hasRole('HRCoordinator', 'HRSpecialist', 'Manager', 'ITAdmin');

  return (
    <div className="app">
      <a className="skip-link" href="#main">
        Skip to main content
      </a>

      <header className="app__header">
        <span className="app__brand">Employee Onboarding</span>

        <nav aria-label="Primary">
          <ul className="app__nav">
            {isHr ? (
              <>
                <li>
                  <NavLink to="/dashboard">Dashboard</NavLink>
                </li>
                <li>
                  <NavLink to="/tasks">Tasks</NavLink>
                </li>
                <li>
                  <NavLink to="/documents">Document review</NavLink>
                </li>
                <li>
                  <NavLink to="/provisioning">Provisioning</NavLink>
                </li>
              </>
            ) : null}
            {user ? (
              <>
                <li>
                  <NavLink to="/portal">My portal</NavLink>
                </li>
                <li>
                  <NavLink to="/orientation">Orientation</NavLink>
                </li>
              </>
            ) : null}
          </ul>
        </nav>

        <div className="app__identity">
          {user ? (
            <>
              <span>{user.displayName}</span>
              <button type="button" onClick={signOut}>
                Sign out
              </button>
            </>
          ) : (
            <NavLink to="/signin">Sign in</NavLink>
          )}
        </div>
      </header>

      <main id="main" className="app__main">
        <Routes>
          <Route path="/" element={<Navigate to={user ? (isHr ? '/dashboard' : '/portal') : '/signin'} replace />} />
          <Route path="/signin" element={<SignInPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/onboarding/:id" element={<OnboardingDetailPage />} />
          <Route path="/tasks" element={<TasksPage />} />
          <Route path="/documents" element={<DocumentsPage />} />
          <Route path="/provisioning" element={<ProvisioningPage />} />
          <Route path="/portal" element={<PortalPage />} />
          <Route path="/orientation" element={<OrientationPage />} />
          <Route path="*" element={<p role="status">The requested page was not found.</p>} />
        </Routes>
      </main>
    </div>
  );
}
