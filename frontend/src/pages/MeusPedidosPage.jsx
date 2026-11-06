import PropTypes from "prop-types"
import { useEffect, useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { pedidosService } from "../services/pedidos.service"
import { BackButton } from "../components/ui/BackButton"
import { Badge } from "../components/ui/Badge"
import { Button } from "../components/ui/Button"
import { EmptyState } from "../components/ui/EmptyState"
import { Skeleton } from "../components/ui/Skeleton"
import { formatDataHora, formatPreco } from "../lib/formatters"
import { mensagemPorStatus } from "../lib/apiErrors"

const STATUS_INFO = {
  1: { label: "Pendente", tone: "warning" },
  2: { label: "Em preparo", tone: "info" },
  3: { label: "Pronto para retirada", tone: "info" },
  4: { label: "Finalizado", tone: "success" },
  5: { label: "Cancelado", tone: "danger" },
}

function statusInfo(status) {
  return STATUS_INFO[status] ?? { label: "Desconhecido", tone: "neutral" }
}

function estaEmAndamento(pedido) {
  return pedido.status !== 4 && pedido.status !== 5
}

export default function MeusPedidosPage() {
  const navigate = useNavigate()
  const [pedidos, setPedidos] = useState([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)

  function carregar() {
    setLoading(true)
    setErro(null)
    pedidosService
      .listarMeus()
      .then((r) => setPedidos(r ?? []))
      .catch((e) => {
        setPedidos([])
        setErro(mensagemPorStatus(e, "Erro ao carregar pedidos"))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [])

  const emAndamento = pedidos.filter(estaEmAndamento)
  const anteriores = pedidos.filter((p) => !estaEmAndamento(p))

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <BackButton onClick={() => navigate(-1)} />
      <h1 className="text-2xl font-bold mb-6">Meus pedidos</h1>
      {loading ? <Skeleton variant="card" /> : null}
      {!loading && erro ? (
        <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4 flex items-center justify-between">
          <span>{erro}</span>
          <Button size="sm" variant="ghost" onClick={carregar}>Tentar novamente</Button>
        </div>
      ) : null}
      {!loading && !erro && pedidos.length === 0 ? (
        <EmptyState titulo="Nenhum pedido ainda" descricao="Seus pedidos pagos e o histórico aparecem aqui." actionLabel="Ver início" onAction={() => navigate("/")} />
      ) : null}
      {!loading && !erro && emAndamento.length > 0 ? (
        <section className="mb-8">
          <h2 className="text-lg font-semibold mb-3">Em andamento</h2>
          <div className="flex flex-col gap-4">
            {emAndamento.map((p) => (
              <PedidoCard key={p.pedidoId} pedido={p} destaque />
            ))}
          </div>
        </section>
      ) : null}
      {!loading && !erro && anteriores.length > 0 ? (
        <section>
          <h2 className="text-lg font-semibold mb-3">Pedidos anteriores</h2>
          <div className="flex flex-col gap-4">
            {anteriores.map((p) => (
              <PedidoCard key={p.pedidoId} pedido={p} />
            ))}
          </div>
        </section>
      ) : null}
    </div>
  )
}

function PedidoCard({ pedido, destaque }) {
  const info = statusInfo(pedido.status)
  return (
    <article className={["bg-white rounded-2xl border shadow-card p-5", destaque ? "border-primary" : "border-neutral-200"].join(" ")}>
      <div className="flex items-center gap-2 flex-wrap">
        <div className="min-w-0">
          <p className="font-bold">Pedido #{String(pedido.pedidoId).slice(0, 8).toUpperCase()}</p>
          <p className="text-sm text-neutral-600">{pedido.lojaNome} • {formatDataHora(pedido.criadoEmUtc)}</p>
        </div>
        <span className="ml-auto font-bold text-primary whitespace-nowrap">{formatPreco(pedido.valorBruto)}</span>
      </div>
      <div className="flex items-center gap-2 mt-3 flex-wrap">
        <Badge tone={info.tone}>{info.label}</Badge>
        {pedido.pixSimulado ? <Badge tone="success">Pago via PIX</Badge> : <Badge tone="neutral">Pagamento pendente</Badge>}
      </div>
      <ul className="mt-3 pt-3 border-t border-neutral-100 text-sm text-neutral-600 flex flex-col gap-1">
        {(pedido.itensResumo ?? []).map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
      {destaque ? (
        <Link to={`/loja/${pedido.lojaId}`} className="inline-block mt-3 text-sm font-semibold text-primary hover:underline">Ver loja</Link>
      ) : null}
    </article>
  )
}

PedidoCard.propTypes = {
  pedido: PropTypes.shape({
    pedidoId: PropTypes.string.isRequired,
    lojaId: PropTypes.string.isRequired,
    lojaNome: PropTypes.string.isRequired,
    status: PropTypes.number.isRequired,
    valorBruto: PropTypes.number.isRequired,
    criadoEmUtc: PropTypes.string.isRequired,
    pixSimulado: PropTypes.bool.isRequired,
    itensResumo: PropTypes.arrayOf(PropTypes.string),
  }).isRequired,
  destaque: PropTypes.bool,
}
