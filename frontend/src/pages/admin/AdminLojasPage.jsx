import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useUi } from "../../context/UiContext"
import { lojasAdminService } from "../../services/lojasAdmin.service"
import { Button } from "../../components/ui/Button"
import { Input } from "../../components/ui/Input"
import { Badge } from "../../components/ui/Badge"
import { formatDataHora } from "../../lib/formatters"
import { mensagemPorStatus } from "../../lib/apiErrors"

export default function AdminLojasPage() {
  const { showToast } = useUi()
  const [lojas, setLojas] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [filtros, setFiltros] = useState({ busca: "", ativo: null, aprovado: null, pagina: 1, tamanho: 20 })

  async function carregar() {
    setLoading(true)
    try {
      const r = await lojasAdminService.listar({
        busca: filtros.busca || undefined,
        ativo: filtros.ativo,
        aprovado: filtros.aprovado,
        pagina: filtros.pagina,
        tamanho: filtros.tamanho,
      })
      setLojas(r.items ?? r ?? [])
      setTotal(r.totalCount ?? r.total ?? 0)
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível listar lojas."), "danger")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { carregar() }, [filtros.busca, filtros.ativo, filtros.aprovado, filtros.pagina])

  function handleSuspender(loja) {
    if (!confirm(`Suspender a loja "${loja.nome}"?`)) return
    lojasAdminService
      .suspender(loja.id)
      .then(() => { showToast("Loja suspensa com sucesso.", "success"); carregar() })
      .catch((e) => showToast(mensagemPorStatus(e, "Não foi possível suspender."), "danger"))
  }

  function handleReativar(loja) {
    if (!confirm(`Reativar a loja "${loja.nome}"?`)) return
    lojasAdminService
      .reativar(loja.id)
      .then(() => { showToast("Loja reativada com sucesso.", "success"); carregar() })
      .catch((e) => showToast(mensagemPorStatus(e, "Não foi possível reativar."), "danger"))
  }

  if (loading) return (
    <div className="flex items-center justify-center h-[500px]">
      <div className="h-8 w-48 rounded bg-neutral-200 animate-pulse" />
    </div>
  )

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h1 className="font-display text-2xl font-bold text-neutral-900">Gerenciamento de Lojas</h1>
          <p className="text-neutral-600">Suspenda, reative e acompanhe todas as lojas da plataforma.</p>
        </div>
        <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
          <div className="flex-1 sm:w-64">
            <Input
              placeholder="Buscar por nome da loja ou proprietário"
              value={filtros.busca}
              onChange={(e) => setFiltros({ ...filtros, busca: e.target.value, pagina: 1 })}
            />
          </div>
          <div className="flex-1 sm:w-56">
            <select
              value={filtros.ativo === null ? "" : filtros.ativo ? "1" : "0"}
              onChange={(e) => setFiltros({ ...filtros, ativo: e.target.value === "" ? null : e.target.value === "1", pagina: 1 })}
              className="h-11 px-3 rounded-xl border border-neutral-200 bg-white text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="">Todos</option>
              <option value="1">Ativas</option>
              <option value="0">Suspensas</option>
            </select>
          </div>
          <div className="flex-1 sm:w-56">
            <select
              value={filtros.aprovado === null ? "" : filtros.aprovado ? "1" : "0"}
              onChange={(e) => setFiltros({ ...filtros, aprovado: e.target.value === "" ? null : e.target.value === "1", pagina: 1 })}
              className="h-11 px-3 rounded-xl border border-neutral-200 bg-white text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="">Todos</option>
              <option value="1">Aprovadas</option>
              <option value="0">Não Aprovadas</option>
            </select>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-neutral-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-neutral-200 text-left text-neutral-600">
              <th className="p-3">Loja</th>
              <th className="p-3">Proprietário</th>
              <th className="p-3">Categoria</th>
              <th className="p-3">Produtos</th>
              <th className="p-3">Status</th>
              <th className="p-3">Criação</th>
              <th className="p-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody>
            {lojas.length === 0 ? (
              <tr>
                <td colSpan={7} className="p-6 text-center text-neutral-500">
                  Nenhuma loja encontrada.
                </td>
              </tr>
            ) : (
              lojas.map((l) => (
                <tr key={l.id} className="border-b border-neutral-100 hover:bg-neutral-50 transition-colors">
                  <td className="p-3">
                    <div className="font-semibold">{l.nome}</div>
                    <div className="text-xs text-neutral-500">{l.enderecoComercial} — {l.categoria}</div>
                  </td>
                  <td className="p-3">{l.nomeProprietario}</td>
                  <td className="p-3">{l.categoria}</td>
                  <td className="p-3">{l.totalProdutos}</td>
                  <td className="p-3">
                    {l.ativo ? <Badge tone="success">Ativa</Badge> : <Badge tone="danger">Suspensa</Badge>}
                  </td>
                  <td className="p-3 text-neutral-600">{formatDataHora(l.criadoEmUtc)}</td>
                  <td className="p-3 text-right">
                    {l.ativo ? (
                      <Button size="sm" variant="danger" onClick={() => handleSuspender(l)}>Suspender</Button>
                    ) : (
                      <Button size="sm" variant="success" onClick={() => handleReativar(l)}>Reativar</Button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {lojas.length > 0 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-neutral-600">Exibindo {lojas.length} de {total} lojas</p>
          <div className="flex gap-2">
            <Button size="sm" disabled={filtros.pagina === 1} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina - 1 })}>
              Anterior
            </Button>
            <Button size="sm" disabled={lojas.length < filtros.tamanho} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina + 1 })}>
              Próxima
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

AdminLojasPage.propTypes = {}
