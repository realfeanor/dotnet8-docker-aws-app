import axios from 'axios';
import constants from '../constants/constants';
const api = axios.create({ baseURL: constants.apiBaseUrl, headers: { 'Content-Type': 'application/json' } });
api.interceptors.request.use(config => {
  const token = localStorage.getItem(constants.authTokenKey);
  if (token && !/^Auth\//i.test(config.url)) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(response => response, error => {
  if (error.response?.status === 401 && !/^Auth\//i.test(error.config?.url || '')) window.dispatchEvent(new Event('session-expired'));
  return Promise.reject(error);
});
export function errorMessage(error) {
  const data = error.response?.data;
  if (typeof data === 'string') return data;
  if (data?.errors) return Object.values(data.errors).flat().map(e => typeof e === 'string' ? e : e.errorMessage || e.message).filter(Boolean).join(' ');
  return data?.message || data?.Message || data?.detail || error.message || 'The request could not be completed.';
}
export default api;

