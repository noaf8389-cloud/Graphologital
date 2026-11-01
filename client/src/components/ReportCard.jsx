import React from "react";
import {
  FileText,
  User,
  CalendarDays,
  BarChart3,
  ArrowLeft,
} from "lucide-react";
import Card from "./ui/Card";
import Button from "./ui/Button";

export default function ReportCard({
  candidateName = "מועמד",
  email = "",
  analysisDate = null,
  topPersonalityType = "",
  diagnosisText = "",
  score = null,
  onOpen,
}) {
  const hasAnalysis = Boolean(topPersonalityType || diagnosisText);

  return (
    <Card hover className="overflow-hidden">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-4">
          <div className="w-13 h-13 rounded-2xl bg-slate-900 text-white flex items-center justify-center shrink-0">
            <User size={24} />
          </div>

          <div>
            <h3 className="text-lg font-extrabold text-slate-950">
              {candidateName || "מועמד"}
            </h3>

            {email && (
              <p className="text-sm text-slate-500 mt-1">{email}</p>
            )}
          </div>
        </div>

        <StatusBadge hasAnalysis={hasAnalysis} />
      </div>

      <div className="grid sm:grid-cols-3 gap-3 mt-6">
        <InfoBox
          icon={<FileText size={18} />}
          label="טיפוס מוביל"
          value={topPersonalityType || "אין אבחון"}
        />

        <InfoBox
          icon={<BarChart3 size={18} />}
          label="ציון מרכזי"
          value={score !== null && score !== undefined ? `${Math.round(score)}%` : "-"}
        />

        <InfoBox
          icon={<CalendarDays size={18} />}
          label="תאריך אבחון"
          value={formatDate(analysisDate)}
        />
      </div>

      <div className="mt-5 bg-slate-50 border border-slate-100 rounded-2xl p-4 min-h-[96px]">
        <p className="text-xs font-semibold text-slate-400 mb-2">
          תקציר אבחון
        </p>

        <p className="text-sm text-slate-700 leading-7 line-clamp-3">
          {diagnosisText || "עדיין לא קיים אבחון שמור עבור מועמד זה."}
        </p>
      </div>

      {onOpen && (
        <div className="mt-5 flex justify-end">
          <Button variant="secondary" size="sm" onClick={onOpen}>
            צפייה בדוח
            <ArrowLeft size={16} />
          </Button>
        </div>
      )}
    </Card>
  );
}

function InfoBox({ icon, label, value }) {
  return (
    <div className="bg-white border border-slate-100 rounded-2xl p-4">
      <div className="flex items-center gap-2 text-slate-400 mb-2">
        {icon}
        <span className="text-xs font-semibold">{label}</span>
      </div>

      <p className="text-sm font-bold text-slate-900 truncate">{value}</p>
    </div>
  );
}

function StatusBadge({ hasAnalysis }) {
  return (
    <span
      className={[
        "inline-flex items-center rounded-full px-3 py-1 text-xs font-bold",
        hasAnalysis
          ? "bg-emerald-50 text-emerald-700 border border-emerald-100"
          : "bg-amber-50 text-amber-700 border border-amber-100",
      ].join(" ")}
    >
      {hasAnalysis ? "קיים אבחון" : "אין אבחון"}
    </span>
  );
}

function formatDate(date) {
  if (!date) return "-";

  try {
    return new Date(date).toLocaleDateString("he-IL", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    });
  } catch {
    return "-";
  }
}