import PropTypes from "prop-types"

const toneClasses = {
  neutral: "bg-neutral-100 text-neutral-600 border-neutral-200",
  success: "bg-success/10 text-success border-success",
  warning: "bg-warning/10 text-warning border-warning",
  danger: "bg-danger/10 text-danger border-danger",
  info: "bg-info/10 text-info border-info",
}

export function Badge({ tone = "neutral", children, className }) {
  return (
    <span className={["inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold", toneClasses[tone], className ?? ""].join(" ")}>
      {children}
    </span>
  )
}

Badge.propTypes = {
  tone: PropTypes.oneOf(["neutral", "success", "warning", "danger", "info"]),
  children: PropTypes.node.isRequired,
  className: PropTypes.string,
}
