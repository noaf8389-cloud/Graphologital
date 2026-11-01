import React from "react";

export default function Input({
  label,
  value,
  onChange,
  type = "text",
  placeholder = "",
  icon,
  error,
  disabled = false,
  className = "",
  ...props
}) {
  return (
    <label className={`block ${className}`}>
      {label && (
        <span className="text-xs font-semibold text-slate-500 mb-1.5 block">
          {label}
        </span>
      )}

      <div
        className={`flex items-center gap-2 rounded-2xl border bg-slate-50 px-3 py-3 transition ${
          error
            ? "border-red-300 focus-within:border-red-500"
            : "border-slate-200 focus-within:border-teal-500 focus-within:bg-white focus-within:ring-4 focus-within:ring-teal-500/10"
        } ${disabled ? "opacity-60" : ""}`}
      >
        {icon && <span className="text-slate-400 shrink-0">{icon}</span>}

        <input
          type={type}
          value={value}
          disabled={disabled}
          placeholder={placeholder}
          onChange={(e) => onChange?.(e.target.value)}
          className="w-full bg-transparent outline-none text-sm text-slate-800 placeholder:text-slate-400"
          {...props}
        />
      </div>

      {error && <p className="text-xs text-red-600 mt-1.5">{error}</p>}
    </label>
  );
}