import { create } from 'zustand';
import { authApi } from '../api/authApi';

const useAuthStore = create((set) => ({
  user: JSON.parse(localStorage.getItem('user') || 'null'),
  token: localStorage.getItem('token') || null,
  error: null,
  loading: false,

  login: async (credentials) => {
    set({ loading: true, error: null });
    try {
      const data = await authApi.login(credentials);
      const user = { userId: data.userId, username: data.username, role: data.role };
      localStorage.setItem('token', data.token);
      localStorage.setItem('user', JSON.stringify(user));
      set({ user, token: data.token, loading: false });
      return true;
    } catch (e) {
      set({ error: e.response?.data?.error || 'Ошибка входа', loading: false });
      return false;
    }
  },

  register: async (credentials) => {
    set({ loading: true, error: null });
    try {
      const data = await authApi.register(credentials);
      const user = { userId: data.userId, username: data.username, role: data.role };
      localStorage.setItem('token', data.token);
      localStorage.setItem('user', JSON.stringify(user));
      set({ user, token: data.token, loading: false });
      return true;
    } catch (e) {
      set({ error: e.response?.data?.error || 'Ошибка регистрации', loading: false });
      return false;
    }
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    set({ user: null, token: null, error: null });
  },
}));

export default useAuthStore;
