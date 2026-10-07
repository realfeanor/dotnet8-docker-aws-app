import React, { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import api, { errorMessage } from '../api/api';
import { useAuth } from './AuthContext';
import PasswordInput from './PasswordInput';
export default function LoginPage({ register = false }) {
  const { session, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  if (session) return <Navigate to="/products" replace />;
  async function submit(event) {
    event.preventDefault(); setError(''); setBusy(true);
    const values = Object.fromEntries(new FormData(event.currentTarget));
    if (register && values.password !== values.confirmPassword) { setError('Passwords do not match.'); setBusy(false); return; }
    delete values.confirmPassword;
    try {
      const { data } = await api.post(register ? 'Auth/register' : 'Auth/login', values);
      login(data.token);
      navigate(location.state?.from || '/products', { replace: true });
    } catch (err) { setError(errorMessage(err)); } finally { setBusy(false); }
  }
  return <main className="auth-page"><section className="auth-card"><span className="eyebrow">STOCKROOM</span><h1>{register ? 'Create an account' : 'Welcome back'}</h1><p className="muted">Explore and manage your product catalog in one place.</p><form onSubmit={submit}>
    {register && <div className="form-grid"><label>First name<input name="firstName" required autoComplete="given-name" /></label><label>Last name<input name="lastName" required autoComplete="family-name" /></label></div>}
    <label>Email<input name="email" type="email" required maxLength={320} autoComplete="email" /></label>
    <PasswordInput label="Password" name="password" autoComplete={register ? 'new-password' : 'current-password'} />
    {register && <PasswordInput label="Confirm password" name="confirmPassword" autoComplete="new-password" />}
    {error && <p role="alert" className="error">{error}</p>}<button disabled={busy}>{busy ? 'Please wait…' : register ? 'Create account' : 'Sign in'}</button>
  </form><p>{register ? 'Already have an account?' : "Don't have an account?"} <Link to={register ? '/login' : '/register'}>{register ? 'Sign in' : 'Create account'}</Link></p>{register && <p className="muted">New accounts can view products and categories.</p>}</section></main>;
}
