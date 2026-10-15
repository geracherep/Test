import { useNavigate } from 'react-router-dom';
import useAuthStore from '../store/authStore';
import Button from '../components/ui/Button';
import styles from './MainMenu.module.css';

export default function MainMenuPage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);

  return (
    <div className={styles.menu}>
      <h1>Привет, {user?.username}! 👋</h1>
      <div className={styles.buttons}>
        <Button onClick={() => navigate('/setup')}>🎮 Новая игра</Button>
        <Button onClick={() => navigate('/leaderboard')}>🏆 Таблица рекордов</Button>
        <Button onClick={() => navigate('/rules')}>📜 Правила</Button>
      </div>
    </div>
  );
}
