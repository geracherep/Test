import { useEffect, useState } from 'react';
import { usersApi } from '../api/usersApi';
import { RANKS } from '../utils/ranks';
import styles from './Profile.module.css';

export default function ProfilePage() {
  const [profile, setProfile] = useState(null);

  useEffect(() => {
    usersApi.me().then(setProfile).catch(() => {});
  }, []);

  if (!profile) return <div className={styles.page}>Загрузка...</div>;

  const idx = Math.max(0, RANKS.findIndex((r) => r.rank === profile.rank));
  const current = RANKS[idx];
  const next = RANKS[idx + 1];
  const progress = next
    ? Math.min(100, Math.max(0, ((profile.experience - current.minXp) / (next.minXp - current.minXp)) * 100))
    : 100;

  return (
    <div className={styles.page}>
      <h2>👤 Профиль</h2>
      <div className={styles.card}>
        <div className={styles.name}>{profile.username}</div>
        <div className={styles.rank}>{current.icon} {current.name}</div>

        <div className={styles.xpRow}>
          <div className={styles.xpBar}>
            <div className={styles.xpFill} style={{ width: `${progress}%` }} />
          </div>
          <span className={styles.xpText}>{profile.experience} XP</span>
        </div>
        {next && (
          <p className={styles.next}>
            До ранга «{next.name}»: {next.minXp - profile.experience} XP
          </p>
        )}

        <div className={styles.stats}>
          <div><b>{profile.wins}</b><span>Побед</span></div>
          <div><b>{profile.losses}</b><span>Поражений</span></div>
          <div><b>{profile.gamesPlayed}</b><span>Игр</span></div>
          <div><b>{profile.bestStreak}</b><span>Лучшая серия</span></div>
        </div>
      </div>
    </div>
  );
}
