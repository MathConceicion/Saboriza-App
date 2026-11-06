import { useEffect, useState } from "react"
import { agendaService } from "../services/agenda.service"
import { KpiCard } from "../components/dashboard/KpiCard"
import { Skeleton } from "../components/ui/Skeleton"
import { formatPreco } from "../lib/formatters"

export default function VendedorDashboardPage() {
  const [itens, setItens] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    agendaService
      .listarDoDia()
      .then((r) => setItens(r.items ?? r ?? []))
      .catch(() => setItens([]))
      .finally(() => setLoading(false))
  }, [])

  const emPreparo = itens.filter((i) => i.status === 2)
  const prontos = itens.filter((i) => i.status === 3)
  const finalizados = itens.filter((i) => i.status === 4)
  const brutoHoje = itens.reduce((acc, i) => acc + (i.valorBruto ?? 0), 0)

  return (
    <div>
      <h2 className="text-xl font-semibold mb-4">Hoje na sua loja</h2>
      {loading ? <Skeleton variant="card" /> : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <KpiCard titulo="Em preparo" valor={String(emPreparo.length)} tone="orange" />
          <KpiCard titulo="Prontos p/ retirada" valor={String(prontos.length)} tone="blue" />
          <KpiCard titulo="Finalizados hoje" valor={String(finalizados.length)} tone="green" />
          <KpiCard titulo="Bruto hoje" valor={formatPreco(brutoHoje)} tone="red" />
        </div>
      )}
    </div>
  )
}
