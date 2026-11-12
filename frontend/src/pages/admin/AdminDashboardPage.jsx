import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useAuth } from "../../context/AuthContext"
import { useUi } from "../../context/UiContext"
import { Button } from "../../components/ui/Button"
import { Badge } from "../../components/ui/Badge"
import { Card } from "../../components/ui/Card"
import { Icon } from "../../components/ui/Icon"
import { adminDashboardService } from "../../services/adminDashboard.service"
import { formatPreco } from "../../lib/formatters"

const estatisticas = [
  { titulo: "Total de Usuários", icone: "users", tone: "blue", chave: "totalUsuarios" },
  { titulo: "Total de Lojas", icone: "store", tone: "green", chave: "totalLojas" },
  { titulo: "Lojas Pendentes", icone: "clock", tone: "orange", chave: "lojasAguardandoAprovacao" },
  { titulo: "Lojas Aprovadas", icone: "check", tone: "green", chave: "lojasAprovadas" },
  { titulo: "Total de Produtos", icone: "clipboard", tone: "blue", chave: "totalProdutos" },
  { titulo: "Total de Vendas", icone: "cart", tone: "green", chave: "totalVendas" },
  { titulo: "Faturamento Geral", icone: "credit-card", tone: "red", chave: "faturamentoGeral" },
  { titulo: "Novos Usuários (30d)", icone: "user", tone: "blue", chave: "novosUsuarios30Dias" },
]

export default function AdminDashboardPage() {
  const { user } = useAuth()
  const { showToast } = useUi()
  const [dados, setDados] = useState(null)
  const [crescimento, setCrescimento] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    adminDashboardService
      .estatisticas()
      .then((r) => setDados(r))
      .catch((e) => showToast("Não foi carregar estatísticas.", "danger"))
    adminDashboardService
      .crescimento()
      .then((r) => setCrescimento(r))
      .catch(() => setCrescimento([]))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 h-[600px]">
      {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
        <div key={i} className="bg-white rounded-xl border p-6">
          <div className="h-4 w-3/4 rounded bg-neutral-200 animate-pulse mb-4" />
          <div className="h-3 w-full rounded bg-neutral-200 animate-pulse" />
          <div className="h-3 w-2/3 rounded bg-neutral-200 animate-pulse mt-2" />
        </div>
      ))}
    </div>
  )

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="font-display text-2xl font-bold text-neutral-900">Visão Geral da Plataforma</h1>
          <p className="text-neutral-600">Bem-vindo(a), {user?.nome}. Aqui você gerencia toda a plataforma Saboriza.</p>
        </div>
        <Button variant="ghost" onClick={() => window.location.reload()}>
          <Icon name="clock" className="h-4 w-4 mr-2" /> Atualizar
        </Button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {estatisticas.map((e) => (
          <Card key={e.chave} className="p-5">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-neutral-600">{e.titulo}</p>
                <p className="text-2xl font-bold text-neutral-900 mt-1">
                  {e.chave === "faturamentoGeral" ? formatPreco(dados?.[e.chave] ?? 0) : dados?.[e.chave] ?? "—"}
                </p>
              </div>
              <div className={`p-3 rounded-xl ${e.tone === "blue" ? "bg-blue-100 text-blue-600" : e.tone === "green" ? "bg-green-100 text-green-600" : e.tone === "orange" ? "bg-orange-100 text-orange-600" : "bg-red-100 text-red-600"}`}>
                <Icon name={e.icone} className="h-6 w-6" />
              </div>
            </div>
          </Card>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 bg-white rounded-xl border border-neutral-200 p-6">
          <h2 className="text-lg font-semibold mb-4">Crescimento da Plataforma</h2>
          <div className="h-64 bg-neutral-100 rounded-lg border border-neutral-200">
            <div className="flex items-center justify-center h-full text-neutral-400 text-sm">
              Gráfico de crescimento por período
            </div>
          </div>
        </div>
        <div className="bg-white rounded-xl border border-neutral-200 p-6">
          <h2 className="text-lg font-semibold mb-4">Resumo Rápido</h2>
          <div className="space-y-4">
            <div className="flex items-center justify-between p-3 bg-neutral-50 rounded-lg">
              <div>
                <p className="text-sm text-neutral-600">Lojas aguardando aprovação</p>
                <p className="text-sm font-bold text-orange-600">{dados?.lojasAguardandoAprovacao ?? "—"}</p>
              </div>
              <Button size="sm" variant="primary" fullWidth>Ver solicitações</Button>
            </div>
            <div className="flex items-center justify-between p-3 bg-neutral-50 rounded-lg">
              <div>
                <p className="text-sm text-neutral-600">Faturamento (30 dias)</p>
                <p className="text-sm font-bold text-green-600">{formatPreco(dados?.vendas30Dias ?? 0)}</p>
              </div>
              <Button size="sm" variant="ghost">Ver relatório</Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

AdminDashboardPage.propTypes = {}
