import apiClient from './client';

export const usersApi = {
  me: () => apiClient.get('/api/Users/me').then((r) => r.data),
};
