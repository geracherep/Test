import useThemeStore from '../../store/themeStore';
import styles from './ThemeToggle.module.css';

export default function ThemeToggle() {
  const theme = useThemeStore((s) => s.theme);
  const toggleTheme = useThemeStore((s) => s.toggleTheme);

  return (
    <button className={styles.toggle} onClick={toggleTheme} title="Сменить тему">
      {theme === 'light' ? '🌙' : '☀️'}
    </button>
  );
}
