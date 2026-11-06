import PropTypes from "prop-types"

export function Card({ children, className, hoverable, onClick }) {
  return (
    <div
      onClick={onClick}
      className={["bg-white rounded-xl border border-neutral-200 shadow-sm", hoverable ? "hover:shadow-md transition-shadow" : "", className ?? ""].join(" ")}
    >
      {children}
    </div>
  )
}

Card.propTypes = {
  children: PropTypes.node.isRequired,
  className: PropTypes.string,
  hoverable: PropTypes.bool,
  onClick: PropTypes.func,
}
