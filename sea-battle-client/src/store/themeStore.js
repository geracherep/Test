import { create } from 'zustand';

const savedTheme = localStorage.getItem('theme') || 'light';
document.documentElement.dataset.theme = savedTheme;

const useThemeStore = create((set) => ({
  theme: savedTheme,
  toggleTheme: () =>
    set((state) => {
      const next = state.theme === 'light' ? 'dark' : 'light';
      localStorage.setItem('theme', next);
      document.documentElement.dataset.theme = next;
      return { theme: next };
    }),
}));

export default useThemeStore;
