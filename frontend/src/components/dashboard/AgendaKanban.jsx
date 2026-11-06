import PropTypes from "prop-types"
import { Button } from "../ui/Button"
import { Badge } from "../ui/Badge"
import { formatPreco, formatDataHora } from "../../lib/formatters"

const colunas = [
  { status: 2, titulo: "Em Preparo", tone: "warning", proximo: "ProntoParaRetirada", acao: "Marcar pronto" },
  { status: 3, titulo: "Pronto p/ Retirada", tone: "info", proximo: "Finalizado", acao: "Finalizar" },
  { status: 4, titulo: "Finalizado", tone: "success", proximo: null, acao: null },
]

export function AgendaKanban({ itens, avancandoId, onAvancar }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {colunas.map((col) => {
        const daColuna = itens.filter((i) => i.status === col.status)
        return (
          <div key={col.status} className="bg-neutral-100 rounded-xl p-3">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold">{col.titulo}</h3>
              <Badge tone={col.tone}>{String(daColuna.length)}</Badge>
            </div>
            <div className="flex flex-col gap-2">
              {daColuna.map((item) => (
                <div key={item.agendaId} className="bg-white rounded-xl border border-neutral-200 p-3">
                  <div className="flex items-center justify-between text-sm">
                    <strong>#{item.posicaoFila} {item.clienteNome}</strong>
                    <span className="font-mono text-xs text-neutral-500">{String(item.pedidoId).slice(0, 8).toUpperCase()}</span>
                  </div>
                  <p className="text-sm text-neutral-600 mt-1">{(item.itensResumo ?? []).join(", ")}</p>
                  <div className="flex items-center justify-between mt-2 text-sm">
                    <span className="font-bold text-primary">{formatPreco(item.valorBruto)}</span>
                    <span className="text-xs text-neutral-500">{formatDataHora(item.aprovadoEmUtc)}</span>
                  </div>
                  {col.proximo ? (
                    <div className="mt-2">
                      <Button size="sm" fullWidth loading={avancandoId === item.agendaId} onClick={() => onAvancar(item.agendaId, col.proximo)}>{col.acao}</Button>
                    </div>
                  ) : null}
                </div>
              ))}
              {daColuna.length === 0 ? <p className="text-sm text-neutral-500 text-center py-4">Nada aqui</p> : null}
            </div>
          </div>
        )
      })}
    </div>
  )
}

AgendaKanban.propTypes = {
  itens: PropTypes.arrayOf(
    PropTypes.shape({
      agendaId: PropTypes.string.isRequired,
      pedidoId: PropTypes.string.isRequired,
      clienteNome: PropTypes.string,
      aprovadoEmUtc: PropTypes.string.isRequired,
      status: PropTypes.number.isRequired,
      itensResumo: PropTypes.arrayOf(PropTypes.string),
      valorBruto: PropTypes.number.isRequired,
      posicaoFila: PropTypes.number.isRequired,
    })
  ).isRequired,
  avancandoId: PropTypes.string,
  onAvancar: PropTypes.func.isRequired,
}
