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
    if (register && values.password !== values.confirmPassword) { setError('Şifreler eşleşmiyor.'); setBusy(false); return; }
    delete values.confirmPassword;
    try {
      const { data } = await api.post(register ? 'Auth/register' : 'Auth/login', values);
      login(data.token);
      navigate(location.state?.from || '/products', { replace: true });
    } catch (err) { setError(errorMessage(err)); } finally { setBusy(false); }
  }
  return <main className="auth-page"><section className="auth-card"><span className="eyebrow">STOCKROOM</span><h1>{register ? 'Hesap oluştur' : 'Hoş geldiniz'}</h1><p className="muted">Ürün kataloğunuzu tek bir yerden keşfedin ve yönetin.</p><form onSubmit={submit}>
    {register && <div className="form-grid"><label>Ad<input name="firstName" required autoComplete="given-name" /></label><label>Soyad<input name="lastName" required autoComplete="family-name" /></label></div>}
    <label>E-posta<input name="email" type="email" required maxLength={320} autoComplete="email" /></label>
    <PasswordInput label="Şifre" name="password" autoComplete={register ? 'new-password' : 'current-password'} />
    {register && <PasswordInput label="Şifre tekrar" name="confirmPassword" autoComplete="new-password" />}
    {error && <p role="alert" className="error">{error}</p>}<button disabled={busy}>{busy ? 'Lütfen bekleyin…' : register ? 'Kayıt ol' : 'Giriş yap'}</button>
  </form><p>{register ? 'Zaten hesabınız var mı?' : 'Hesabınız yok mu?'} <Link to={register ? '/login' : '/register'}>{register ? 'Giriş yap' : 'Kayıt ol'}</Link></p>{register && <p className="muted">Yeni hesaplar ürün ve kategori görüntüleme yetkisiyle oluşturulur.</p>}</section></main>;
}
