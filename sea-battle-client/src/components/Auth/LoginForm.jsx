import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import useAuthStore from '../../store/authStore';
import Button from '../ui/Button';
import Input from '../ui/Input';
import styles from './AuthForm.module.css';

export default function LoginForm() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const login = useAuthStore((s) => s.login);
  const error = useAuthStore((s) => s.error);
  const loading = useAuthStore((s) => s.loading);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    const ok = await login({ username, password });
    if (ok) navigate('/');
  };

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <h2>Вход</h2>
      <Input label="Логин" value={username} onChange={(e) => setUsername(e.target.value)} required />
      <Input label="Пароль" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
      {error && <p className={styles.error}>{error}</p>}
      <Button type="submit" disabled={loading}>{loading ? 'Входим...' : 'Войти'}</Button>
      <p className={styles.link}>Нет аккаунта? <Link to="/register">Регистрация</Link></p>
    </form>
  );
}
