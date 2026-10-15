import apiClient from './client';

export const authApi = {
  register: (data) => apiClient.post('/api/Auth/register', data).then((r) => r.data),
  login: (data) => apiClient.post('/api/Auth/login', data).then((r) => r.data),
};
