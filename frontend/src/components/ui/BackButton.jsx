import PropTypes from "prop-types"
import { Icon } from "./Icon"

export function BackButton({ onClick, children }) {
  return (
    <button onClick={onClick} className="inline-flex items-center gap-1 text-primary font-semibold mb-4">
      <Icon name="arrowLeft" className="h-4 w-4" />
      {children ?? "Voltar"}
    </button>
  )
}

BackButton.propTypes = {
  onClick: PropTypes.func.isRequired,
  children: PropTypes.node,
}
