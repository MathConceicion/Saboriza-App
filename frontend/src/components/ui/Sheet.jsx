import PropTypes from "prop-types"

const sideClasses = {
  right: "right-0 top-0 h-full w-full max-w-md",
  bottom: "bottom-0 left-0 w-full max-h-[85vh] rounded-t-2xl",
}

export function Sheet({ open, onClose, side = "right", title, children }) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label={title}>
      <button aria-label="Fechar" onClick={onClose} className="absolute inset-0 bg-neutral-900/50" />
      <div className={["absolute bg-white shadow-lg p-4 overflow-y-auto", sideClasses[side]].join(" ")}>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">{title}</h2>
          <button onClick={onClose} aria-label="Fechar painel" className="text-neutral-600 hover:text-primary font-bold px-2">X</button>
        </div>
        {children}
      </div>
    </div>
  )
}

Sheet.propTypes = {
  open: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  side: PropTypes.oneOf(["right", "bottom"]),
  title: PropTypes.string.isRequired,
  children: PropTypes.node.isRequired,
}
