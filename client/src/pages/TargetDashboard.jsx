import React, { useEffect, useMemo, useState } from "react";
import {
  Users,
  Loader2,
  Search,
  RefreshCw,
  ShieldCheck,
  FileText,
} from "lucide-react";
import api from "../services/api";
import AppLayout from "../layouts/AppLayout";
import Card, { CardHeader } from "../components/ui/Card";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import ReportCard from "../components/ReportCard";

export default function TargetDashboard() {
  const [candidates, setCandidates] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchText, setSearchText] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    loadCandidates();
  }, []);

  const loadCandidates = async () => {
    setLoading(true);
    setError("");

    try {
      const res = await api.get("/targetusers/candidates");
      setCandidates(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error("Failed loading target candidates", err);

      setError(
        err.response?.data?.message ||
          err.response?.data?.error ||
          "לא ניתן לטעון את רשימת המועמדים כרגע."
      );

      setCandidates([]);
    } finally {
      setLoading(false);
    }
  };

  const filteredCandidates = useMemo(() => {
    const value = searchText.trim().toLowerCase();

    if (!value) return candidates;

    return candidates.filter((candidate) => {
      return (
        candidate.fullName?.toLowerCase().includes(value) ||
        candidate.email?.toLowerCase().includes(value) ||
        candidate.topPersonalityType?.toLowerCase().includes(value)
      );
    });
  }, [candidates, searchText]);

  const stats = useMemo(() => {
    const total = candidates.length;
    const withAnalysis = candidates.filter(
      (c) => c.topPersonalityType || c.diagnosisText
    ).length;

    return {
      total,
      withAnalysis,
      withoutAnalysis: total - withAnalysis,
    };
  }, [candidates]);

  return (
    <AppLayout>
      <header className="mb-8">
        <p className="text-sm text-teal-700 font-semibold mb-2">
          אזור משתמש יעד
        </p>

        <h1 className="text-4xl font-light text-slate-950">
          מועמדים{" "}
          <span className="font-extrabold">מורשים לצפייה</span>
        </h1>

        <p className="text-slate-500 mt-3 leading-7 max-w-3xl">
          כאן מוצגים מועמדים שנתנו הרשאת צפייה למשתמש היעד. עבור כל מועמד
          ניתן לראות את האבחון האחרון שנשמר במערכת.
        </p>
      </header>

      <div className="grid md:grid-cols-3 gap-4 mb-6">
        <StatBox
          icon={<Users size={22} />}
          label="סה״כ מועמדים"
          value={stats.total}
        />

        <StatBox
          icon={<FileText size={22} />}
          label="עם אבחון"
          value={stats.withAnalysis}
        />

        <StatBox
          icon={<ShieldCheck size={22} />}
          label="ללא אבחון"
          value={stats.withoutAnalysis}
        />
      </div>

      <Card className="mb-6">
        <CardHeader
          title="חיפוש וסינון"
          subtitle="איתור מהיר לפי שם מועמד, אימייל או טיפוס אישיות."
          icon={<Search size={22} />}
          action={
            <Button variant="secondary" size="sm" onClick={loadCandidates}>
              <RefreshCw size={16} />
              רענון
            </Button>
          }
        />

        <Input
          label="חיפוש מועמד"
          value={searchText}
          onChange={setSearchText}
          placeholder="הקלידי שם, אימייל או טיפוס..."
          icon={<Search size={18} />}
        />
      </Card>

      <Card>
        <CardHeader
          title="רשימת מועמדים"
          subtitle="מועמדים שאישרו לך צפייה בתוצאות האבחון שלהם."
          icon={<Users size={22} />}
        />

        {loading && (
          <div className="py-20 flex flex-col items-center justify-center text-slate-500">
            <Loader2 className="animate-spin mb-4" size={36} />
            <p className="font-medium">טוען מועמדים...</p>
          </div>
        )}

        {!loading && error && (
          <div className="bg-red-50 border border-red-200 text-red-700 rounded-3xl p-6 text-sm leading-7">
            {error}
          </div>
        )}

        {!loading && !error && filteredCandidates.length === 0 && (
          <EmptyCandidatesState hasSearch={Boolean(searchText)} />
        )}

        {!loading && !error && filteredCandidates.length > 0 && (
          <div className="grid lg:grid-cols-2 gap-5">
            {filteredCandidates.map((candidate) => (
              <ReportCard
                key={`${candidate.candidateId}-${candidate.analysisId}`}
                candidateName={candidate.fullName}
                email={candidate.email}
                analysisDate={candidate.analysisDate}
                topPersonalityType={candidate.topPersonalityType}
                diagnosisText={candidate.diagnosisText}
                score={candidate.score}
              />
            ))}
          </div>
        )}
      </Card>
    </AppLayout>
  );
}

function StatBox({ icon, label, value }) {
  return (
    <div className="bg-white border border-slate-200 rounded-3xl shadow-sm p-5">
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="text-sm text-slate-500">{label}</p>
          <p className="text-3xl font-extrabold text-slate-950 mt-2">
            {value}
          </p>
        </div>

        <div className="w-12 h-12 rounded-2xl bg-teal-50 text-teal-700 flex items-center justify-center">
          {icon}
        </div>
      </div>
    </div>
  );
}

function EmptyCandidatesState({ hasSearch }) {
  return (
    <div className="bg-slate-50 border border-slate-100 rounded-3xl p-10 text-center">
      <div className="w-16 h-16 rounded-3xl bg-white border border-slate-200 mx-auto mb-5 flex items-center justify-center text-slate-400">
        <Users size={30} />
      </div>

      <h3 className="text-xl font-extrabold text-slate-900 mb-2">
        {hasSearch ? "לא נמצאו תוצאות" : "אין מועמדים מורשים כרגע"}
      </h3>

      <p className="text-slate-500 leading-7 max-w-md mx-auto">
        {hasSearch
          ? "נסי לשנות את מילות החיפוש או לרענן את הרשימה."
          : "מועמדים יופיעו כאן לאחר שיאשרו לך הרשאת צפייה במסך ניהול ההרשאות שלהם."}
      </p>
    </div>
  );
}