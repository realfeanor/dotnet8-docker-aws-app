import React from 'react';
import { BrowserRouter, Routes, Route, NavLink, Navigate, Outlet, Link } from 'react-router-dom';
import { AuthProvider, useAuth } from './components/AuthContext';
import PrivateRoute from './components/PrivateRoute';
import LoginPage from './components/LoginPage';
import Catalog from './components/Catalog';
import './App.css';
function Layout() {
  const { session, can, logout } = useAuth();

  return (
    <div className="app-shell">
      <aside>
        <Link className="brand" to="/products">Stockroom<span>Product management</span></Link>
        <nav aria-label="Main navigation">
          {can('Product.Get') && <NavLink to="/products">Products</NavLink>}
          {can('Category.Get') && <NavLink to="/categories">Categories</NavLink>}
        </nav>
        <div className="account">
          <strong>{session.name}</strong>
          <small>{session.email}</small>
          <span className="badge">{session.roles.includes('Admin') ? 'Admin' : 'User'}</span>
          <button className="secondary" onClick={logout}>Sign out</button>
        </div>
      </aside>
      <main className="content"><Outlet /></main>
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<LoginPage register />} />
          <Route element={<PrivateRoute />}>
            <Route element={<Layout />}>
              <Route element={<PrivateRoute permission="Product.Get" />}>
                <Route path="/products" element={<Catalog key="products" />} />
              </Route>
              <Route element={<PrivateRoute permission="Category.Get" />}>
                <Route path="/categories" element={<Catalog key="categories" categoriesOnly />} />
              </Route>
              <Route path="/forbidden" element={<section className="panel"><h1>Access denied</h1><p>You do not have permission to view this page. Choose an available page from the navigation.</p></section>} />
              <Route path="*" element={<section className="panel"><h1>Page not found</h1><Link to="/products">Back to products</Link></section>} />
            </Route>
          </Route>
          <Route path="/" element={<Navigate to="/products" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
