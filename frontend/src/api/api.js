import axios from "axios";
import constants from "../constants/constants";

const api = axios.create({
  baseURL: constants.apiBaseUrl,
  headers: { "Content-Type": "application/json" }
});

// Token otomatik ekleme
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(constants.tokenKey2);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// --- Request helpers ---
export const Get = (endpoint) => api.get(endpoint);

export const Post = (endpoint, data) =>
  api.post(endpoint, data);

export const PostNoneToken = (endpoint, data) =>
  axios.post(constants.apiBaseUrl + endpoint, data, {
    headers: { "Content-Type": "application/json" }
  });

export default api;
