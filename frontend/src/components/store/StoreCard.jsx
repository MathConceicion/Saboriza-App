import PropTypes from "prop-types"
import { formatDistancia } from "../../lib/formatters"
import { Icon } from "../ui/Icon"

export function StoreCard({ fotoUrl, nome, categoria, distanciaKm, avaliacao, endereco, descricao, abertoAgora, horarioAbertura, horarioFechamento, onSelect }) {
  return (
    <button onClick={onSelect} aria-label={`Ver portfólio de ${nome}`} className="bg-white rounded-xl border border-neutral-200 overflow-hidden text-left hover:shadow-md transition-shadow focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary">
      <img src={fotoUrl} alt={nome} loading="lazy" className="h-32 w-full object-cover" />
      <div className="p-4">
        <div className="flex items-center gap-2 flex-wrap">
          <h3 className="font-semibold flex-1">{nome}</h3>
          {abertoAgora === true ? <span className="bg-success/10 text-success text-xs font-bold rounded-full px-2 py-0.5">Aberto agora</span> : null}
          {abertoAgora === false ? <span className="bg-neutral-200 text-neutral-600 text-xs font-bold rounded-full px-2 py-0.5">Fechado</span> : null}
        </div>
        {descricao ? <p className="text-sm text-neutral-600 line-clamp-2 mt-1">{descricao}</p> : null}
        <p className="text-sm text-neutral-600">{categoria}{distanciaKm != null ? ` • ${formatDistancia(distanciaKm)}` : null}</p>
        {horarioAbertura && horarioFechamento ? <p className="text-xs text-neutral-500 mt-0.5">{horarioAbertura} às {horarioFechamento}</p> : null}
        {avaliacao != null ? <p className="text-sm mt-1 inline-flex items-center gap-1"><Icon name="star" className="h-4 w-4 text-accent-dark" />{avaliacao}</p> : null}
        {endereco ? <p className="text-sm text-neutral-600 mt-1">{endereco}</p> : null}
      </div>
    </button>
  )
}

StoreCard.propTypes = {
  fotoUrl: PropTypes.string.isRequired,
  nome: PropTypes.string.isRequired,
  categoria: PropTypes.string,
  distanciaKm: PropTypes.number,
  avaliacao: PropTypes.number,
  endereco: PropTypes.string,
  descricao: PropTypes.string,
  abertoAgora: PropTypes.bool,
  horarioAbertura: PropTypes.string,
  horarioFechamento: PropTypes.string,
  onSelect: PropTypes.func.isRequired,
}
