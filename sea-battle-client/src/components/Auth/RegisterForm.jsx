import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import useAuthStore from '../../store/authStore';
import Button from '../ui/Button';
import Input from '../ui/Input';
import styles from './AuthForm.module.css';

export default function RegisterForm() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [localError, setLocalError] = useState('');
  const register = useAuthStore((s) => s.register);
  const error = useAuthStore((s) => s.error);
  const loading = useAuthStore((s) => s.loading);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (password !== confirm) {
      setLocalError('Пароли не совпадают');
      return;
    }
    setLocalError('');
    const ok = await register({ username, password });
    if (ok) navigate('/');
  };

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <h2>Регистрация</h2>
      <Input label="Логин" value={username} onChange={(e) => setUsername(e.target.value)} required minLength={3} />
      <Input label="Пароль" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={4} />
      <Input label="Повторите пароль" type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} required />
      {(localError || error) && <p className={styles.error}>{localError || error}</p>}
      <Button type="submit" disabled={loading}>{loading ? 'Создаём...' : 'Зарегистрироваться'}</Button>
      <p className={styles.link}>Уже есть аккаунт? <Link to="/login">Войти</Link></p>
    </form>
  );
}
