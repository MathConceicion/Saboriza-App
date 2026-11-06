import PropTypes from "prop-types"

export function QuantityStepper({ quantidade, onAlterar, min = 1, max = 99 }) {
  return (
    <div className="inline-flex items-center gap-2">
      <button onClick={() => onAlterar(Math.max(min, quantidade - 1))} disabled={quantidade <= min} aria-label="Diminuir quantidade" className="h-8 w-8 rounded-full border border-neutral-200 font-bold disabled:opacity-40 hover:border-primary hover:text-primary">-</button>
      <span aria-live="polite" className="text-sm font-semibold w-6 text-center">{quantidade}</span>
      <button onClick={() => onAlterar(Math.min(max, quantidade + 1))} disabled={quantidade >= max} aria-label="Aumentar quantidade" className="h-8 w-8 rounded-full border border-neutral-200 font-bold disabled:opacity-40 hover:border-primary hover:text-primary">+</button>
    </div>
  )
}

QuantityStepper.propTypes = {
  quantidade: PropTypes.number.isRequired,
  onAlterar: PropTypes.func.isRequired,
  min: PropTypes.number,
  max: PropTypes.number,
}
