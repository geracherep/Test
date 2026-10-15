import apiClient from './client';

export const gameApi = {
  create: (data) => apiClient.post('/api/Games', data).then((r) => r.data),
  getState: (gameId) => apiClient.get(`/api/Games/${gameId}/state`).then((r) => r.data),
  placeRandom: (gameId) => apiClient.post(`/api/Games/${gameId}/ships/random`).then((r) => r.data),
  placeManual: (gameId, ships) => apiClient.post(`/api/Games/${gameId}/ships/manual`, { ships }).then((r) => r.data),
  start: (gameId) => apiClient.post(`/api/Games/${gameId}/start`).then((r) => r.data),
  pause: (gameId) => apiClient.post(`/api/Games/${gameId}/pause`).then((r) => r.data),
  resume: (gameId) => apiClient.post(`/api/Games/${gameId}/resume`).then((r) => r.data),
  stop: (gameId) => apiClient.post(`/api/Games/${gameId}/stop`).then((r) => r.data),
  move: (gameId, row, column) => apiClient.post(`/api/Moves/${gameId}`, { row, column }).then((r) => r.data),
  aiStep: (gameId) => apiClient.post(`/api/Moves/${gameId}/ai`).then((r) => r.data),
};

export const leaderboardApi = {
  getTop: (count = 50) => apiClient.get(`/api/Leaderboard?count=${count}`).then((r) => r.data),
};
