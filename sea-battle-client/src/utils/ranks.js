export const RANKS = [
  { rank: 'Recruit', name: 'Начинающий моряк', minXp: 0, icon: '🪖' },
  { rank: 'Sailor', name: 'Матрос', minXp: 101, icon: '⚓' },
  { rank: 'Boatswain', name: 'Боцман', minXp: 501, icon: '🧭' },
  { rank: 'Navigator', name: 'Штурман', minXp: 1501, icon: '🗺️' },
  { rank: 'Captain', name: 'Капитан', minXp: 3001, icon: '🚢' },
  { rank: 'Commander', name: 'Командор', minXp: 6001, icon: '🎖️' },
  { rank: 'Admiral', name: 'Адмирал', minXp: 10001, icon: '👑' },
];

export const RANK_NAMES = Object.fromEntries(RANKS.map((r) => [r.rank, r.name]));
