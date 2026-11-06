import PropTypes from "prop-types"
import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { adminService } from "../services/admin.service"
import { useAuth } from "../context/AuthContext"
import { useUi } from "../context/UiContext"
import { Button } from "../components/ui/Button"
import { Input } from "../components/ui/Input"
import { Badge } from "../components/ui/Badge"
import { Icon } from "../components/ui/Icon"
import { BackButton } from "../components/ui/BackButton"
import { KpiCard } from "../components/dashboard/KpiCard"
import { EmptyState } from "../components/ui/EmptyState"
import { Skeleton } from "../components/ui/Skeleton"
import { formatPreco, formatDataHora } from "../lib/formatters"
import { mensagemPorStatus } from "../lib/apiErrors"

const gruposMenu = [
  {
    rotulo: "Operação",
    itens: [
      { id: "inicio", titulo: "Início", icone: "store" },
      { id: "faturamento", titulo: "Financeiro", icone: "chart" },
    ],
  },
  {
    rotulo: "Moderação",
    itens: [
      { id: "vendedores", titulo: "Vendedores", icone: "user" },
      { id: "produtos", titulo: "Cardápio", icone: "utensils" },
    ],
  },
  {
    rotulo: "Sistema",
    itens: [
      { id: "taxa", titulo: "Taxa", icone: "check" },
      { id: "auditoria", titulo: "Auditoria", icone: "eye" },
    ],
  },
]

const titulos = {
  inicio: "Visão geral",
  faturamento: "Detalhamento do repasse",
  vendedores: "Aprovação de vendedores",
  produtos: "Aprovação do cardápio",
  taxa: "Taxa de comissão",
  auditoria: "Trilha de auditoria",
}

export default function AdminPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [aba, setAba] = useState("faturamento")
  const [resumo, setResumo] = useState(null)
  function carregarResumo() {
    Promise.all([
      adminService.listarVendedores("PendenteAprovacao").catch(() => []),
      adminService.listarProdutos(false).catch(() => []),
      adminService.faturamento({ de: new Date(Date.now() - 30 * 86400000).toISOString(), ate: new Date().toISOString() }).catch(() => null),
    ]).then(([vends, prods, fat]) => {
      const lv = vends.items ?? vends ?? []
      const lp = prods.items ?? prods ?? []
      setResumo({ vendedoresPendentes: lv.length, produtosPendentes: lp.length, faturamento: fat })
    })
  }
  useEffect(() => {
    carregarResumo()
  }, [])
  return (
    <div className="flex items-stretch">
      <aside className="hidden lg:flex w-60 shrink-0 flex-col bg-white border-r border-neutral-200 px-4 py-5">
        <div className="px-2">
          <p className="font-display font-bold text-primary leading-tight">Painel Admin</p>
          <p className="text-xs text-neutral-500 leading-tight mt-1">Moderação do marketplace</p>
        </div>
        <div className="mt-4 flex items-center gap-2 rounded-xl bg-success/10 border border-success/30 px-3 py-2">
          <span className="h-2.5 w-2.5 rounded-full bg-success" />
          <div>
            <p className="text-sm font-bold text-success leading-tight">Moderação ativa</p>
            <p className="text-xs text-neutral-600 leading-tight">Aprovações pendentes no painel</p>
          </div>
        </div>
        <nav className="mt-5 flex flex-col gap-4" aria-label="Menu do admin">
          {gruposMenu.map((g) => (
            <div key={g.rotulo}>
              <p className="px-3 mb-1 text-xs font-bold uppercase tracking-wider text-neutral-400">{g.rotulo}</p>
              <div className="flex flex-col gap-0.5">
                {g.itens.map((m) => (
                  <button
                    key={m.id}
                    onClick={() => setAba(m.id)}
                    className={["flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-semibold transition-colors", aba === m.id ? "bg-primary/10 text-primary border-l-4 border-primary" : "text-neutral-600 hover:bg-neutral-100"].join(" ")}
                  >
                    <Icon name={m.icone} className="h-5 w-5" />
                    {m.titulo}
                    {m.id === "vendedores" && (resumo?.vendedoresPendentes ?? 0) > 0 ? <span className="ml-auto bg-danger text-white text-xs font-bold rounded-full h-5 min-w-5 px-1 flex items-center justify-center">{resumo.vendedoresPendentes}</span> : null}
                    {m.id === "produtos" && (resumo?.produtosPendentes ?? 0) > 0 ? <span className="ml-auto bg-danger text-white text-xs font-bold rounded-full h-5 min-w-5 px-1 flex items-center justify-center">{resumo.produtosPendentes}</span> : null}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </nav>
        <div className="mt-auto pt-6 border-t border-neutral-200">
          <p className="text-sm font-semibold truncate px-2">{user?.email}</p>
          <p className="text-xs text-neutral-500 px-2">Admin logado</p>
        </div>
      </aside>
      <div className="flex-1 min-w-0">
        <nav className="lg:hidden flex gap-2 overflow-x-auto px-4 py-3 border-b border-neutral-200 bg-white" aria-label="Menu do admin">
          {gruposMenu.flatMap((g) => g.itens).map((m) => (
            <button
              key={m.id}
              onClick={() => setAba(m.id)}
              className={["whitespace-nowrap px-4 py-2 rounded-full text-sm font-semibold", aba === m.id ? "bg-primary text-white" : "bg-white border border-neutral-200 text-neutral-600"].join(" ")}
            >
              {m.titulo}
            </button>
          ))}
        </nav>
        <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8">
          <BackButton onClick={() => navigate("/buscar")}>Voltar</BackButton>
          <h1 className="font-display text-2xl font-bold">{titulos[aba]}</h1>
          <div className="mt-8">
            {aba === "inicio" ? <AbaInicio resumo={resumo} /> : null}
            {aba === "faturamento" ? <AbaFaturamento /> : null}
            {aba === "vendedores" ? <AbaVendedores onAprovado={carregarResumo} /> : null}
            {aba === "produtos" ? <AbaProdutos onAprovado={carregarResumo} /> : null}
            {aba === "taxa" ? <AbaTaxa /> : null}
            {aba === "auditoria" ? <AbaAuditoria /> : null}
          </div>
        </div>
      </div>
    </div>
  )
}

function AbaInicio({ resumo }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      <KpiCard titulo="Lojas pendentes" valor={resumo ? String(resumo.vendedoresPendentes) : "..."} tone="orange" />
      <KpiCard titulo="Produtos pendentes" valor={resumo ? String(resumo.produtosPendentes) : "..."} tone="blue" />
      <KpiCard titulo="Bruto 30d" valor={resumo?.faturamento ? formatPreco(resumo.faturamento.faturamentoBrutoTotal ?? 0) : "..."} tone="green" />
      <KpiCard titulo="Comissão 30d" valor={resumo?.faturamento ? formatPreco(resumo.faturamento.comissaoTotalRetida ?? 0) : "..."} tone="red" />
    </div>
  )
}

AbaInicio.propTypes = {
  resumo: PropTypes.shape({
    vendedoresPendentes: PropTypes.number,
    produtosPendentes: PropTypes.number,
    faturamento: PropTypes.object,
  }),
}

function AbaVendedores({ onAprovado }) {
  const { showToast } = useUi()
  const [pendentes, setPendentes] = useState([])
  const [loading, setLoading] = useState(true)
  const [aprovandoId, setAprovandoId] = useState(null)
  function carregar() {
    setLoading(true)
    adminService
      .listarVendedores("PendenteAprovacao")
      .then((r) => setPendentes(r.items ?? r ?? []))
      .catch((e) => showToast(mensagemPorStatus(e, "Não foi possível listar."), "danger"))
      .finally(() => setLoading(false))
  }
  useEffect(() => {
    carregar()
  }, [])
  async function handleAprovar(vendedorId) {
    setAprovandoId(vendedorId)
    try {
      await adminService.aprovarVendedor(vendedorId)
      showToast("Vendedor aprovado e loja criada", "success")
      carregar()
      onAprovado()
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível aprovar."), "danger")
    } finally {
      setAprovandoId(null)
    }
  }
  if (loading) return <Skeleton variant="card" />
  if (pendentes.length === 0) return <EmptyState titulo="Nada pendente" descricao="Nenhum vendedor aguardando aprovação." />
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {pendentes.map((v) => (
        <div key={v.vendedorId} className="bg-white rounded-xl border border-neutral-200 p-4">
          <div className="flex gap-3 mb-2">
            {v.fotoUrl ? <img src={v.fotoUrl} alt={v.nomeLoja} loading="lazy" className="h-16 w-16 rounded-lg object-cover" /> : null}
            <div className="flex-1">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold">{v.nomeLoja}</h3>
                <Badge tone="warning">{v.status}</Badge>
              </div>
              <p className="text-sm text-neutral-600">{v.categoriaNome} • {v.usuarioEmail}</p>
            </div>
          </div>
          <p className="text-sm text-neutral-600">{v.enderecoComercial} — {v.cep}</p>
          <p className="text-sm text-neutral-600">{v.telefone}</p>
          <div className="mt-3">
            <Button size="sm" fullWidth loading={aprovandoId === v.vendedorId} onClick={() => handleAprovar(v.vendedorId)}>Aprovar e criar loja</Button>
          </div>
        </div>
      ))}
    </div>
  )
}

AbaVendedores.propTypes = {
  onAprovado: PropTypes.func.isRequired,
}

function AbaProdutos({ onAprovado }) {
  const { showToast } = useUi()
  const [pendentes, setPendentes] = useState([])
  const [loading, setLoading] = useState(true)
  const [aprovandoId, setAprovandoId] = useState(null)
  function carregar() {
    setLoading(true)
    adminService
      .listarProdutos(false)
      .then((r) => setPendentes(r.items ?? r ?? []))
      .catch((e) => showToast(mensagemPorStatus(e, "Não foi possível listar."), "danger"))
      .finally(() => setLoading(false))
  }
  useEffect(() => {
    carregar()
  }, [])
  async function handleAprovar(produtoId) {
    setAprovandoId(produtoId)
    try {
      await adminService.aprovarProduto(produtoId)
      showToast("Produto aprovado", "success")
      carregar()
      onAprovado()
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível aprovar."), "danger")
    } finally {
      setAprovandoId(null)
    }
  }
  if (loading) return <Skeleton variant="card" />
  if (pendentes.length === 0) return <EmptyState titulo="Nada pendente" descricao="Nenhum produto aguardando aprovação." />
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {pendentes.map((p) => (
        <div key={p.produtoId} className="bg-white rounded-xl border border-neutral-200 p-4">
          <div className="flex items-center justify-between">
            <h3 className="font-semibold">{p.nome}</h3>
            <Badge tone="warning">Pendente</Badge>
          </div>
          <p className="text-sm text-neutral-600 mt-1">{p.lojaNome} • {formatPreco(p.preco ?? 0)} • Estoque {p.quantidadeEmEstoque ?? 0}</p>
          {p.descricao ? <p className="text-sm text-neutral-600 mt-1 line-clamp-2">{p.descricao}</p> : null}
          <div className="mt-3">
            <Button size="sm" fullWidth loading={aprovandoId === p.produtoId} onClick={() => handleAprovar(p.produtoId)}>Aprovar produto</Button>
          </div>
        </div>
      ))}
    </div>
  )
}

AbaProdutos.propTypes = {
  onAprovado: PropTypes.func.isRequired,
}

function AbaTaxa() {
  const { showToast } = useUi()
  const [taxa, setTaxa] = useState("8.5")
  const [erro, setErro] = useState(null)
  const [loading, setLoading] = useState(false)
  async function handleSalvar(e) {
    e.preventDefault()
    const n = Number(String(taxa).replace(",", "."))
    if (!Number.isFinite(n) || n < 0 || n > 30) {
      setErro("Taxa deve estar entre 0 e 30%.")
      return
    }
    setErro(null)
    setLoading(true)
    try {
      await adminService.atualizarTaxa(n)
      showToast("Taxa atualizada", "success")
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível atualizar."), "danger")
    } finally {
      setLoading(false)
    }
  }
  return (
    <div className="max-w-xl">
      <form onSubmit={handleSalvar} className="bg-white rounded-xl border border-neutral-200 p-4 flex flex-col gap-3">
        <Input label="Taxa de comissão (%)" type="number" step="0.01" min="0" max="30" value={taxa} onChange={(e) => setTaxa(e.target.value)} error={erro} required />
        <Button type="submit" loading={loading}>Salvar taxa</Button>
      </form>
    </div>
  )
}

function AbaAuditoria() {
  const { showToast } = useUi()
  const [filtros, setFiltros] = useState({ entidade: "", entidadeId: "", usuarioId: "" })
  const [logs, setLogs] = useState([])
  const [loading, setLoading] = useState(false)
  const [buscou, setBuscou] = useState(false)
  async function handleBuscar(e) {
    if (e) e.preventDefault()
    setLoading(true)
    try {
      const r = await adminService.listarAuditLogs({ entidade: filtros.entidade || undefined, entidadeId: filtros.entidadeId || undefined, usuarioId: filtros.usuarioId || undefined })
      setLogs(r.items ?? r ?? [])
      setBuscou(true)
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível listar."), "danger")
    } finally {
      setLoading(false)
    }
  }
  return (
    <div>
      <form onSubmit={handleBuscar} className="flex flex-col sm:flex-row gap-2 mb-4">
        <Input placeholder="Entidade (ex: Usuario)" value={filtros.entidade} onChange={(e) => setFiltros({ ...filtros, entidade: e.target.value })} />
        <Input placeholder="EntidadeId" value={filtros.entidadeId} onChange={(e) => setFiltros({ ...filtros, entidadeId: e.target.value })} />
        <Input placeholder="UsuarioId" value={filtros.usuarioId} onChange={(e) => setFiltros({ ...filtros, usuarioId: e.target.value })} />
        <Button type="submit" loading={loading}>Buscar</Button>
      </form>
      {buscou && logs.length === 0 ? <EmptyState titulo="Sem registros" descricao="Nenhum log para estes filtros." /> : null}
      {logs.length > 0 ? (
        <div className="bg-white rounded-xl border border-neutral-200 overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-neutral-200 text-left">
                <th className="p-3">Data</th>
                <th className="p-3">Ação</th>
                <th className="p-3">Entidade</th>
                <th className="p-3">ID</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((l, i) => (
                <tr key={l.id ?? i} className="border-b border-neutral-100">
                  <td className="p-3 whitespace-nowrap">{l.dataUtc ? formatDataHora(l.dataUtc) : "-"}</td>
                  <td className="p-3"><Badge tone="neutral">{l.acao}</Badge></td>
                  <td className="p-3">{l.entidade}</td>
                  <td className="p-3 font-mono text-xs">{String(l.entidadeId ?? "").slice(0, 8)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}

function PeriodoInput({ label, value, onChange }) {
  return (
    <label className="text-sm flex flex-col gap-1">
      <span className="font-medium">{label}</span>
      <input type="date" value={value} onChange={(e) => onChange(e.target.value)} className="h-11 rounded-xl border border-neutral-200 bg-white px-3" />
    </label>
  )
}

PeriodoInput.propTypes = {
  label: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  onChange: PropTypes.func.isRequired,
}

function formatoDia(d) {
  return d.toISOString().slice(0, 10)
}

function formatoCurto(isoDia) {
  const partes = String(isoDia).split("-")
  if (partes.length !== 3) return isoDia
  return `${partes[2]}/${partes[1]}`
}

function AbaFaturamento() {
  const { showToast } = useUi()
  const [de, setDe] = useState(() => formatoDia(new Date(Date.now() - 30 * 86400000)))
  const [ate, setAte] = useState(() => formatoDia(new Date()))
  const [dados, setDados] = useState(null)
  const [loading, setLoading] = useState(false)
  const [visao, setVisao] = useState("resumo")
  async function handleBuscar(e) {
    if (e) e.preventDefault()
    setLoading(true)
    try {
      const r = await adminService.faturamento({ de: new Date(de).toISOString(), ate: new Date(ate).toISOString() })
      setDados(r)
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível carregar."), "danger")
    } finally {
      setLoading(false)
    }
  }
  useEffect(() => {
    handleBuscar()
  }, [])
  function baixarLancamentos() {
    if (!dados) return
    const linhas = [
      "loja;pedidos;bruto;comissao;liquido",
      ...((dados.porLoja ?? []).map((l) => [l.lojaNome, l.qtdPedidos, l.bruto, l.comissao, l.liquido].join(";"))),
      ["TOTAL", dados.totalPedidos ?? 0, dados.faturamentoBrutoTotal ?? 0, dados.comissaoTotalRetida ?? 0, dados.repasseTotalVendedores ?? 0].join(";"),
    ]
    const blob = new Blob(["\ufeff" + linhas.join("\n")], { type: "text/csv;charset=utf-8" })
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = `repasse-${de}_${ate}.csv`
    a.click()
    URL.revokeObjectURL(url)
  }
  const bruto = dados?.faturamentoBrutoTotal ?? 0
  const comissao = dados?.comissaoTotalRetida ?? 0
  const repasse = dados?.repasseTotalVendedores ?? 0
  const qtd = dados?.totalPedidos ?? 0
  const percentual = bruto > 0 ? `${((comissao / bruto) * 100).toFixed(1).replace(".", ",")}%` : "—"
  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center gap-3 justify-between">
        <p className="text-neutral-600">
          Período de vendas <button type="button" onClick={() => setVisao("resumo")} className="text-primary font-semibold">de {formatoCurto(de)} a {formatoCurto(ate)}</button>
        </p>
        <button type="button" onClick={baixarLancamentos} disabled={!dados} className="text-primary font-semibold text-sm hover:underline disabled:opacity-40">
          Baixar lançamentos
        </button>
      </div>
      <form onSubmit={handleBuscar} className="flex flex-col sm:flex-row gap-3 sm:items-end mt-4 mb-6">
        <PeriodoInput label="De" value={de} onChange={setDe} />
        <PeriodoInput label="Até" value={ate} onChange={setAte} />
        <Button type="submit" loading={loading}>Carregar</Button>
      </form>
      {loading && !dados ? <Skeleton variant="card" /> : null}
      {dados ? (
        <>
          <div className="flex gap-6 border-b border-neutral-200 mb-6">
            <button type="button" onClick={() => setVisao("resumo")} className={["pb-2 text-sm font-semibold", visao === "resumo" ? "text-primary border-b-2 border-primary" : "text-neutral-400"].join(" ")}>Resumo</button>
            <button type="button" onClick={() => setVisao("lojas")} className={["pb-2 text-sm font-semibold", visao === "lojas" ? "text-primary border-b-2 border-primary" : "text-neutral-400"].join(" ")}>Por loja</button>
          </div>
          <h2 className="text-xl font-bold">Detalhando os valores</h2>
          <p className="text-sm text-neutral-600 mt-1">Aqui você encontra o detalhamento do repasse com taxas e o total a repassar aos vendedores.</p>
          {visao === "resumo" ? (
            <div className="grid grid-cols-1 lg:grid-cols-5 gap-4 mt-4">
              <div className="lg:col-span-2 bg-white rounded-xl border border-neutral-200 p-5">
                <p className="text-neutral-600">Valor a repassar</p>
                <p className="font-display text-3xl font-bold mt-3"><span className="text-sm font-sans font-normal align-top">R$ </span>{repasse.toFixed(2).replace(".", ",")}</p>
                <p className="text-sm font-semibold mt-3">Status: {qtd} {qtd === 1 ? "pedido finalizado" : "pedidos finalizados"}</p>
                <button type="button" onClick={() => setVisao("lojas")} className="text-primary text-sm font-semibold mt-2 hover:underline">Detalhar pagamento ›</button>
                <div className="border-t border-neutral-200 mt-5 pt-4 flex gap-2 text-sm text-neutral-600">
                  <span className="text-danger font-bold">!</span>
                  Valores calculados sobre pedidos finalizados com PIX simulado no período.
                </div>
              </div>
              <div className="lg:col-span-3 bg-white rounded-xl border border-neutral-200 overflow-hidden">
                <div className="flex justify-between gap-4 p-4 bg-neutral-100">
                  <div>
                    <p className="font-semibold">Total de vendas no período</p>
                    <p className="text-sm text-neutral-600">Foram {qtd} vendas concluídas.</p>
                  </div>
                  <p className="font-semibold whitespace-nowrap">R$ {bruto.toFixed(2).replace(".", ",")}</p>
                </div>
                <div className="flex justify-between gap-4 p-4 border-t border-neutral-100">
                  <div>
                    <p className="font-semibold">Comissão Saboriza ({percentual})</p>
                    <p className="text-sm text-neutral-600">Retida sobre o valor das vendas ({qtd} pedidos).</p>
                  </div>
                  <p className="whitespace-nowrap">-R$ {comissao.toFixed(2).replace(".", ",")}</p>
                </div>
                <div className="flex justify-between gap-4 p-4 border-t border-neutral-100 bg-neutral-100">
                  <p className="font-bold">Total a repassar</p>
                  <p className="font-bold whitespace-nowrap">R$ {repasse.toFixed(2).replace(".", ",")}</p>
                </div>
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-xl border border-neutral-200 overflow-x-auto mt-4">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-neutral-200 text-left">
                    <th className="p-3">Loja</th>
                    <th className="p-3">Pedidos</th>
                    <th className="p-3">Bruto</th>
                    <th className="p-3">Comissão</th>
                    <th className="p-3">Líquido</th>
                  </tr>
                </thead>
                <tbody>
                  {(dados.porLoja ?? []).map((l) => (
                    <tr key={l.lojaId} className="border-b border-neutral-100">
                      <td className="p-3">{l.lojaNome}</td>
                      <td className="p-3">{l.qtdPedidos}</td>
                      <td className="p-3 font-semibold">{formatPreco(l.bruto)}</td>
                      <td className="p-3">{formatPreco(l.comissao)}</td>
                      <td className="p-3">{formatPreco(l.liquido)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      ) : null}
    </div>
  )
}
