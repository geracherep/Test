import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute from './components/ui/ProtectedRoute';
import Header from './components/Layout/Header';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import MainMenuPage from './pages/MainMenuPage';
import RulesPage from './pages/RulesPage';
import LeaderboardPage from './pages/LeaderboardPage';
import GameSetupPage from './pages/GameSetupPage';
import GamePage from './pages/GamePage';
import ProfilePage from './pages/ProfilePage';

function App() {
  return (
    <BrowserRouter>
      <Header />
      <main style={{ flex: 1 }}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/rules" element={<RulesPage />} />
          <Route path="/" element={<ProtectedRoute><MainMenuPage /></ProtectedRoute>} />
          <Route path="/setup" element={<ProtectedRoute><GameSetupPage /></ProtectedRoute>} />
          <Route path="/game/:id" element={<ProtectedRoute><GamePage /></ProtectedRoute>} />
          <Route path="/leaderboard" element={<ProtectedRoute><LeaderboardPage /></ProtectedRoute>} />
          <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
