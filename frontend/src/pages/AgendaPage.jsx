import { useEffect, useState } from "react"
import { agendaService } from "../services/agenda.service"
import { useUi } from "../context/UiContext"
import { AgendaKanban } from "../components/dashboard/AgendaKanban"
import { Skeleton } from "../components/ui/Skeleton"
import { EmptyState } from "../components/ui/EmptyState"
import { Button } from "../components/ui/Button"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function AgendaPage() {
  const { showToast } = useUi()
  const [itens, setItens] = useState([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)
  const [avancandoId, setAvancandoId] = useState(null)

  function carregar() {
    setLoading(true)
    setErro(null)
    agendaService
      .listarDoDia()
      .then((r) => setItens(r.items ?? r ?? []))
      .catch((e) => {
        setItens([])
        setErro(mensagemPorStatus(e, "Não foi possível carregar a agenda."))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [])

  async function handleAvancar(agendaId, novoStatus) {
    setAvancandoId(agendaId)
    try {
      await agendaService.avancar(agendaId, novoStatus)
      showToast("Status atualizado", "success")
      carregar()
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível avançar."), "danger")
    } finally {
      setAvancandoId(null)
    }
  }

  return (
    <div>
      <h2 className="text-xl font-semibold mb-2">Agenda de Produção</h2>
      <p className="text-neutral-600 mb-6">Pedidos de hoje ordenados por horário de aprovação</p>
      {loading ? <Skeleton variant="card" /> : null}
      {!loading && erro ? (
        <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4 flex items-center justify-between">
          <span>{erro}</span>
          <Button size="sm" variant="ghost" onClick={carregar}>Tentar novamente</Button>
        </div>
      ) : null}
      {!loading && !erro && itens.length === 0 ? <EmptyState titulo="Agenda vazia" descricao="Nenhum pedido aprovado hoje ainda." /> : null}
      {!loading && !erro && itens.length > 0 ? <AgendaKanban itens={itens} avancandoId={avancandoId} onAvancar={handleAvancar} /> : null}
    </div>
  )
}
