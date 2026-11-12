import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useUi } from "../../context/UiContext"
import { produtosAdminService } from "../../services/produtosAdmin.service"
import { Button } from "../../components/ui/Button"
import { Input } from "../../components/ui/Input"
import { Badge } from "../../components/ui/Badge"
import { formatPreco } from "../../lib/formatters"
import { mensagemPorStatus } from "../../lib/apiErrors"

export default function AdminProdutosPage() {
  const { showToast } = useUi()
  const [produtos, setProdutos] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [filtros, setFiltros] = useState({ busca: "", lojaId: "", ativo: null, aprovado: null, pagina: 1, tamanho: 20 })

  async function carregar() {
    setLoading(true)
    try {
      const r = await produtosAdminService.listar({
        busca: filtros.busca || undefined,
        lojaId: filtros.lojaId || undefined,
        ativo: filtros.ativo,
        aprovado: filtros.aprovado,
        pagina: filtros.pagina,
        tamanho: filtros.tamanho,
      })
      setProdutos(r.items ?? r ?? [])
      setTotal(r.totalCount ?? r.total ?? 0)
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível listar produtos."), "danger")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { carregar() }, [filtros.busca, filtros.lojaId, filtros.ativo, filtros.aprovado, filtros.pagina])

  function handleDesativar(produto) {
    if (!confirm(`Desativar o produto "${produto.nome}"?`)) return
    produtosAdminService
      .desativar(produto.id)
      .then(() => { showToast("Produto desativado com sucesso.", "success"); carregar() })
      .catch((e) => showToast(mensagemPorStatus(e, "Não foi possível desativar."), "danger"))
  }

  function handleAtivar(produto) {
    if (!confirm(`Ativar o produto "${produto.nome}"?`)) return
    produtosAdminService
      .ativar(produto.id)
      .then(() => { showToast("Produto ativado com sucesso.", "success"); carregar() })
      .catch((e) => showToast(mensagemPorStatus(e, "Não foi possível ativar."), "danger"))
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
          <h1 className="font-display text-2xl font-bold text-neutral-900">Gerenciamento de Produtos</h1>
          <p className="text-neutral-600">Acompanhe e controle todos os produtos cadastrados em todas as lojas.</p>
        </div>
        <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
          <div className="flex-1 sm:w-64">
            <Input
              placeholder="Buscar por nome do produto"
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
              <option value="1">Ativos</option>
              <option value="0">Inativos</option>
            </select>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-neutral-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-neutral-200 text-left text-neutral-600">
              <th className="p-3">Produto</th>
              <th className="p-3">Loja</th>
              <th className="p-3">Preço</th>
              <th className="p-3">Estoque</th>
              <th className="p-3">Status</th>
              <th className="p-3">Criação</th>
              <th className="p-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody>
            {produtos.length === 0 ? (
              <tr>
                <td colSpan={7} className="p-6 text-center text-neutral-500">
                  Nenhum produto encontrado.
                </td>
              </tr>
            ) : (
              produtos.map((p) => (
                <tr key={p.id} className="border-b border-neutral-100 hover:bg-neutral-50 transition-colors">
                  <td className="p-3">
                    <div className="font-semibold">{p.nome}</div>
                    <div className="text-xs text-neutral-500">{p.descricao ? p.descricao.slice(0, 60) + (p.descricao.length > 60 ? "..." : "") : "—"}</div>
                  </td>
                  <td className="p-3">{p.lojaNome}</td>
                  <td className="p-3">{formatPreco(p.preco)}</td>
                  <td className="p-3">{p.quantidadeEmEstoque}</td>
                  <td className="p-3">
                    {p.ativo ? <Badge tone="success">Ativo</Badge> : <Badge tone="danger">Inativo</Badge>}
                  </td>
                  <td className="p-3 text-neutral-600">{p.criadoEmUtc ? new Date(p.criadoEmUtc).toLocaleDateString("pt-BR") : "—"}</td>
                  <td className="p-3 text-right">
                    {p.ativo ? (
                      <Button size="sm" variant="danger" onClick={() => handleDesativar(p)}>Desativar</Button>
                    ) : (
                      <Button size="sm" variant="success" onClick={() => handleAtivar(p)}>Ativar</Button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {produtos.length > 0 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-neutral-600">Exibindo {produtos.length} de {total} produtos</p>
          <div className="flex gap-2">
            <Button size="sm" disabled={filtros.pagina === 1} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina - 1 })}>
              Anterior
            </Button>
            <Button size="sm" disabled={produtos.length < filtros.tamanho} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina + 1 })}>
              Próxima
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

AdminProdutosPage.propTypes = {}
