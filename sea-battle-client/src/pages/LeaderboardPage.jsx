import { useEffect, useState } from 'react';
import { leaderboardApi } from '../api/gameApi';
import { RANK_NAMES } from '../utils/ranks';
import styles from './Leaderboard.module.css';

export default function LeaderboardPage() {
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    leaderboardApi.getTop(50).then(setRows).finally(() => setLoading(false));
  }, []);

  return (
    <div className={styles.page}>
      <h2>🏆 Таблица рекордов</h2>
      {loading ? (
        <p>Загрузка...</p>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>#</th><th>Игрок</th><th>Ранг</th><th>XP</th>
              <th>Победы</th><th>Поражения</th><th>% побед</th><th>Рейтинг</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={r.userId}>
                <td>{i + 1}</td>
                <td>{r.username}</td>
                <td>{RANK_NAMES[r.rank] || r.rank}</td>
                <td>{r.experience}</td>
                <td>{r.wins}</td>
                <td>{r.losses}</td>
                <td>{r.winRate}%</td>
                <td>{r.score}</td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr><td colSpan={8}>Пока нет сыгранных игр — стань первым!</td></tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}
