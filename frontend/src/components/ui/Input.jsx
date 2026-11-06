import PropTypes from "prop-types"

export function Input({ label, error, id, ...rest }) {
  const inputId = id ?? `input-${label?.toLowerCase().replace(/\s/g, "-")}`
  return (
    <div className="flex flex-col gap-1">
      {label ? <label htmlFor={inputId} className="text-sm font-medium text-neutral-900">{label}</label> : null}
      <input
        id={inputId}
        className={[
          "h-11 px-3 rounded-xl border bg-white focus:outline-none focus:ring-2 focus:ring-primary",
          error ? "border-danger" : "border-neutral-200",
        ].join(" ")}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? `${inputId}-error` : undefined}
        {...rest}
      />
      {error ? <p id={`${inputId}-error`} className="text-danger text-sm mt-1" aria-live="polite">{error}</p> : null}
    </div>
  )
}

Input.propTypes = {
  label: PropTypes.string,
  error: PropTypes.string,
  id: PropTypes.string,
}
