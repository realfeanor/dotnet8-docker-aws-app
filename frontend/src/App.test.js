import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider, hasPermission, readSession } from './components/AuthContext';
import PrivateRoute from './components/PrivateRoute';
import LoginPage from './components/LoginPage';
import Catalog from './components/Catalog';
import constants from './constants/constants';
import api from './api/api';
jest.mock('./api/api', () => ({ __esModule: true, default: { get: jest.fn(), post: jest.fn() }, errorMessage: () => 'Request failed' }));
const roleKey = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const token = (roles, exp = Math.floor(Date.now()/1000)+3600) => `header.${btoa(JSON.stringify({ exp, email: 'demo@example.com', [roleKey]: roles }))}.signature`;
beforeEach(() => { localStorage.clear(); jest.clearAllMocks(); });
test('registration password visibility toggles independently without submitting', () => {
 render(<AuthProvider><MemoryRouter><LoginPage register /></MemoryRouter></AuthProvider>);
 const password = screen.getByLabelText('Şifre');
 const confirmation = screen.getByLabelText('Şifre tekrar');
 fireEvent.change(password, { target: { value: 'typed-password' } });
 fireEvent.click(screen.getByRole('button', { name: 'Şifre: göster' }));
 expect(password).toHaveAttribute('type', 'text');
 expect(password).toHaveValue('typed-password');
 expect(confirmation).toHaveAttribute('type', 'password');
 fireEvent.click(screen.getByRole('button', { name: 'Şifre: gizle' }));
 expect(password).toHaveAttribute('type', 'password');
 expect(api.post).not.toHaveBeenCalled();
});
test('reads a single .NET role claim and rejects malformed or expired sessions', () => {
 expect(readSession(token('Admin')).roles).toEqual(['Admin']);
 expect(readSession(token([], 1))).toBeNull();
 expect(readSession('invalid')).toBeNull();
});
test('new user read permissions do not grant management permissions', () => {
 const session = readSession(token(['Product.Get','Category.Get']));
 expect(hasPermission(session,'Product.Get')).toBe(true);
 expect(hasPermission(session,'Product.Add')).toBe(false);
 expect(hasPermission(session,'Category.Delete')).toBe(false);
 expect(hasPermission(readSession(token('Admin')),'Product.Delete')).toBe(true);
});
test('unauthenticated direct navigation redirects to login', async () => {
 render(<AuthProvider><MemoryRouter initialEntries={['/products']}><Routes><Route element={<PrivateRoute permission="Product.Get"/>}><Route path="/products" element={<p>Protected catalog</p>}/></Route><Route path="/login" element={<LoginPage/>}/></Routes></MemoryRouter></AuthProvider>);
 expect(await screen.findByRole('heading',{name:'Hoş geldiniz'})).toBeInTheDocument();
 expect(screen.queryByText('Protected catalog')).not.toBeInTheDocument();
});
test('a read-only user cannot open a management route directly', async () => {
 localStorage.setItem(constants.tokenKey2, token(['Product.Get']));
 render(<AuthProvider><MemoryRouter initialEntries={['/admin']}><Routes><Route element={<PrivateRoute permission="Product.Add"/>}><Route path="/admin" element={<p>Management</p>}/></Route><Route path="/forbidden" element={<p>Access denied</p>}/></Routes></MemoryRouter></AuthProvider>);
 expect(await screen.findByText('Access denied')).toBeInTheDocument();
});
test.each([['User',['Product.Get','Category.Get'],false],['Admin',['Admin'],true]])('%s catalog shows only permitted controls', async (_,roles,manage) => {
 localStorage.setItem(constants.tokenKey2,token(roles));
 api.get.mockResolvedValue({data:[{id:1,productName:'Coffee',categoryId:2,unitPrice:12,unitsInStock:3}]});
 api.post.mockResolvedValue({data:{data:[{id:2,categoryName:'Drinks'}]}});
 render(<AuthProvider><Catalog/></AuthProvider>);
 expect(await screen.findByText('Coffee')).toBeInTheDocument();
 expect(Boolean(screen.queryByRole('button',{name:/Ürün ekle/}))).toBe(manage);
 expect(Boolean(screen.queryByRole('button',{name:'Düzenle'}))).toBe(manage);
 expect(Boolean(screen.queryByRole('button',{name:'Sil'}))).toBe(manage);
});
