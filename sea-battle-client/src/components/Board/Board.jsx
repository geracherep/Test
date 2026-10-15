import styles from './Board.module.css';

export default function Board({ grid, onCellClick, interactive = false, title }) {
  return (
    <div className={styles.wrapper}>
      {title && <h3 className={styles.title}>{title}</h3>}
      <div className={`${styles.board} ${interactive ? styles.clickable : ''}`}>
        {grid.map((row, r) =>
          row.map((cell, c) => (
            <div
              key={`${r}-${c}`}
              className={`${styles.cell} ${styles[cell] || ''}`}
              onClick={() => interactive && onCellClick && onCellClick(r, c)}
            />
          ))
        )}
      </div>
    </div>
  );
}
