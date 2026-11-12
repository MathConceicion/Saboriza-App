import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useUi } from "../../context/UiContext"
import { solicitacoesLojasService } from "../../services/solicitacoesLojas.service"
import { Button } from "../../components/ui/Button"
import { Input } from "../../components/ui/Input"
import { Badge } from "../../components/ui/Badge"
import { Modal } from "../../components/ui/Modal"
import { formatDataHora } from "../../lib/formatters"
import { mensagemPorStatus } from "../../lib/apiErrors"

const STATUS_OPCOES = [
  { valor: "", rotulo: "Todas" },
  { valor: "1", rotulo: "Pendentes" },
  { valor: "4", rotulo: "Em Análise" },
  { valor: "2", rotulo: "Aprovadas" },
  { valor: "3", rotulo: "Recusadas" },
]

function badgeParaStatus(status) {
  const map = {
    1: { tone: "warning", rotulo: "Pendente" },
    4: { tone: "info", rotulo: "Em Análise" },
    2: { tone: "success", rotulo: "Aprovada" },
    3: { tone: "danger", rotulo: "Recusada" },
  }
  return map[status] || { tone: "neutral", rotulo: "—" }
}

export default function AdminSolicitacoesLojasPage() {
  const { showToast } = useUi()
  const [solicitacoes, setSolicitacoes] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [filtros, setFiltros] = useState({ status: "", busca: "", pagina: 1, tamanho: 20 })

  const [modalAberta, setModalAberta] = useState(false)
  const [selecionada, setSelecionada] = useState(null)
  const [acaoModal, setAcaoModal] = useState(null)
  const [motivoRecusa, setMotivoRecusa] = useState("")

  async function carregar() {
    setLoading(true)
    try {
      const r = await solicitacoesLojasService.listar({
        status: filtros.status || undefined,
        busca: filtros.busca || undefined,
        pagina: filtros.pagina,
        tamanho: filtros.tamanho,
      })
      setSolicitacoes(r.items ?? r ?? [])
      setTotal(r.totalCount ?? r.total ?? 0)
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível listar solicitações."), "danger")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { carregar() }, [filtros.status, filtros.busca, filtros.pagina])

  function handleAprovar(solicitacao) {
    setSelecionada(solicitacao)
    setAcaoModal("aprovar")
    setModalAberta(true)
  }

  function handleRecusar(solicitacao) {
    setSelecionada(solicitacao)
    setAcaoModal("recusar")
    setMotivoRecusa("")
    setModalAberta(true)
  }

  function handleAnalise(solicitacao) {
    setSelecionada(solicitacao)
    setAcaoModal("analise")
    setModalAberta(true)
  }

  async function executarAcao() {
    if (!selecionada) return
    try {
      if (acaoModal === "aprovar") {
        await solicitacoesLojasService.aprovar(selecionada.vendedorId)
        showToast("Loja aprovada com sucesso.", "success")
      } else if (acaoModal === "recusar") {
        if (!motivoRecusa.trim()) {
          showToast("Informe o motivo da recusa.", "danger")
          return
        }
        await solicitacoesLojasService.recusar(selecionada.vendedorId, motivoRecusa.trim())
        showToast("Solicitação recusada.", "success")
      } else if (acaoModal === "analise") {
        await solicitacoesLojasService.definirEmAnalise(selecionada.vendedorId)
        showToast("Solicitação enviada para análise.", "success")
      }
      setModalAberta(false)
      carregar()
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível concluir a ação."), "danger")
    }
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
          <h1 className="font-display text-2xl font-bold text-neutral-900">Solicitações de Lojas</h1>
          <p className="text-neutral-600">Aprove, recurse ou encaminhe para análise as solicitações de abertura de lojas.</p>
        </div>
        <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
          <div className="flex-1 sm:w-64">
            <Input
              placeholder="Buscar por nome da loja, proprietário ou e-mail"
              value={filtros.busca}
              onChange={(e) => setFiltros({ ...filtros, busca: e.target.value, pagina: 1 })}
            />
          </div>
          <div className="flex-1 sm:w-56">
            <select
              value={filtros.status}
              onChange={(e) => setFiltros({ ...filtros, status: e.target.value, pagina: 1 })}
              className="h-11 px-3 rounded-xl border border-neutral-200 bg-white text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            >
              {STATUS_OPCOES.map((o) => (
                <option key={o.valor} value={o.valor}>{o.rotulo}</option>
              ))}
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
              <th className="p-3">E-mail</th>
              <th className="p-3">Categoria</th>
              <th className="p-3">Status</th>
              <th className="p-3">Data Solicitação</th>
              <th className="p-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody>
            {solicitacoes.length === 0 ? (
              <tr>
                <td colSpan={7} className="p-6 text-center text-neutral-500">
                  Nenhuma solicitação encontrada.
                </td>
              </tr>
            ) : (
              solicitacoes.map((s) => {
                const badge = badgeParaStatus(s.status.valor)
                return (
                  <tr key={s.vendedorId} className="border-b border-neutral-100 hover:bg-neutral-50 transition-colors">
                    <td className="p-3">
                      <div className="font-semibold">{s.nomeLoja}</div>
                      <div className="text-xs text-neutral-500">{s.enderecoComercial} — {s.cep}</div>
                    </td>
                    <td className="p-3">{s.nomeProprietario}</td>
                    <td className="p-3">{s.emailProprietario}</td>
                    <td className="p-3">{s.categoria}</td>
                    <td className="p-3"><Badge tone={badge.tone}>{badge.rotulo}</Badge></td>
                    <td className="p-3 text-neutral-600">{formatDataHora(s.criadoEmUtc)}</td>
                    <td className="p-3 text-right">
                      {(s.status.valor === 1 || s.status.valor === 4) && (
                        <>
                          <Button size="sm" variant="primary" onClick={() => handleAprovar(s)} className="mr-2">
                            Aprovar
                          </Button>
                          <Button size="sm" variant="danger" onClick={() => handleRecusar(s)}>
                            Recusar
                          </Button>
                        </>
                      )}
                      {s.status.valor === 1 && (
                        <Button size="sm" variant="ghost" onClick={() => handleAnalise(s)} className="ml-2">
                          Análise
                        </Button>
                      )}
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </table>
      </div>

      {solicitacoes.length > 0 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-neutral-600">Exibindo {solicitacoes.length} de {total} solicitações</p>
          <div className="flex gap-2">
            <Button size="sm" disabled={filtros.pagina === 1} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina - 1 })}>
              Anterior
            </Button>
            <Button size="sm" disabled={solicitacoes.length < filtros.tamanho} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina + 1 })}>
              Próxima
            </Button>
          </div>
        </div>
      )}

      <Modal open={modalAberta} onClose={() => setModalAberta(false)} title={acaoModal === "aprovar" ? "Aprovar Loja" : acaoModal === "recusar" ? "Recusar Solicitação" : "Enviar para Análise"}>
        {acaoModal === "aprovar" && (
          <>
            <p className="mb-4">Tem certeza que deseja aprovar a loja <strong>{selecionada?.nomeLoja}</strong>?</p>
            <p className="text-sm text-neutral-600 mb-4">O proprietário receberá uma notificação e poderá acessar o gerenciamento da loja.</p>
            <div className="flex gap-2">
              <Button variant="ghost" onClick={() => setModalAberta(false)}>Cancelar</Button>
              <Button variant="primary" onClick={executarAcao}>Confirmar Aprovação</Button>
            </div>
          </>
        )}
        {acaoModal === "recusar" && (
          <>
            <p className="mb-4">Informe o motivo da recusa para a loja <strong>{selecionada?.nomeLoja}</strong>:</p>
            <Input label="Motivo" value={motivoRecusa} onChange={(e) => setMotivoRecusa(e.target.value)} required />
            <div className="flex gap-2 mt-4">
              <Button variant="ghost" onClick={() => setModalAberta(false)}>Cancelar</Button>
              <Button variant="danger" disabled={!motivoRecusa.trim()} onClick={executarAcao}>Confirmar Recusa</Button>
            </div>
          </>
        )}
        {acaoModal === "analise" && (
          <>
            <p className="mb-4">Tem certeza que deseja encaminhar a loja <strong>{selecionada?.nomeLoja}</strong> para análise?</p>
            <div className="flex gap-2">
              <Button variant="ghost" onClick={() => setModalAberta(false)}>Cancelar</Button>
              <Button variant="primary" onClick={executarAcao}>Confirmar Análise</Button>
            </div>
          </>
        )}
      </Modal>
    </div>
  )
}

AdminSolicitacoesLojasPage.propTypes = {}
