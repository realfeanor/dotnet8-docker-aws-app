import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, expect, test, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider, hasPermission, readSession } from './components/AuthContext';
import PrivateRoute from './components/PrivateRoute';
import LoginPage from './components/LoginPage';
import Catalog from './components/Catalog';
import constants from './constants/constants';
import api from './api/api';
vi.mock('./api/api', () => ({ __esModule: true, default: { get: vi.fn(), post: vi.fn() }, errorMessage: () => 'Request failed' }));
const roleKey = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const token = (roles, exp = Math.floor(Date.now() / 1000) + 3600) =>
  `header.${btoa(JSON.stringify({ exp, email: 'demo@example.com', [roleKey]: roles }))}.signature`;

beforeEach(() => {
  localStorage.clear();
  vi.clearAllMocks();
});

test('registration password visibility toggles independently without submitting', () => {
  render(<AuthProvider><MemoryRouter><LoginPage register /></MemoryRouter></AuthProvider>);
  const password = screen.getByLabelText('Password');
  const confirmation = screen.getByLabelText('Confirm password');
  fireEvent.change(password, { target: { value: 'typed-password' } });
  fireEvent.click(screen.getByRole('button', { name: 'Show password' }));
  expect(password).toHaveAttribute('type', 'text');
  expect(password).toHaveValue('typed-password');
  expect(confirmation).toHaveAttribute('type', 'password');
  fireEvent.click(screen.getByRole('button', { name: 'Hide password' }));
  expect(password).toHaveAttribute('type', 'password');
  expect(api.post).not.toHaveBeenCalled();
});

test('reads a single .NET role claim and rejects malformed or expired sessions', () => {
  expect(readSession(token('Admin')).roles).toEqual(['Admin']);
  expect(readSession(token([], 1))).toBeNull();
  expect(readSession('invalid')).toBeNull();
});

test('new user read permissions do not grant management permissions', () => {
  const session = readSession(token(['Product.Get', 'Category.Get']));
  expect(hasPermission(session, 'Product.Get')).toBe(true);
  expect(hasPermission(session, 'Product.Add')).toBe(false);
  expect(hasPermission(session, 'Category.Delete')).toBe(false);
  expect(hasPermission(readSession(token('Admin')), 'Product.Delete')).toBe(true);
});

test('unauthenticated direct navigation redirects to login', async () => {
  render(<AuthProvider><MemoryRouter initialEntries={['/products']}><Routes><Route element={<PrivateRoute permission="Product.Get" />}><Route path="/products" element={<p>Protected catalog</p>} /></Route><Route path="/login" element={<LoginPage />} /></Routes></MemoryRouter></AuthProvider>);
  expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
  expect(screen.queryByText('Protected catalog')).not.toBeInTheDocument();
});

test('a read-only user cannot open a management route directly', async () => {
  localStorage.setItem(constants.authTokenKey, token(['Product.Get']));
  render(<AuthProvider><MemoryRouter initialEntries={['/admin']}><Routes><Route element={<PrivateRoute permission="Product.Add" />}><Route path="/admin" element={<p>Management</p>} /></Route><Route path="/forbidden" element={<p>Access denied</p>} /></Routes></MemoryRouter></AuthProvider>);
  expect(await screen.findByText('Access denied')).toBeInTheDocument();
});

test.each([['User', ['Product.Get', 'Category.Get'], false], ['Admin', ['Admin'], true]])('%s catalog shows only permitted controls', async (_, roles, manage) => {
  localStorage.setItem(constants.authTokenKey, token(roles));
  api.get.mockResolvedValue({ data: [{ id: 1, productName: 'Coffee', categoryId: 2, unitPrice: 12, unitsInStock: 3 }] });
  api.post.mockResolvedValue({ data: { data: [{ id: 2, categoryName: 'Drinks' }] } });
  render(<AuthProvider><Catalog /></AuthProvider>);
  expect(await screen.findByText('Coffee')).toBeInTheDocument();
  expect(Boolean(screen.queryByRole('button', { name: /Add product/ }))).toBe(manage);
  expect(Boolean(screen.queryByRole('button', { name: 'Edit' }))).toBe(manage);
  expect(Boolean(screen.queryByRole('button', { name: 'Delete' }))).toBe(manage);
});

test.each([
  ['product', false, 'Coffee', /Add product/, 'Add product'],
  ['category', true, 'Drinks', /Add category/, 'Add category'],
])('%s editor opens over its catalog and Escape closes it', async (_, categoriesOnly, itemName, buttonName, dialogName) => {
  localStorage.setItem(constants.authTokenKey, token(['Admin']));
  api.get.mockResolvedValue({ data: [{ id: 1, productName: 'Coffee', categoryId: 2, unitPrice: 12, unitsInStock: 3 }] });
  api.post.mockResolvedValue({ data: { data: [{ id: 2, categoryName: 'Drinks' }] } });
  render(<AuthProvider><Catalog categoriesOnly={categoriesOnly} /></AuthProvider>);
  expect(await screen.findByText(itemName)).toBeInTheDocument();

  fireEvent.click(screen.getByRole('button', { name: buttonName }));

  expect(screen.getByRole('dialog', { name: dialogName })).toBeInTheDocument();
  expect(screen.getByText(itemName)).toBeInTheDocument();
  fireEvent.keyDown(document, { key: 'Escape' });
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
