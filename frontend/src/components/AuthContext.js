import React, { createContext, useContext, useEffect, useState } from 'react';
import constants from '../constants/constants';

export function readSession(token) {
  try {
    const part = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const claims = JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(part.padEnd(Math.ceil(part.length / 4) * 4, '=')), c => c.charCodeAt(0))));
    if (!Number.isFinite(claims.exp) || claims.exp * 1000 <= Date.now()) return null;
    const roles = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || claims.role || [];
    return { token, expires: claims.exp * 1000, name: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || claims.unique_name || claims.email, email: claims.email, roles: Array.isArray(roles) ? roles : [roles] };
  } catch { return null; }
}
export const hasPermission = (session, permission) => Boolean(session && (session.roles.includes('Admin') || session.roles.includes(permission)));
const AuthContext = createContext(null);
export const useAuth = () => useContext(AuthContext);
export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => readSession(localStorage.getItem(constants.tokenKey2) || ''));
  const logout = () => { localStorage.removeItem(constants.tokenKey2); localStorage.removeItem('isLoggedIn'); setSession(null); };
  const login = token => {
    const next = readSession(token);
    if (!next) throw new Error('Sunucudan geçerli bir oturum alınamadı.');
    localStorage.setItem(constants.tokenKey2, token); setSession(next);
  };
  useEffect(() => {
    if (!session) return;
    const timer = setTimeout(logout, Math.min(session.expires - Date.now(), 2147483647));
    return () => clearTimeout(timer);
  }, [session]);
  useEffect(() => {
    const sync = () => setSession(readSession(localStorage.getItem(constants.tokenKey2) || ''));
    window.addEventListener('storage', sync);
    window.addEventListener('session-expired', logout);
    return () => { window.removeEventListener('storage', sync); window.removeEventListener('session-expired', logout); };
  }, []);
  return <AuthContext.Provider value={{ session, login, logout, can: permission => hasPermission(session, permission) }}>{children}</AuthContext.Provider>;
}
