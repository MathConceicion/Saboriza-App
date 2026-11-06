import PropTypes from "prop-types"
import { Button } from "./Button"
import { Icon } from "./Icon"

export function EmptyState({ titulo, descricao, actionLabel, onAction }) {
  return (
    <div className="bg-white rounded-xl border border-neutral-200 p-8 text-center">
      <Icon name="utensils" className="h-10 w-10 mx-auto mb-2 text-primary" />
      <h3 className="font-semibold text-lg">{titulo}</h3>
      {descricao ? <p className="text-sm text-neutral-600 mt-2">{descricao}</p> : null}
      {actionLabel ? <div className="mt-4"><Button onClick={onAction}>{actionLabel}</Button></div> : null}
    </div>
  )
}

EmptyState.propTypes = {
  titulo: PropTypes.string.isRequired,
  descricao: PropTypes.string,
  actionLabel: PropTypes.string,
  onAction: PropTypes.func,
}
