import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useNavigate } from "react-router-dom"
import { pedidosService } from "../services/pedidos.service"
import { agendaService } from "../services/agenda.service"
import { useUi } from "../context/UiContext"
import { Button } from "../components/ui/Button"
import { Badge } from "../components/ui/Badge"
import { Skeleton } from "../components/ui/Skeleton"
import { EmptyState } from "../components/ui/EmptyState"
import { Sheet } from "../components/ui/Sheet"
import { BackButton } from "../components/ui/BackButton"
import { formatPreco, formatDataHora } from "../lib/formatters"
import { mensagemPorStatus } from "../lib/apiErrors"

const abas = [
  { id: "aprovar", titulo: "A aprovar" },
  { id: "historico", titulo: "Histórico" },
]

const statusOpcoes = ["Todos", "EmPreparo", "ProntoParaRetirada", "Finalizado"]

const statusMeta = {
  EmPreparo: { tone: "warning", rotulo: "Em preparo" },
  ProntoParaRetirada: { tone: "info", rotulo: "Pronto p/ retirada" },
  Finalizado: { tone: "success", rotulo: "Finalizado" },
}

function formatoDia(d) {
  return d.toISOString().slice(0, 10)
}

export default function PedidosVendedorPage() {
  const navigate = useNavigate()
  const [aba, setAba] = useState("aprovar")
  return (
    <div>
      <BackButton onClick={() => navigate("/vendedor/dashboard")}>Voltar</BackButton>
      <h1 className="font-display text-2xl font-bold">Pedidos</h1>
      <p className="text-sm text-neutral-600 mt-1">Aprove novos pedidos e consulte o histórico com valores.</p>
      <div className="flex gap-6 border-b border-neutral-200 mt-6 mb-8">
        {abas.map((a) => (
          <button
            key={a.id}
            type="button"
            onClick={() => setAba(a.id)}
            className={["pb-2 text-sm font-semibold", aba === a.id ? "text-primary border-b-2 border-primary" : "text-neutral-400"].join(" ")}
          >
            {a.titulo}
          </button>
        ))}
      </div>
      {aba === "aprovar" ? <AbaAprovar /> : <AbaHistorico />}
    </div>
  )
}

function AbaAprovar() {
  const { showToast } = useUi()
  const [pendentes, setPendentes] = useState([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)
  const [aprovandoId, setAprovandoId] = useState(null)

  function carregar() {
    setLoading(true)
    setErro(null)
    pedidosService
      .listarPendentes()
      .then((r) => setPendentes(r.items ?? r ?? []))
      .catch((e) => {
        setPendentes([])
        setErro(mensagemPorStatus(e, "Não foi possível carregar os pedidos."))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [])

  async function handleAprovar(pedidoId) {
    setAprovandoId(pedidoId)
    try {
      await pedidosService.aprovar(pedidoId)
      showToast("Pedido aprovado e enviado à agenda", "success")
      carregar()
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível aprovar."), "danger")
    } finally {
      setAprovandoId(null)
    }
  }

  if (loading) return <Skeleton variant="card" />
  if (erro) {
    return (
      <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4 flex items-center justify-between">
        <span>{erro}</span>
        <Button size="sm" variant="ghost" onClick={carregar}>Tentar novamente</Button>
      </div>
    )
  }
  if (pendentes.length === 0) return <EmptyState titulo="Nenhum pedido pendente" descricao="Novos pedidos dos clientes aparecem aqui." />
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {pendentes.map((p) => (
        <div key={p.pedidoId} className="bg-white rounded-xl border border-neutral-200 p-4">
          <div className="flex items-center justify-between mb-2">
            <strong>{p.clienteNome}</strong>
            {p.pixSimulado ? <Badge tone="success">Pago (PIX)</Badge> : <Badge tone="warning">Aguardando pagto</Badge>}
          </div>
          <p className="text-sm text-neutral-600">{(p.itensResumo ?? []).join(", ")}</p>
          <div className="flex items-center justify-between mt-2 text-sm">
            <span className="font-bold text-primary">{formatPreco(p.valorBruto)}</span>
            <span className="text-xs text-neutral-500">{formatDataHora(p.criadoEmUtc)}</span>
          </div>
          <p className="font-mono text-xs text-neutral-500 mt-1">{String(p.pedidoId).slice(0, 8).toUpperCase()}</p>
          <div className="mt-3">
            <Button size="sm" fullWidth loading={aprovandoId === p.pedidoId} onClick={() => handleAprovar(p.pedidoId)}>Aprovar</Button>
          </div>
        </div>
      ))}
    </div>
  )
}

function FiltrosPedidos({ status, onStatus, data, onData, onAplicar, onLimpar }) {
  return (
    <div className="flex flex-col gap-4">
      <label className="flex flex-col gap-1 text-sm">
        <span className="font-medium">Status</span>
        <select value={status} onChange={(e) => onStatus(e.target.value)} className="h-11 rounded-xl border border-neutral-200 bg-white px-3">
          {statusOpcoes.map((s) => (
            <option key={s} value={s}>{s === "Todos" ? "Todos" : (statusMeta[s]?.rotulo ?? s)}</option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span className="font-medium">Dia</span>
        <input type="date" value={data} onChange={(e) => onData(e.target.value)} className="h-11 rounded-xl border border-neutral-200 bg-white px-3" />
      </label>
      <div className="flex gap-2 mt-2">
        <Button variant="ghost" fullWidth onClick={onLimpar}>Limpar</Button>
        <Button fullWidth onClick={onAplicar}>Aplicar</Button>
      </div>
    </div>
  )
}

FiltrosPedidos.propTypes = {
  status: PropTypes.string.isRequired,
  onStatus: PropTypes.func.isRequired,
  data: PropTypes.string.isRequired,
  onData: PropTypes.func.isRequired,
  onAplicar: PropTypes.func.isRequired,
  onLimpar: PropTypes.func.isRequired,
}

function AbaHistorico() {
  const { showToast } = useUi()
  const [itens, setItens] = useState([])
  const [loading, setLoading] = useState(true)
  const [busca, setBusca] = useState("")
  const [buscaAplicada, setBuscaAplicada] = useState("")
  const [status, setStatus] = useState("Todos")
  const [data, setData] = useState(() => formatoDia(new Date()))
  const [filtrosAbertos, setFiltrosAbertos] = useState(false)

  function carregar(statusAtual, dataAtual) {
    setLoading(true)
    agendaService
      .listarDoDia({ data: dataAtual, status: statusAtual === "Todos" ? undefined : statusAtual })
      .then((r) => setItens(r.items ?? r ?? []))
      .catch((e) => {
        setItens([])
        showToast(mensagemPorStatus(e, "Não foi possível carregar o histórico."), "danger")
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar("Todos", formatoDia(new Date()))
  }, [])

  function handleAplicarFiltros() {
    setFiltrosAbertos(false)
    carregar(status, data)
  }

  function handleLimparFiltros() {
    const hoje = formatoDia(new Date())
    setStatus("Todos")
    setData(hoje)
    setBusca("")
    setBuscaAplicada("")
    setFiltrosAbertos(false)
    carregar("Todos", hoje)
  }

  function handleBuscar(e) {
    if (e) e.preventDefault()
    setBuscaAplicada(busca.trim().toLowerCase())
  }

  const filtrados = itens.filter((p) => {
    if (!buscaAplicada) return true
    return String(p.pedidoId ?? "").toLowerCase().includes(buscaAplicada)
  })
  const comFiltro = status !== "Todos" || buscaAplicada !== ""

  return (
    <div>
      <h2 className="text-xl font-bold">Histórico</h2>
      <p className="text-sm text-neutral-600 mt-1">Lista de pedidos com valores e status.</p>
      <form onSubmit={handleBuscar} className="flex flex-col sm:flex-row gap-2 mt-4 mb-4">
        <label className="text-sm flex flex-col gap-1 flex-1">
          <span className="font-medium">Número do pedido</span>
          <input
            value={busca}
            onChange={(e) => setBusca(e.target.value)}
            placeholder="Buscar número do pedido"
            className="h-11 rounded-xl border border-neutral-200 bg-white px-3"
          />
        </label>
        <div className="flex gap-2 sm:items-end">
          <Button type="submit" variant="secondary">Buscar</Button>
          <Button type="button" variant="secondary" onClick={() => setFiltrosAbertos(true)}>
            Mais filtros{comFiltro ? " •" : ""}
          </Button>
        </div>
      </form>
      {loading ? <Skeleton variant="card" /> : null}
      {!loading && filtrados.length === 0 ? <EmptyState titulo="Nenhum pedido" descricao="Ajuste a busca ou os filtros." /> : null}
      {!loading && filtrados.length > 0 ? (
        <div className="bg-white rounded-xl border border-neutral-200 overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-neutral-200 text-left">
                <th className="p-3">Pedido</th>
                <th className="p-3">Status</th>
                <th className="p-3">Cliente</th>
                <th className="p-3">Itens</th>
                <th className="p-3">Valor</th>
                <th className="p-3">Aprovado em</th>
              </tr>
            </thead>
            <tbody>
              {filtrados.map((p) => {
                const meta = statusMeta[p.status] ?? { tone: "neutral", rotulo: p.status }
                return (
                  <tr key={p.agendaId ?? p.pedidoId} className="border-b border-neutral-100">
                    <td className="p-3 font-mono text-xs">{String(p.pedidoId ?? "").slice(0, 8).toUpperCase()}</td>
                    <td className="p-3"><Badge tone={meta.tone}>{meta.rotulo}</Badge></td>
                    <td className="p-3">{p.clienteNome}</td>
                    <td className="p-3 text-neutral-600">{(p.itensResumo ?? []).join(", ")}</td>
                    <td className="p-3 font-semibold whitespace-nowrap">{formatPreco(p.valorBruto ?? 0)}</td>
                    <td className="p-3 whitespace-nowrap text-neutral-600">{p.aprovadoEmUtc ? formatDataHora(p.aprovadoEmUtc) : "-"}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      ) : null}
      <Sheet open={filtrosAbertos} onClose={() => setFiltrosAbertos(false)} side="right" title="Filtrar pedidos">
        <FiltrosPedidos
          status={status}
          onStatus={setStatus}
          data={data}
          onData={setData}
          onAplicar={handleAplicarFiltros}
          onLimpar={handleLimparFiltros}
        />
      </Sheet>
    </div>
  )
}
