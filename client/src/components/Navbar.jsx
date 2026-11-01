import React from "react";
import { Home, LogOut } from "lucide-react";
import Button from "./ui/Button";

export default function Navbar({ user, onHome, onLogout }) {
  return (
    <nav className="h-[76px] bg-white/90 backdrop-blur border-b border-slate-200 sticky top-0 z-50">
      <div className="page-container h-full flex items-center justify-between">
        <button
          type="button"
          onClick={onHome}
          className="flex items-center gap-3"
        >
          <div className="w-12 h-12 rounded-2xl bg-slate-900 flex items-center justify-center overflow-hidden shadow-sm">
            <img
              src="/logo.png"
              alt="Graphologital"
              className="w-10 h-10 object-contain"
            />
          </div>

          <div className="text-right">
            <h1 className="font-extrabold text-xl text-slate-950 leading-none">
              Graphologital
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              מערכת אבחון כתב יד
            </p>
          </div>
        </button>

        {user ? (
          <div className="flex items-center gap-4">
            <div className="hidden sm:block text-left">
              <p className="text-sm font-semibold text-slate-800">
                {user.fullName || user.username}
              </p>
              <p className="text-xs text-slate-400">
                {user.role === "candidate" ? "מועמד" : "משתמש יעד"}
              </p>
            </div>

            <Button variant="secondary" size="sm" onClick={onLogout}>
              <LogOut size={17} />
              התנתקות
            </Button>
          </div>
        ) : (
          <Button variant="secondary" size="sm" onClick={onHome}>
            <Home size={17} />
            דף הבית
          </Button>
        )}
      </div>
    </nav>
  );
}