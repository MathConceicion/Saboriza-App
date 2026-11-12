import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useUi } from "../../context/UiContext"
import { notificacoesService } from "../../services/notificacoes.service"
import { Button } from "../../components/ui/Button"
import { Badge } from "../../components/ui/Badge"
import { formatDataHora } from "../../lib/formatters"
import { mensagemPorStatus } from "../../lib/apiErrors"

const TIPO_OPCOES = [
  { valor: 1, rotulo: "Solicitação de Loja", cor: "blue" },
  { valor: 2, rotulo: "Novo Usuário", cor: "green" },
  { valor: 3, rotulo: "Loja Aprovada", cor: "green" },
  { valor: 4, rotulo: "Loja Recusada", cor: "red" },
  { valor: 5, rotulo: "Loja Suspensa", cor: "orange" },
  { valor: 6, rotulo: "Usuário Bloqueado", cor: "red" },
  { valor: 7, rotulo: "Sistema", cor: "neutral" },
]

function tipoLabel(tipo) {
  const t = TIPO_OPCOES.find((x) => x.valor === tipo)
  return t ? t.rotulo : "—"
}

export default function AdminNotificacoesPage() {
  const { showToast } = useUi()
  const [notificacoes, setNotificacoes] = useState([])
  const [naoLidas, setNaoLidas] = useState(0)
  const [loading, setLoading] = useState(true)
  const [apenasNaoLidas, setApenasNaoLidas] = useState(false)

  async function carregar() {
    setLoading(true)
    try {
      const [r, c] = await Promise.all([
        notificacoesService.listar({ apenasNaoLidas, tamanho: 50 }),
        notificacoesService.contarNaoLidas(),
      ])
      setNotificacoes(r.items ?? r ?? [])
      setNaoLidas(c.naoLidas ?? 0)
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível carregar notificações."), "danger")
    } finally {
      setLoading(false)
    }
  }

  async function marcarTodasLidas() {
    await Promise.all(notificacoes.filter((n) => !n.lida).map((n) => notificacoesService.marcarLida(n.id)))
    setNotificacoes(notificacoes.map((n) => ({ ...n, lida: true })))
    setNaoLidas(0)
    showToast("Todas as notificações foram marcadas como lidas.", "success")
  }

  async function marcarLida(id) {
    try {
      await notificacoesService.marcarLida(id)
      setNotificacoes(notificacoes.map((n) => (n.id === id ? { ...n, lida: true } : n)))
      setNaoLidas((prev) => Math.max(0, prev - 1))
    } catch {}
  }

  useEffect(() => { carregar() }, [apenasNaoLidas])

  if (loading) return (
    <div className="flex items-center justify-center h-[500px]">
      <div className="h-8 w-48 rounded bg-neutral-200 animate-pulse" />
    </div>
  )

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h1 className="font-display text-2xl font-bold text-neutral-900">Central de Notificações</h1>
          <p className="text-neutral-600">Acompanhe eventos administrativos da plataforma Saboriza.</p>
        </div>
        <div className="flex gap-2">
          <Button variant={naoLidas > 0 ? "danger" : "ghost"} size="sm" onClick={marcarTodasLidas} disabled={naoLidas === 0}>
            Marcar todas como lidas ({naoLidas})
          </Button>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-neutral-200 overflow-hidden">
        <div className="border-b border-neutral-200 p-3 flex items-center justify-between">
          <label className="flex items-center gap-2 text-sm cursor-pointer select-none">
            <input
              type="checkbox"
              checked={apenasNaoLidas}
              onChange={(e) => setApenasNaoLidas(e.target.checked)}
              className="rounded text-primary focus:ring-primary"
            />
            Apenas não lidas
          </label>
        </div>
        <div className="divide-y divide-neutral-100">
          {notificacoes.length === 0 ? (
            <div className="p-6 text-center text-neutral-500">
              Nenhuma notificação encontrada.
            </div>
          ) : (
            notificacoes.map((n) => (
              <div
                key={n.id}
                onClick={() => marcarLida(n.id)}
                className={`p-4 hover:bg-neutral-50 transition-colors cursor-pointer border-b border-neutral-100 last:border-0 ${!n.lida ? "bg-blue-50" : "bg-white"}`}
              >
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-semibold text-primary">{tipoLabel(n.tipo)}</span>
                  <span className="text-xs text-neutral-400">{formatDataHora(n.criadoEmUtc)}</span>
                  {!n.lida && <span className="h-2 w-2 rounded-full bg-danger" />}
                </div>
                <div className="flex items-center gap-2 mb-1">
                  <h3 className="text-sm font-medium text-neutral-900">{n.titulo}</h3>
                  {n.url && (
                    <Button size="sm" variant="ghost" onClick={(e) => { e.stopPropagation(); window.location.href = n.url }} className="text-primary text-xs font-semibold">
                      Ver detalhes
                    </Button>
                  )}
                </div>
                <p className="text-sm text-neutral-600 line-clamp-2">{n.mensagem}</p>
              </div>
            ))
          )}
        </div>
      </div>

      {notificacoes.length > 0 && (
        <div className="mt-4 text-sm text-neutral-600">
          {naoLidas > 0 ? `${naoLidas} notificação(s) não lida(s)` : "Todas as notificações estão lidas."}
        </div>
      )}
    </div>
  )
}

AdminNotificacoesPage.propTypes = {}
