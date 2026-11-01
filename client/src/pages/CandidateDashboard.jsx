import React, { useEffect, useMemo, useRef, useState } from "react";
import {
  Upload,
  FileText,
  Loader2,
  CheckCircle,
  AlertCircle,
  X,
  Clock,
  ShieldCheck,
  Building2,
  Search,
  RefreshCw,
} from "lucide-react";
import api from "../services/api";
import AppLayout from "../layouts/AppLayout";
import Card, { CardHeader } from "../components/ui/Card";
import Button from "../components/ui/Button";

import * as pdfjsLib from "pdfjs-dist";
import pdfWorker from "pdfjs-dist/build/pdf.worker?url";

pdfjsLib.GlobalWorkerOptions.workerSrc = pdfWorker;

function createImagePreview(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = () => resolve(reader.result);
    reader.onerror = () => reject(new Error("טעינת תצוגת התמונה נכשלה."));

    reader.readAsDataURL(file);
  });
}

async function convertPdfToPngFile(pdfFile) {
  const arrayBuffer = await pdfFile.arrayBuffer();
  const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;

  if (pdf.numPages === 0) {
    throw new Error("קובץ ה-PDF ריק.");
  }

  const page = await pdf.getPage(1);
  const scale = 2;
  const viewport = page.getViewport({ scale });

  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d");

  canvas.width = viewport.width;
  canvas.height = viewport.height;

  await page.render({
    canvasContext: context,
    viewport,
  }).promise;

  const blob = await new Promise((resolve) => {
    canvas.toBlob(resolve, "image/png");
  });

  if (!blob) {
    throw new Error("המרת PDF לתמונה נכשלה.");
  }

  return new File([blob], pdfFile.name.replace(/\.pdf$/i, ".png"), {
    type: "image/png",
  });
}

export default function CandidateDashboard() {
  const fileInputRef = useRef(null);

  const [selectedFile, setSelectedFile] = useState(null);
  const [analysis, setAnalysis] = useState(null);
  const [previewUrl, setPreviewUrl] = useState("");
  const [targetSearch, setTargetSearch] = useState("");

  const [history, setHistory] = useState([]);
  const [targets, setTargets] = useState([]);

  const [isUploading, setIsUploading] = useState(false);
  const [loadingPermissions, setLoadingPermissions] = useState(false);
  const [message, setMessage] = useState({ type: "", text: "" });

  useEffect(() => {
    loadHistory();
    loadPermissions();
  }, []);

  const authHeaders = () => {
    const token = localStorage.getItem("token");

    return {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    };
  };

  const loadHistory = async () => {
    try {
      const res = await api.get("/candidates/analyses", authHeaders());
      setHistory(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error("שגיאה בטעינת היסטוריית אבחונים", err);
      setHistory([]);
    }
  };

  const loadPermissions = async () => {
    setLoadingPermissions(true);

    try {
      const res = await api.get("/permissions/targets", authHeaders());
      setTargets(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error("שגיאה בטעינת הרשאות", err);
      setTargets([]);
    } finally {
      setLoadingPermissions(false);
    }
  };

  const normalizedAnalysis = useMemo(() => {
    if (!analysis) return null;

    return {
      topPersonalityType:
        analysis.topPersonalityType ||
        analysis.TopPersonalityType ||
        "לא זוהה",

      diagnosisText:
        analysis.diagnosisText ||
        analysis.DiagnosisText ||
        "",
    };
  }, [analysis]);

  const allowedTargetsCount = useMemo(() => {
    return targets.filter((target) => target.isAllowed).length;
  }, [targets]);

  const handleFileChange = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const name = file.name.toLowerCase();
    const validExtensions = [".tif", ".tiff", ".jpg", ".jpeg", ".png", ".pdf"];
    const isValid = validExtensions.some((ext) => name.endsWith(ext));

    if (!isValid) {
      setSelectedFile(null);
      setAnalysis(null);
      setPreviewUrl("");
      setMessage({
        type: "error",
        text: "סוג הקובץ אינו נתמך. ניתן להעלות TIF, TIFF, JPG, PNG או PDF בלבד.",
      });
      return;
    }

    setSelectedFile(file);
    setAnalysis(null);
    setMessage({ type: "", text: "" });

    const canPreview =
      name.endsWith(".jpg") ||
      name.endsWith(".jpeg") ||
      name.endsWith(".png");

    if (canPreview) {
      try {
        const preview = await createImagePreview(file);
        setPreviewUrl(preview);
      } catch {
        setPreviewUrl("");
      }
    } else {
      setPreviewUrl("");
    }
  };

  const clearFile = () => {
    setSelectedFile(null);
    setAnalysis(null);
    setPreviewUrl("");
    setMessage({ type: "", text: "" });

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const upload = async () => {
    if (!selectedFile) {
      setMessage({
        type: "error",
        text: "יש לבחור קובץ כתב יד לפני שליחה לאבחון.",
      });
      return;
    }

    setIsUploading(true);
    setMessage({ type: "", text: "" });

    try {
      let fileToSend = selectedFile;
      const selectedFileName = selectedFile.name.toLowerCase();

      if (selectedFileName.endsWith(".pdf")) {
        fileToSend = await convertPdfToPngFile(selectedFile);

        const preview = await createImagePreview(fileToSend);
        setPreviewUrl(preview);
      } else if (
        !previewUrl &&
        (selectedFileName.endsWith(".jpg") ||
          selectedFileName.endsWith(".jpeg") ||
          selectedFileName.endsWith(".png"))
      ) {
        const preview = await createImagePreview(selectedFile);
        setPreviewUrl(preview);
      }

      const formData = new FormData();
      formData.append("file", fileToSend);

      const res = await api.post("/handwriting/upload", formData, authHeaders());

      setAnalysis(res.data.analysis);

      setMessage({
        type: "success",
        text: "האבחון בוצע ונשמר בהצלחה.",
      });

      await loadHistory();
    } catch (err) {
      console.error("שגיאה בהעלאה", err);

      setMessage({
        type: "error",
        text:
          err.response?.data?.message ||
          err.response?.data?.error ||
          "שגיאה בביצוע האבחון.",
      });
    } finally {
      setIsUploading(false);
    }
  };

  const togglePermission = async (target) => {
    const targetUserId = target.targetUserId;
    const isAllowed = target.isAllowed;

    try {
      if (isAllowed) {
        await api.delete(`/permissions/${targetUserId}`, authHeaders());
      } else {
        await api.post(`/permissions/${targetUserId}`, null, authHeaders());
      }

      await loadPermissions();

      setMessage({
        type: "success",
        text: "הרשאת הצפייה עודכנה בהצלחה.",
      });
    } catch (err) {
      console.error("שגיאה בעדכון הרשאה", err);

      setMessage({
        type: "error",
        text:
          err.response?.data?.message ||
          err.response?.data?.error ||
          "שגיאה בעדכון הרשאת הצפייה.",
      });
    }
  };

  return (
    <AppLayout className="!py-6">
      <header className="mb-4">
        <div className="flex flex-col xl:flex-row xl:items-start xl:justify-between gap-4">
          <div>
            <p className="text-xs text-teal-700 font-semibold mb-1.5">
              אזור מועמד
            </p>

            <h1 className="text-3xl lg:text-4xl font-light text-slate-950 leading-tight">
              ניהול אבחוני כתב יד{" "}
              <span className="font-extrabold">והרשאות צפייה</span>
            </h1>

            <p className="text-slate-500 mt-2 leading-7 max-w-3xl text-sm">
              העלאת כתב יד לאבחון, צפייה בתוצאה האחרונה, ניהול הרשאות
              והיסטוריית אבחונים.
            </p>
          </div>

          <div className="flex xl:pt-1 gap-2 shrink-0">
            <MiniStat label="אבחונים" value={history.length} />
            <MiniStat label="מורשים" value={allowedTargetsCount} />
            <MiniStat label="קובץ" value={selectedFile ? 1 : 0} />
          </div>
        </div>
      </header>

      {message.text && (
        <StatusMessage type={message.type} text={message.text} />
      )}

      <section className="grid xl:grid-cols-[410px_minmax(0,1fr)] gap-5 items-stretch mb-5">
        <UploadCard
          fileInputRef={fileInputRef}
          selectedFile={selectedFile}
          isUploading={isUploading}
          onFileChange={handleFileChange}
          onClear={clearFile}
          onUpload={upload}
        />

        <ResultCard
          isUploading={isUploading}
          analysis={normalizedAnalysis}
          previewUrl={previewUrl}
        />
      </section>

      <section className="grid xl:grid-cols-[minmax(0,1fr)_410px] gap-5 items-stretch">
        <HistoryCard
          history={history}
          onSelect={(item) => {
            setAnalysis(item);
            setPreviewUrl("");
            setMessage({ type: "", text: "" });
          }}
          onRefresh={loadHistory}
        />

        <PermissionsCard
          targets={targets}
          loading={loadingPermissions}
          searchText={targetSearch}
          onSearchChange={setTargetSearch}
          onToggle={togglePermission}
          onRefresh={loadPermissions}
        />
      </section>
    </AppLayout>
  );
}

function MiniStat({ label, value }) {
  return (
    <div className="bg-white/80 border border-slate-200 rounded-2xl shadow-sm px-3 py-2 min-w-[70px] text-center">
      <p className="text-base font-extrabold text-slate-950 leading-none">
        {value}
      </p>
      <p className="text-[10px] text-slate-400 mt-1">{label}</p>
    </div>
  );
}

function UploadCard({
  fileInputRef,
  selectedFile,
  isUploading,
  onFileChange,
  onClear,
  onUpload,
}) {
  return (
    <Card className="h-full p-5 md:p-5 flex flex-col">
      <CardHeader
        title="העלאת כתב יד"
        subtitle="בחרי קובץ ברור לצורך עיבוד ואבחון."
        icon={<Upload size={20} />}
      />

      <button
        type="button"
        onClick={() => fileInputRef.current?.click()}
        disabled={isUploading}
        className="w-full flex-1 min-h-[235px] border-2 border-dashed border-slate-200 rounded-3xl bg-slate-50 hover:bg-teal-50/50 hover:border-teal-400 transition disabled:opacity-60 flex flex-col items-center justify-center gap-2.5 px-5 text-center"
      >
        <div className="w-12 h-12 rounded-2xl bg-white border border-slate-200 flex items-center justify-center text-teal-700 shadow-sm">
          <Upload size={24} />
        </div>

        <div>
          <p className="font-extrabold text-slate-900 text-sm">
            לחצי לבחירת קובץ כתב יד
          </p>

          <p className="text-xs text-slate-400 mt-1">
            TIF, TIFF, JPG, PNG או PDF
          </p>
        </div>
      </button>

      <input
        ref={fileInputRef}
        type="file"
        className="hidden"
        accept=".tif,.tiff,.jpg,.jpeg,.png,.pdf"
        onChange={onFileChange}
        disabled={isUploading}
      />

      {selectedFile && (
        <div className="mt-4 flex items-center justify-between gap-3 bg-slate-50 border border-slate-200 rounded-2xl px-3 py-2.5">
          <div className="min-w-0">
            <p className="text-[11px] text-slate-400">קובץ נבחר</p>
            <p className="text-xs font-semibold text-slate-800 truncate">
              {selectedFile.name}
            </p>
          </div>

          <button
            type="button"
            onClick={onClear}
            disabled={isUploading}
            className="text-slate-400 hover:text-red-600 transition shrink-0"
          >
            <X size={17} />
          </button>
        </div>
      )}

      <Button
        onClick={onUpload}
        disabled={!selectedFile || isUploading}
        loading={isUploading}
        className="w-full mt-4"
        size="md"
      >
        {!isUploading && <FileText size={18} />}
        {isUploading ? "מבצע אבחון..." : "שליחת קובץ לאבחון"}
      </Button>
    </Card>
  );
}

function ResultCard({ isUploading, analysis, previewUrl }) {
  return (
    <Card className="h-full min-h-[430px] p-5 md:p-5">
      <CardHeader
        title="תוצאת אבחון"
        subtitle="האזור המרכזי להצגת תהליך העיבוד ותוצאת האבחון."
        icon={<FileText size={20} />}
      />

      {isUploading && (
        <BlurredPreviewState
          previewUrl={previewUrl}
          title="המערכת מנתחת את כתב היד..."
          subtitle="מיד לאחר סיום העיבוד תופיע כאן תוצאת האבחון."
          loading
        />
      )}

      {!isUploading && !analysis && previewUrl && (
        <BlurredPreviewState
          previewUrl={previewUrl}
          title="כתב היד מוכן לאבחון"
          subtitle="לחצי על שליחת קובץ לאבחון כדי להתחיל את העיבוד."
        />
      )}

      {!isUploading && !analysis && !previewUrl && (
        <div className="min-h-[295px] rounded-3xl bg-slate-50 border border-slate-100 flex flex-col items-center justify-center p-6 text-center">
          <div className="w-14 h-14 rounded-2xl bg-white border border-slate-200 flex items-center justify-center text-slate-300 mb-4">
            <FileText size={28} />
          </div>

          <p className="text-slate-500 leading-7 max-w-sm text-sm">
            תוצאת האבחון תופיע כאן לאחר בחירת קובץ ושליחה לעיבוד.
          </p>
        </div>
      )}

      {!isUploading && analysis && <AnalysisView analysis={analysis} />}
    </Card>
  );
}

function BlurredPreviewState({ previewUrl, title, subtitle, loading = false }) {
  return (
    <div className="relative min-h-[295px] rounded-3xl overflow-hidden bg-slate-50 border border-slate-100 flex items-center justify-center">
      {previewUrl ? (
        <img
          src={previewUrl}
          alt="כתב היד שנשלח לאבחון"
          className="absolute inset-0 w-full h-full object-contain scale-[1.02]"
          style={{
            filter: "blur(1px)",
            opacity: 0.68,
          }}
        />
      ) : (
        <div className="absolute inset-0 bg-slate-50" />
      )}

      <div className="absolute inset-0 bg-white/35" />

      <div className="relative z-10 text-center px-6 max-w-md">
        <div className="w-16 h-16 rounded-3xl bg-white/90 border border-slate-200 mx-auto mb-4 flex items-center justify-center text-teal-700 shadow-sm">
          {loading ? (
            <Loader2 className="animate-spin" size={30} />
          ) : (
            <FileText size={29} />
          )}
        </div>

        <h3 className="text-lg font-extrabold text-slate-950">{title}</h3>

        <p className="text-slate-500 leading-7 mt-2 text-sm">{subtitle}</p>
      </div>
    </div>
  );
}

function AnalysisView({ analysis }) {
  return (
    <div className="space-y-4">
      <div className="rounded-3xl bg-teal-50 border border-teal-100 p-4">
        <p className="text-xs font-bold text-teal-700 mb-1.5">טיפוס מוביל</p>

        <h3 className="text-2xl font-extrabold text-slate-950">
          {analysis.topPersonalityType}
        </h3>
      </div>

      <div className="rounded-3xl bg-slate-50 border border-slate-100 p-5">
        <p className="text-xs font-bold text-slate-500 mb-2">אבחון</p>

        <div className="max-h-[220px] overflow-y-auto pl-2">
          <p className="text-slate-700 leading-8 whitespace-pre-line text-sm">
            {analysis.diagnosisText || "לא התקבל טקסט אבחון."}
          </p>
        </div>
      </div>
    </div>
  );
}

function HistoryCard({ history, onSelect, onRefresh }) {
  return (
    <Card className="h-[360px] flex flex-col p-5 md:p-5">
      <CardHeader
        title="היסטוריית אבחונים"
        subtitle="רשימת אבחונים קודמים. ניתן לגלול ולפתוח אבחון."
        icon={<Clock size={20} />}
        action={
          <Button variant="secondary" size="sm" onClick={onRefresh}>
            <RefreshCw size={15} />
            רענון
          </Button>
        }
      />

      {history.length === 0 ? (
        <div className="bg-slate-50 border border-slate-100 rounded-3xl p-6 text-center text-slate-500 text-sm">
          אין אבחונים קודמים.
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto rounded-3xl border border-slate-100 divide-y divide-slate-100 bg-slate-50">
          {history.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => onSelect(item)}
              className="w-full text-right px-4 py-3 hover:bg-teal-50/50 transition flex items-center justify-between gap-4"
            >
              <div className="min-w-0">
                <div className="flex items-center gap-2 font-bold text-slate-900 text-sm">
                  <Clock size={15} className="text-slate-400 shrink-0" />
                  <span className="truncate">
                    {item.topPersonalityType || "לא זוהה"}
                  </span>
                </div>

                <p className="text-[11px] text-slate-400 mt-1">
                  {item.analysisDate
                    ? new Date(item.analysisDate).toLocaleString("he-IL")
                    : ""}
                </p>
              </div>

              <span className="text-[11px] font-bold text-teal-700 bg-white border border-teal-100 rounded-full px-2.5 py-1 shrink-0">
                הצגה
              </span>
            </button>
          ))}
        </div>
      )}
    </Card>
  );
}

function PermissionsCard({
  targets,
  loading,
  searchText,
  onSearchChange,
  onToggle,
  onRefresh,
}) {
  const search = searchText.trim().toLowerCase();

  const filteredTargets = targets.filter((target) => {
    if (!search) return true;

    return (
      target.fullName?.toLowerCase().includes(search) ||
      target.email?.toLowerCase().includes(search) ||
      target.roleDescription?.toLowerCase().includes(search)
    );
  });

  return (
    <Card className="h-[360px] overflow-hidden flex flex-col p-5 md:p-5">
      <div className="flex items-start justify-between gap-3 mb-2.5">
        <div className="flex items-center gap-3 min-w-0">
          <div className="w-9 h-9 rounded-2xl bg-teal-50 text-teal-700 flex items-center justify-center shrink-0">
            <ShieldCheck size={18} />
          </div>

          <div className="min-w-0">
            <h2 className="text-lg font-extrabold text-slate-950">
              הרשאות צפייה
            </h2>

            <p className="text-xs text-slate-500 truncate max-w-[300px]">
              בחירת משתמשי יעד שיוכלו לצפות באבחון.
            </p>
          </div>
        </div>

        <Button variant="ghost" size="sm" onClick={onRefresh}>
          <RefreshCw size={15} />
        </Button>
      </div>

      <div className="mb-2.5 flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-3 py-2 focus-within:border-teal-500 focus-within:bg-white transition">
        <Search size={16} className="text-slate-400 shrink-0" />

        <input
          type="text"
          value={searchText}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="חיפוש לפי שם, מייל או תפקיד..."
          className="w-full bg-transparent outline-none text-xs text-slate-800 placeholder:text-slate-400"
        />
      </div>

      {loading && (
        <div className="py-10 flex flex-col items-center justify-center text-slate-500">
          <Loader2 className="animate-spin mb-3" size={26} />
          <p className="text-sm">טוען משתמשי יעד...</p>
        </div>
      )}

      {!loading && targets.length === 0 && (
        <EmptyPermissions text="אין משתמשי יעד פעילים במערכת." />
      )}

      {!loading && targets.length > 0 && filteredTargets.length === 0 && (
        <EmptyPermissions text="לא נמצאו משתמשי יעד מתאימים לחיפוש." />
      )}

      {!loading && filteredTargets.length > 0 && (
        <div className="flex-1 space-y-2 overflow-y-auto pl-1">
          {filteredTargets.map((target) => (
            <div
              key={target.targetUserId}
              className="w-full bg-slate-50 border border-slate-100 rounded-2xl px-3 py-2"
            >
              <div className="flex items-center gap-2.5">
                <div className="w-8 h-8 rounded-xl bg-white border border-slate-200 text-slate-400 flex items-center justify-center shrink-0">
                  <Building2 size={15} />
                </div>

                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <p className="font-bold text-sm text-slate-900 truncate">
                      {target.fullName || "משתמש יעד"}
                    </p>

                    <span
                      className={`text-[10px] font-bold rounded-full px-2 py-0.5 shrink-0 ${
                        target.isAllowed
                          ? "bg-emerald-50 text-emerald-700 border border-emerald-100"
                          : "bg-white text-slate-500 border border-slate-200"
                      }`}
                    >
                      {target.isAllowed ? "מאושר" : "לא מאושר"}
                    </span>
                  </div>

                  <p className="text-[11px] text-slate-500 mt-0.5 truncate">
                    {target.email}
                  </p>

                  {target.roleDescription && (
                    <p className="text-[11px] text-slate-400 mt-0.5 leading-4 truncate">
                      {target.roleDescription}
                    </p>
                  )}
                </div>
              </div>

              <button
                type="button"
                onClick={() => onToggle(target)}
                className={`mt-2 w-full rounded-xl py-1.5 text-xs font-bold transition ${
                  target.isAllowed
                    ? "bg-emerald-50 text-emerald-700 border border-emerald-100 hover:bg-emerald-100"
                    : "bg-white text-slate-600 border border-slate-200 hover:border-teal-300 hover:text-teal-700"
                }`}
              >
                {target.isAllowed ? "הסרת הרשאה" : "מתן הרשאה"}
              </button>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}

function EmptyPermissions({ text }) {
  return (
    <div className="bg-slate-50 border border-slate-100 rounded-3xl p-6 text-center">
      <div className="w-12 h-12 rounded-2xl bg-white border border-slate-200 mx-auto mb-3 flex items-center justify-center text-slate-300">
        <Building2 size={23} />
      </div>

      <p className="text-sm text-slate-500 leading-7">{text}</p>
    </div>
  );
}

function StatusMessage({ type, text }) {
  const success = type === "success";

  return (
    <div
      className={`mb-5 flex items-center gap-3 rounded-3xl border px-5 py-3 text-sm font-medium ${
        success
          ? "bg-emerald-50 border-emerald-200 text-emerald-700"
          : "bg-red-50 border-red-200 text-red-700"
      }`}
    >
      {success ? <CheckCircle size={18} /> : <AlertCircle size={18} />}
      {text}
    </div>
  );
}