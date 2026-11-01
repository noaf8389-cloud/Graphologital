import React, { useState } from "react";
import { User, Lock, Mail, Phone, Briefcase, Loader2 } from "lucide-react";
import api from "../services/api";

export default function AuthPage({ mode, onSuccess }) {
  const isLogin = mode === "login";

  const [role, setRole] = useState("candidate");
  const [form, setForm] = useState({
    username: "",
    password: "",
    fullName: "",
    email: "",
    phone: "",
    roleDescription: "",
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const update = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setError("");
  };

  const submit = async (e) => {
    e.preventDefault();

    if (!form.username || !form.password) {
      setError("יש להזין שם משתמש וסיסמה.");
      return;
    }

    if (!isLogin && !form.fullName) {
      setError("יש להזין שם מלא.");
      return;
    }

    setLoading(true);
    setError("");

    try {
      const base = role === "candidate" ? "/candidates" : "/targetusers";
      const endpoint = isLogin ? `${base}/login` : `${base}/register`;

      const payload = isLogin
        ? {
            username: form.username,
            password: form.password,
          }
        : {
            username: form.username,
            password: form.password,
            fullName: form.fullName,
            email: form.email,
            phone: form.phone,
            roleDescription: form.roleDescription,
          };

    const res = await api.post(endpoint, payload);
            
    localStorage.setItem("token", res.data.token);
    localStorage.setItem("role", res.data.user.role);
            
    onSuccess({
      token: res.data.token,
      user: res.data.user,
    });
    
    } catch (err) {
      setError(
        err.response?.data?.error ||
          "אירעה שגיאה בתקשורת מול השרת."
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-[calc(100vh-72px)] bg-slate-50 px-4 py-10" dir="rtl">
      <div className="max-w-5xl mx-auto grid lg:grid-cols-2 gap-8 items-center">
        <section className="hidden lg:block">
          <h1 className="text-5xl font-light text-slate-950 mb-5">
            {isLogin ? "התחברות" : "הרשמה"}
            <span className="block font-bold mt-2">ל־Graphologital</span>
          </h1>

          <p className="text-slate-600 leading-8 max-w-md">
            בחרי את סוג המשתמש, מלאי פרטים והמערכת תעביר אותך למסך המתאים.
          </p>
        </section>

        <section className="bg-white border border-slate-200 rounded-3xl shadow-sm p-6 md:p-8">
          <div className="flex justify-center mb-6">
            <img src="/logo.png" alt="Graphologital" className="w-28 object-contain" />
          </div>

          <h2 className="text-2xl font-bold text-slate-950 text-center mb-6">
            {isLogin ? "כניסה למערכת" : "יצירת חשבון"}
          </h2>

          <div className="grid grid-cols-2 gap-3 mb-6">
            <button
              type="button"
              onClick={() => setRole("candidate")}
              className={`rounded-2xl py-3 text-sm border transition ${
                role === "candidate"
                  ? "bg-slate-900 text-white border-slate-900"
                  : "bg-white text-slate-600 border-slate-200"
              }`}
            >
              מועמד
            </button>

            <button
              type="button"
              onClick={() => setRole("target")}
              className={`rounded-2xl py-3 text-sm border transition ${
                role === "target"
                  ? "bg-slate-900 text-white border-slate-900"
                  : "bg-white text-slate-600 border-slate-200"
              }`}
            >
              משתמש יעד
            </button>
          </div>

          {error && (
            <div className="mb-5 bg-red-50 border border-red-200 text-red-700 rounded-2xl px-4 py-3 text-sm">
              {error}
            </div>
          )}

          <form onSubmit={submit} className="space-y-4">
            {!isLogin && (
              <Input icon={<User />} label="שם מלא" value={form.fullName} onChange={(v) => update("fullName", v)} />
            )}

            <Input icon={<User />} label="שם משתמש" value={form.username} onChange={(v) => update("username", v)} />
            <Input icon={<Lock />} label="סיסמה" type="password" value={form.password} onChange={(v) => update("password", v)} />

            {!isLogin && (
              <>
                <Input icon={<Mail />} label="אימייל" value={form.email} onChange={(v) => update("email", v)} />
                <Input icon={<Phone />} label="טלפון" value={form.phone} onChange={(v) => update("phone", v)} />

                {role === "target" && (
                  <Input
                    icon={<Briefcase />}
                    label="תיאור תפקיד / מטרה"
                    value={form.roleDescription}
                    onChange={(v) => update("roleDescription", v)}
                  />
                )}
              </>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-slate-900 text-white py-4 rounded-2xl hover:bg-slate-800 transition disabled:opacity-60 flex items-center justify-center gap-2"
            >
              {loading && <Loader2 className="animate-spin" size={18} />}
              {isLogin ? "כניסה מאובטחת" : "יצירת חשבון"}
            </button>
          </form>
        </section>
      </div>
    </main>
  );
}

function Input({ icon, label, value, onChange, type = "text" }) {
  return (
    <label className="block">
      <span className="text-xs text-slate-500 mb-1 block">{label}</span>

      <div className="flex items-center gap-2 bg-slate-50 border border-slate-200 rounded-2xl px-3 py-3 focus-within:border-teal-400 transition">
        <span className="text-slate-400">{React.cloneElement(icon, { size: 18 })}</span>

        <input
          type={type}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="w-full bg-transparent outline-none text-sm text-slate-800"
        />
      </div>
    </label>
  );
}