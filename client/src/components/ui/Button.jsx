import React from "react";
import { Loader2 } from "lucide-react";

export default function Button({
  children,
  variant = "primary",
  size = "md",
  loading = false,
  disabled = false,
  className = "",
  type = "button",
  ...props
}) {
  const base =
    "inline-flex items-center justify-center gap-2 rounded-2xl font-semibold transition duration-200 focus:outline-none focus:ring-4 focus:ring-teal-500/20 disabled:opacity-60 disabled:cursor-not-allowed";

  const variants = {
    primary:
      "bg-slate-900 text-white hover:bg-slate-800 shadow-lg shadow-slate-300/40",
    secondary:
      "bg-white text-slate-700 border border-slate-200 hover:border-teal-400 hover:text-teal-700",
    soft:
      "bg-teal-50 text-teal-700 hover:bg-teal-100",
    danger:
      "bg-red-50 text-red-700 hover:bg-red-100 border border-red-100",
    ghost:
      "bg-transparent text-slate-500 hover:bg-slate-100 hover:text-slate-900",
  };

  const sizes = {
    sm: "px-4 py-2 text-sm",
    md: "px-6 py-3 text-sm",
    lg: "px-8 py-4 text-base",
  };

  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={`${base} ${variants[variant]} ${sizes[size]} ${className}`}
      {...props}
    >
      {loading && <Loader2 className="animate-spin" size={18} />}
      {children}
    </button>
  );
}