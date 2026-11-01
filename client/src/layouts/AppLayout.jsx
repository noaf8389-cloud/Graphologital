import React from "react";

export default function AppLayout({ children, className = "" }) {
  return (
    <main className={`app-bg py-10 ${className}`} dir="rtl">
      <div className="page-container">{children}</div>
    </main>
  );
}