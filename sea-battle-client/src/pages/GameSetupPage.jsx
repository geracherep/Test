import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { gameApi } from '../api/gameApi';
import Button from '../components/ui/Button';
import styles from './GameSetup.module.css';

export default function GameSetupPage() {
  const navigate = useNavigate();
  const [mode, setMode] = useState('HumanVsComputer');
  const [aiStrategy, setAiStrategy] = useState('Smart');
  const [placement, setPlacement] = useState('random');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const submit = async () => {
    setBusy(true);
    setError('');
    try {
      const gameId = await gameApi.create({
        mode,
        aiStrategy: mode === 'HumanVsHuman' ? null : aiStrategy,
      });
      if (mode === 'ComputerVsComputer') {
        navigate(`/game/${gameId}`);
        return;
      }
      if (placement === 'random') {
        await gameApi.placeRandom(gameId);
        await gameApi.start(gameId);
      }
      navigate(`/game/${gameId}`);
    } catch (e) {
      setError(e.response?.data?.error || 'Ошибка создания игры');
      setBusy(false);
    }
  };

  return (
    <div className={styles.page}>
      <h2>🎮 Новая игра</h2>
      <div className={styles.group}>
        <span className={styles.label}>Режим игры</span>
        <select value={mode} onChange={(e) => setMode(e.target.value)}>
          <option value="HumanVsComputer">Человек против компьютера</option>
          <option value="ComputerVsComputer">Компьютер против компьютера</option>
          <option value="HumanVsHuman">Человек против человека (онлайн — скоро)</option>
        </select>
      </div>
      {mode !== 'HumanVsHuman' && (
        <div className={styles.group}>
          <span className={styles.label}>Интеллект компьютера</span>
          <select value={aiStrategy} onChange={(e) => setAiStrategy(e.target.value)}>
            <option value="Smart">Интеллектуальный (добивает корабли)</option>
            <option value="Random">Случайные выстрелы</option>
          </select>
        </div>
      )}
      {mode === 'HumanVsComputer' && (
        <div className={styles.group}>
          <span className={styles.label}>Расстановка кораблей</span>
          <select value={placement} onChange={(e) => setPlacement(e.target.value)}>
            <option value="random">Автоматически</option>
            <option value="manual">Вручную</option>
          </select>
        </div>
      )}
      {error && <p className={styles.error}>{error}</p>}
      <Button onClick={submit} disabled={busy}>{busy ? 'Создаём...' : 'Создать игру'}</Button>
    </div>
  );
}
