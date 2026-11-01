import React, { useEffect, useState } from "react";
import Navbar from "./components/Navbar";
import Home from "./pages/Home";
import AuthPage from "./pages/AuthPage";
import CandidateDashboard from "./pages/CandidateDashboard";
import TargetDashboard from "./pages/TargetDashboard";

export default function App() {
  const [page, setPage] = useState("home");
  const [user, setUser] = useState(null);

  useEffect(() => {
    const savedUser = localStorage.getItem("graphologital_user");
    const savedToken = localStorage.getItem("graphologital_token");

    if (savedUser && savedToken) {
      const parsedUser = JSON.parse(savedUser);
      setUser(parsedUser);
      setPage(parsedUser.role === "candidate" ? "candidate" : "target");
    }
  }, []);

  const handleAuthSuccess = ({ token, user }) => {
    localStorage.setItem("graphologital_token", token);
    localStorage.setItem("graphologital_user", JSON.stringify(user));

    setUser(user);
    setPage(user.role === "candidate" ? "candidate" : "target");
  };

  const logout = () => {
    localStorage.removeItem("graphologital_token");
    localStorage.removeItem("graphologital_user");

    setUser(null);
    setPage("home");
  };

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900" dir="rtl">
      <Navbar user={user} onHome={() => setPage("home")} onLogout={logout} />

      {page === "home" && (
        <Home
          onLogin={() => setPage("login")}
          onRegister={() => setPage("register")}
        />
      )}

      {page === "login" && (
        <AuthPage mode="login" onSuccess={handleAuthSuccess} />
      )}

      {page === "register" && (
        <AuthPage mode="register" onSuccess={handleAuthSuccess} />
      )}

      {page === "candidate" && <CandidateDashboard />}
      {page === "target" && <TargetDashboard />}
    </div>
  );
}