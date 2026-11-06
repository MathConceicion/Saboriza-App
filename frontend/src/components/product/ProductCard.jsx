import PropTypes from "prop-types"
import { Button } from "../ui/Button"
import { formatPreco } from "../../lib/formatters"

export function ProductCard({ id, nome, descricao, preco, imagemUrl, onAdd }) {
  return (
    <div className="bg-white rounded-xl border border-neutral-200 overflow-hidden flex flex-col hover:shadow-lg transition-shadow">
      <img src={imagemUrl} alt={nome} loading="lazy" className="h-40 w-full object-cover" />
      <div className="p-4 flex flex-col gap-2 flex-1">
        <span className="text-xs font-mono text-neutral-500">{String(id).slice(0, 8).toUpperCase()}</span>
        <h3 className="font-semibold leading-tight">{nome}</h3>
        <p className="text-sm text-neutral-600 line-clamp-2">{descricao}</p>
        <div className="mt-auto flex items-center justify-between pt-2">
          <span className="text-lg font-bold text-primary">{formatPreco(preco)}</span>
          <Button size="sm" onClick={() => onAdd(id)}>Adicionar</Button>
        </div>
      </div>
    </div>
  )
}

ProductCard.propTypes = {
  id: PropTypes.string.isRequired,
  nome: PropTypes.string.isRequired,
  descricao: PropTypes.string,
  preco: PropTypes.number.isRequired,
  imagemUrl: PropTypes.string.isRequired,
  onAdd: PropTypes.func.isRequired,
}
