import PropTypes from "prop-types"
import { forwardRef } from "react"

const variantClasses = {
  primary: "bg-primary text-white hover:bg-primary-dark focus-visible:ring-primary disabled:bg-neutral-300",
  secondary: "bg-secondary text-primary border border-primary hover:bg-secondary-dark",
  ghost: "bg-transparent text-primary hover:bg-primary/10",
  danger: "bg-danger text-white hover:bg-danger-dark",
}

const sizeClasses = {
  sm: "h-9 px-3 text-sm",
  md: "h-11 px-5 text-base",
  lg: "h-12 px-6 text-lg",
}

export const Button = forwardRef(function Button(
  { variant = "primary", size = "md", loading, fullWidth, className, children, disabled, ...rest },
  ref
) {
  return (
    <button
      ref={ref}
      disabled={disabled || loading}
      className={[
        "inline-flex items-center justify-center rounded-xl font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2",
        variantClasses[variant],
        sizeClasses[size],
        fullWidth ? "w-full" : "",
        className ?? "",
      ].join(" ")}
      {...rest}
    >
      {loading ? <span className="animate-pulse">Carregando...</span> : children}
    </button>
  )
})

Button.propTypes = {
  variant: PropTypes.oneOf(["primary", "secondary", "ghost", "danger"]),
  size: PropTypes.oneOf(["sm", "md", "lg"]),
  loading: PropTypes.bool,
  fullWidth: PropTypes.bool,
  className: PropTypes.string,
  children: PropTypes.node,
  disabled: PropTypes.bool,
}

Button.displayName = "Button"
