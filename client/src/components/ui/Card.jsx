import React from "react";

export default function Card({
  children,
  className = "",
  padded = true,
  hover = false,
}) {
  return (
    <section
      className={[
        "bg-white border border-slate-200 rounded-3xl shadow-sm",
        "transition duration-200",
        padded ? "p-6 md:p-8" : "",
        hover ? "hover:shadow-lg hover:-translate-y-0.5" : "",
        className,
      ].join(" ")}
    >
      {children}
    </section>
  );
}

export function CardHeader({ title, subtitle, icon, action }) {
  return (
    <div className="flex items-start justify-between gap-4 mb-6">
      <div>
        <h2 className="text-xl font-extrabold text-slate-950">{title}</h2>

        {subtitle && (
          <p className="text-sm text-slate-500 mt-1 leading-6">
            {subtitle}
          </p>
        )}
      </div>

      <div className="flex items-center gap-3">
        {action && <div>{action}</div>}

        {icon && (
          <div className="w-12 h-12 rounded-2xl bg-teal-50 text-teal-700 flex items-center justify-center shrink-0">
            {icon}
          </div>
        )}
      </div>
    </div>
  );
}

export function CardBody({ children, className = "" }) {
  return <div className={className}>{children}</div>;
}

export function CardFooter({ children, className = "" }) {
  return (
    <div className={`mt-6 pt-5 border-t border-slate-100 ${className}`}>
      {children}
    </div>
  );
}