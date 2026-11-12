import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useUi } from "../../context/UiContext"
import { adminService } from "../../services/admin.service"
import { Button } from "../../components/ui/Button"
import { Input } from "../../components/ui/Input"
import { formatDataHora } from "../../lib/formatters"
import { mensagemPorStatus } from "../../lib/apiErrors"

export default function AdminAuditoriaPage() {
  const { showToast } = useUi()
  const [logs, setLogs] = useState([])
  const [loading, setLoading] = useState(true)
  const [buscou, setBuscou] = useState(false)
  const [filtros, setFiltros] = useState({ entidade: "", entidadeId: "", usuarioId: "" })

  async function handleBuscar(e) {
    if (e) e.preventDefault()
    setLoading(true)
    try {
      const r = await adminService.listarAuditLogs({
        entidade: filtros.entidade || undefined,
        entidadeId: filtros.entidadeId || undefined,
        usuarioId: filtros.usuarioId || undefined,
      })
      setLogs(r.items ?? r ?? [])
      setBuscou(true)
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível listar logs."), "danger")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { handleBuscar() }, [])

  if (loading) return (
    <div className="flex items-center justify-center h-[500px]">
      <div className="h-8 w-48 rounded bg-neutral-200 animate-pulse" />
    </div>
  )

  return (
    <div>
      <div className="mb-6">
        <h1 className="font-display text-2xl font-bold text-neutral-900">Auditoria e Logs</h1>
        <p className="text-neutral-600">Registros imutáveis de ações administrativas na plataforma.</p>
      </div>

      <form onSubmit={handleBuscar} className="flex flex-col sm:flex-row gap-2 mb-6 bg-white rounded-xl border border-neutral-200 p-4">
        <Input placeholder="Entidade (ex: Usuario, Loja, Produto)" value={filtros.entidade} onChange={(e) => setFiltros({ ...filtros, entidade: e.target.value })} />
        <Input placeholder="Entidade ID" value={filtros.entidadeId} onChange={(e) => setFiltros({ ...filtros, entidadeId: e.target.value })} />
        <Input placeholder="Usuário ID" value={filtros.usuarioId} onChange={(e) => setFiltros({ ...filtros, usuarioId: e.target.value })} />
        <Button type="submit" loading={loading}>Buscar</Button>
      </form>

      {buscou && logs.length === 0 ? (
        <div className="bg-white rounded-xl border border-neutral-200 p-8 text-center">
          <span className="text-neutral-400">Nenhum registro encontrado para estes filtros.</span>
        </div>
      ) : null}

      {logs.length > 0 ? (
        <div className="bg-white rounded-xl border border-neutral-200 overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-neutral-200 text-left text-neutral-600">
                <th className="p-3">Data/Hora</th>
                <th className="p-3">Usuário</th>
                <th className="p-3">Ação</th>
                <th className="p-3">Entidade</th>
                <th className="p-3">ID da Entidade</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((l, i) => (
                <tr key={l.id ?? i} className="border-b border-neutral-100">
                  <td className="p-3 whitespace-nowrap text-neutral-600">{formatDataHora(l.dataUtc)}</td>
                  <td className="p-3">{l.usuarioId ?? "Sistema"}</td>
                  <td className="p-3"><span className="text-xs font-medium text-primary">{l.acao}</span></td>
                  <td className="p-3 text-neutral-600">{l.entidade}</td>
                  <td className="p-3 font-mono text-xs text-neutral-600">{String(l.entidadeId ?? "").slice(0, 8)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}

AdminAuditoriaPage.propTypes = {}
