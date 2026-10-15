import { useNavigate } from 'react-router-dom';
import useAuthStore from '../../store/authStore';
import ThemeToggle from '../Theme/ThemeToggle';
import styles from './Header.module.css';

export default function Header() {
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className={styles.header}>
      <div className={styles.logo} onClick={() => navigate('/')}>⚓ Морской Бой</div>
      <div className={styles.right}>
        {user && (
          <span className={styles.username} onClick={() => navigate('/profile')}>
            👤 {user.username}
          </span>
        )}
        <ThemeToggle />
        {user && (
          <button className={styles.logout} onClick={handleLogout}>Выйти</button>
        )}
      </div>
    </header>
  );
}
