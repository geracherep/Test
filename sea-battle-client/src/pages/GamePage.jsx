import { useCallback, useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { gameApi } from '../api/gameApi';
import Board from '../components/Board/Board';
import Button from '../components/ui/Button';
import styles from './Game.module.css';

const SHIP_SIZES = [4, 3, 3, 2, 2, 2, 1, 1, 1, 1];

const STATUS_NAMES = {
  WaitingForShips: 'Расстановка кораблей',
  ReadyToStart: 'Готово к старту',
  InProgress: 'Идёт бой',
  Paused: 'Пауза',
  Finished: 'Завершена',
  Aborted: 'Остановлена',
};

const emptyGrid = () => Array.from({ length: 10 }, () => Array(10).fill('empty'));

export default function GamePage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [state, setState] = useState(null);
  const [error, setError] = useState('');
  const [placement, setPlacement] = useState(null);
  const [aiThinking, setAiThinking] = useState(false);
  const [isShooting, setIsShooting] = useState(false); // <-- НОВОЕ: блокировка двойных кликов
  const isAiProcessing = useRef(false);
  const stateVersion = useRef(0); // защита от "гонки": устаревшие async-колбэки не должны трогать состояние
  const mountedRef = useRef(true);

  useEffect(() => {
    mountedRef.current = true;
    return () => { mountedRef.current = false; };
  }, []);

  const load = useCallback(async () => {
    try {
      const s = await gameApi.getState(id);
      if (!mountedRef.current) return s;
      stateVersion.current += 1; // любое обновление состояния инвалидирует запланированные ходы ИИ
      setState(s);
      return s;
    } catch (e) {
      if (mountedRef.current) setError(e.response?.data?.error || 'Ошибка загрузки игры');
      return null;
    }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  // Общий планировщик авто-хода ИИ с защитой от гонки состояний:
  // перед выполнением залпа проверяем, что за время таймаута состояние
  // не изменилось (ход уже сделал игрок/ИИ, игра завершилась и т.п.)
  const scheduleAiMove = useCallback((expectedVersion) => {
    isAiProcessing.current = true;
    setAiThinking(true);
    const t = setTimeout(async () => {
      try {
        // ГОНКА СОСТОЯНИЙ: если за время "раздумий" версия состояния изменилась —
        // кто-то уже сходил (или пришёл свежий стейт), отменяем этот залп
        if (!mountedRef.current || stateVersion.current !== expectedVersion) return;
        await gameApi.aiStep(id);
        if (!mountedRef.current) return;
        await load();
      } catch (e) {
        // 400/409 означает, что на сервере уже не ход ИИ (состояние ушло вперёд) — штатная ситуация
        if (e.response?.status !== 400 && e.response?.status !== 409) console.error('AI move error:', e);
        if (mountedRef.current) await load();
      } finally {
        // Если состояние менялось во время нашего запроса, cleanup нового эффекта
        // или новый цикл планирования сами сбросят флаги — не трогаем их, чтобы
        // не снять блокировку, установленную более новым запланированным ходом
        if (mountedRef.current && stateVersion.current === expectedVersion) {
          setAiThinking(false);
          isAiProcessing.current = false;
        }
      }
    }, 1000 + Math.random() * 2500);
    return () => { clearTimeout(t); isAiProcessing.current = false; };
  }, [id, load]);

  // Авто-ход ИИ (ComputerVsComputer)
  useEffect(() => {
    if (!state) return;
    const shouldAiMove = state.mode === 'ComputerVsComputer' && state.status === 'InProgress' && !isAiProcessing.current;
    if (!shouldAiMove) return;
    return scheduleAiMove(stateVersion.current);
  }, [state, scheduleAiMove]);

  // Авто-ход ИИ (HumanVsComputer)
  useEffect(() => {
    if (!state) return;
    const shouldAiMove = state.mode === 'HumanVsComputer' && state.status === 'InProgress' && state.currentTurn !== state.mySide && !isAiProcessing.current;
    if (!shouldAiMove) return;
    return scheduleAiMove(stateVersion.current);
  }, [state, scheduleAiMove]);

  const myGrid = emptyGrid();
  if (state) {
    for (const ship of state.myShips) {
      for (const cell of ship.cells) {
        myGrid[cell.row][cell.column] = ship.isSunk ? 'sunk' : cell.isHit ? 'hit' : 'ship';
      }
    }
    for (const m of state.enemyMoves) {
      if (m.result === 'Miss' && myGrid[m.row][m.column] === 'empty') myGrid[m.row][m.column] = 'miss';
    }
  }

  const enemyGrid = emptyGrid();
  if (state) {
    for (const m of state.myMoves) {
      if (m.result === 'Miss') enemyGrid[m.row][m.column] = 'miss';
      if (m.result === 'Hit' || m.result === 'Sunk' || m.result === 'Win') enemyGrid[m.row][m.column] = 'hit';
    }
    for (const ship of state.enemySunkShips) {
      for (const cell of ship.cells) enemyGrid[cell.row][cell.column] = 'sunk';
    }
  }

  const canPlace = (grid, r, c, size, horizontal) => {
    const cells = [];
    for (let i = 0; i < size; i++) {
      const rr = horizontal ? r : r + i;
      const cc = horizontal ? c + i : c;
      if (rr < 0 || rr > 9 || cc < 0 || cc > 9) return null;
      cells.push([rr, cc]);
    }
    for (const [rr, cc] of cells) {
      for (let dr = -1; dr <= 1; dr++) {
        for (let dc = -1; dc <= 1; dc++) {
          const nr = rr + dr, nc = cc + dc;
          if (nr >= 0 && nr < 10 && nc >= 0 && nc < 10 && grid[nr][nc] !== 'empty') return null;
        }
      }
    }
    return cells;
  };

  const onPlaceClick = (r, c) => {
    if (!placement || placement.current >= SHIP_SIZES.length) return;
    const size = SHIP_SIZES[placement.current];
    const cells = canPlace(placement.grid, r, c, size, placement.horizontal);
    if (!cells) return;
    const grid = placement.grid.map((row) => [...row]);
    cells.forEach(([rr, cc]) => { grid[rr][cc] = 'ship'; });
    setPlacement({ ...placement, grid, ships: [...placement.ships, { size, cells }], current: placement.current + 1 });
  };

  const submitPlacement = async () => {
    try {
      const payload = placement.ships.map((s) => {
        const [r0, c0] = s.cells[0];
        const [r1] = s.cells[s.cells.length - 1];
        return { size: s.size, startRow: r0, startColumn: c0, isHorizontal: r0 === r1 };
      });
      await gameApi.placeManual(id, payload);
      setPlacement(null);
      await gameApi.start(id);
      load();
    } catch (e) {
      setError(e.response?.data?.error || 'Ошибка расстановки');
    }
  };

  const autoPlace = async () => {
    try {
      await gameApi.placeRandom(id);
      setPlacement(null);
      await gameApi.start(id);
      load();
    } catch (e) {
      setError(e.response?.data?.error || 'Ошибка авторасстановки');
    }
  };

  // --- ИСПРАВЛЕННАЯ СТРЕЛЬБА С ЗАЩИТОЙ ОТ ДВОЙНЫХ КЛИКОВ ---
  const onShoot = async (r, c) => {
    if (!state || state.status !== 'InProgress') return;
    if (state.currentTurn !== state.mySide) return;
    if (isShooting) return; // <-- БЛОКИРУЕМ повторные нажатия

    setIsShooting(true);
    try {
      await gameApi.move(id, r, c);
      setError('');
      await load();
    } catch (e) {
      // Если сервер вернул 409 (Conflict) или 400 (Bad Request), значит выстрел уже был обработан
      // Просто перезагружаем состояние, чтобы синхронизировать поле, не показывая страшную ошибку
      if (e.response?.status === 409 || e.response?.status === 400) {
        await load();
      } else {
        setError(e.response?.data?.error || 'Ошибка хода');
      }
    } finally {
      setIsShooting(false); // <-- СНИМАЕМ блокировку
    }
  };

  if (!state) return <div className={styles.page}>Загрузка...</div>;

  const isCvC = state.mode === 'ComputerVsComputer';
  const myTurn = state.status === 'InProgress' && state.currentTurn === state.mySide && !isCvC;
  const needsPlacement = state.status === 'WaitingForShips' && !isCvC && state.myShips.length === 0 && !placement;
  const placing = placement !== null;

  return (
    <div className={styles.page}>
      <div className={styles.statusBar}>
        <span>Статус: {STATUS_NAMES[state.status] || state.status}</span>
        {state.status === 'InProgress' && (
          <span>{aiThinking || isShooting ? '🤖 Обработка хода...' : isCvC ? '🤖 ИИ думает...' : myTurn ? '🎯 Ваш ход' : '⏳ Ход противника'}</span>
        )}
        {state.status === 'Finished' && (
          <span className={styles.result}>
            {state.winner === state.mySide ? '🏆 Победа!' : isCvC ? '🏁 Битва ИИ завершена' : '💀 Поражение'}
          </span>
        )}
      </div>

      {error && <p className={styles.error}>{error}</p>}

      <div className={styles.boards}>
        <Board
          title="Ваше поле"
          grid={placing ? placement.grid : myGrid}
          interactive={placing}
          onCellClick={onPlaceClick}
        />
        <Board
          title="Поле противника"
          grid={enemyGrid}
          interactive={myTurn && !aiThinking && !isShooting} // <-- ОТКЛЮЧАЕМ клики во время выстрела
          onCellClick={onShoot}
        />
      </div>

      <div className={styles.controls}>
        {needsPlacement && (
          <Button onClick={() => setPlacement({ grid: emptyGrid(), ships: [], current: 0, horizontal: true })}>
            🚢 Расставить корабли вручную
          </Button>
        )}
        {placing && (
          <>
            <span className={styles.hint}>
              Корабль: {placement.current < SHIP_SIZES.length ? SHIP_SIZES[placement.current] + '-палубный' : 'все расставлены'}
              {' '}| Ориентация: {placement.horizontal ? 'горизонтально →' : 'вертикально ↓'}
            </span>
            <Button onClick={() => setPlacement({ ...placement, horizontal: !placement.horizontal })}>🔄 Повернуть</Button>
            <Button onClick={autoPlace}>🎲 Авторасстановка</Button>
            {placement.current >= SHIP_SIZES.length && <Button onClick={submitPlacement}>✅ В бой!</Button>}
          </>
        )}
        {isCvC && state.status === 'WaitingForShips' && (
          <Button onClick={async () => { await gameApi.start(id); load(); }}>▶ Запустить битву ИИ</Button>
        )}
        {state.status === 'InProgress' && !isCvC && (
          <Button onClick={async () => { await gameApi.pause(id); load(); }}>⏸ Пауза</Button>
        )}
        {state.status === 'Paused' && (
          <Button onClick={async () => { await gameApi.resume(id); load(); }}>▶ Продолжить</Button>
        )}
        {(state.status === 'InProgress' || state.status === 'Paused') && (
          <Button onClick={async () => { await gameApi.stop(id); load(); }}>🛑 Стоп</Button>
        )}
        {(state.status === 'Finished' || state.status === 'Aborted') && (
          <Button onClick={() => navigate('/setup')}>🔄 Новая игра</Button>
        )}
      </div>
    </div>
  );
}
