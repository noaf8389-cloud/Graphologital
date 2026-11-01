import React from "react";
import {
  ArrowLeft,
  LogIn,
  UserPlus,
  FileText,
  BarChart3,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import AppLayout from "../layouts/AppLayout";

export default function Home({ onLogin, onRegister }) {
  return (
    <AppLayout>
      <section className="grid lg:grid-cols-2 gap-12 items-center min-h-[calc(100vh-156px)]">
        <div>
          <div className="inline-flex items-center gap-2 bg-white border border-slate-200 rounded-full px-4 py-2 text-sm text-slate-600 shadow-sm mb-6">
            <Sparkles size={16} className="text-teal-600" />
            מערכת אבחון כתב יד מקצועית
          </div>

          <h1 className="text-5xl lg:text-6xl font-light leading-tight text-slate-950 mb-6">
            Graphologital
            <span className="block font-extrabold text-slate-900 mt-2">
              אבחון גרפולוגי דיגיטלי
            </span>
          </h1>

          <p className="text-lg text-slate-600 leading-8 max-w-xl mb-8">
            מערכת להעלאת כתב יד, הפעלת מנוע ניתוח גרפולוגי, הצגת תוצאות,
            שמירת אבחונים וניהול הרשאות צפייה למשתמשי יעד.
          </p>

          <div className="flex flex-col sm:flex-row gap-4">
            <Button size="lg" onClick={onLogin}>
              <LogIn size={19} />
              התחברות למערכת
              <ArrowLeft size={18} />
            </Button>

            <Button variant="secondary" size="lg" onClick={onRegister}>
              <UserPlus size={19} />
              הרשמה
            </Button>
          </div>
        </div>

        <Card className="relative overflow-hidden">
          <div className="absolute top-0 left-0 w-44 h-44 bg-teal-100/60 rounded-full blur-3xl -translate-x-10 -translate-y-10" />

          <div className="relative">
            <div className="flex justify-center mb-8">
              <div className="w-32 h-32 rounded-[2rem] bg-slate-900 flex items-center justify-center shadow-xl overflow-hidden">
                <img
                  src="/logo.png"
                  alt="Graphologital Logo"
                  className="w-24 h-24 object-contain"
                />
              </div>
            </div>

            <h2 className="text-2xl font-extrabold text-center text-slate-950 mb-2">
              מערכת אחת. תהליך מלא.
            </h2>

            <p className="text-center text-slate-500 leading-7 mb-8">
              מהעלאת כתב היד ועד הצגת האבחון למשתמש מורשה.
            </p>

            <div className="space-y-4">
              <Feature
                icon={<FileText size={22} />}
                title="העלאת כתב יד"
                text="המועמד מעלה קובץ כתב יד ברור לצורך עיבוד ואבחון."
              />

              <Feature
                icon={<BarChart3 size={22} />}
                title="הצגת תוצאה"
                text="המערכת מציגה טיפוס מוביל, אבחון וציוני התאמה."
              />

              <Feature
                icon={<ShieldCheck size={22} />}
                title="ניהול הרשאות"
                text="המועמד בוחר מי ממשתמשי היעד רשאי לצפות באבחון."
              />
            </div>
          </div>
        </Card>
      </section>
    </AppLayout>
  );
}

function Feature({ icon, title, text }) {
  return (
    <div className="flex gap-4 p-4 rounded-2xl bg-slate-50 border border-slate-100">
      <div className="w-11 h-11 rounded-xl bg-teal-50 text-teal-700 flex items-center justify-center shrink-0">
        {icon}
      </div>

      <div>
        <h3 className="font-bold text-slate-900">{title}</h3>
        <p className="text-sm text-slate-500 mt-1 leading-6">{text}</p>
      </div>
    </div>
  );
}

function MiniStat({ value, label }) {
  return (
    <div className="bg-white border border-slate-200 rounded-2xl px-4 py-3 shadow-sm">
      <p className="text-xl font-extrabold text-slate-950">{value}</p>
      <p className="text-xs text-slate-500 mt-1">{label}</p>
    </div>
  );
}