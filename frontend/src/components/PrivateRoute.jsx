import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
export default function PrivateRoute({ permission }) {
  const { session, can } = useAuth();
  const location = useLocation();
  if (!session) return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  if (permission && !can(permission)) return <Navigate to="/forbidden" replace />;
  return <Outlet />;
}
