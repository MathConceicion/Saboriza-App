import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { NavLink, Outlet, useLocation } from "react-router-dom"
import { useAuth } from "../../context/AuthContext"
import { pedidosService } from "../../services/pedidos.service"
import { vendedorService } from "../../services/vendedor.service"
import { Icon } from "../ui/Icon"

const grupos = [
  {
    rotulo: "Vendas",
    itens: [
      { to: "/vendedor/dashboard", titulo: "Início", icone: "store" },
      { to: "/vendedor/pedidos", titulo: "Pedidos", icone: "chart" },
      { to: "/vendedor/agenda", titulo: "Agenda", icone: "check" },
    ],
  },
  {
    rotulo: "Loja",
    itens: [
      { to: "/vendedor/produtos", titulo: "Cardápio", icone: "utensils" },
      { to: "/vendedor/configuracoes", titulo: "Minha Loja", icone: "user" },
      { to: "/conta", titulo: "Minha conta", icone: "id" },
    ],
  },
]

function BlocoUsuario({ email }) {
  return (
    <div className="mt-auto pt-5 border-t border-neutral-200">
      <p className="text-sm font-semibold truncate px-2">{email}</p>
      <p className="text-xs text-neutral-500 px-2">Vendedor logado</p>
    </div>
  )
}

BlocoUsuario.propTypes = {
  email: PropTypes.string,
}

export function VendedorShell() {
  const { user } = useAuth()
  const location = useLocation()
  const [pendentes, setPendentes] = useState(0)
  const [nomeLoja, setNomeLoja] = useState("Minha Loja")
  const [abertaAgora, setAbertaAgora] = useState(null)

  useEffect(() => {
    pedidosService
      .listarPendentes()
      .then((r) => setPendentes((r.items ?? r ?? []).length))
      .catch(() => setPendentes(0))
  }, [location.pathname])

  useEffect(() => {
    vendedorService
      .obterLoja()
      .then((r) => {
        if (r?.nome) setNomeLoja(r.nome)
        setAbertaAgora(r?.abertoAgora ?? null)
      })
      .catch(() => setNomeLoja("Minha Loja"))
  }, [])

  return (
    <div className="flex items-stretch">
      <aside className="hidden lg:flex w-60 shrink-0 flex-col bg-white border-r border-neutral-200 px-4 py-5">
        <div className="px-2">
          <p className="font-display font-bold text-primary leading-tight">Painel do vendedor</p>
          <p className="text-sm font-semibold truncate mt-1">{nomeLoja}</p>
        </div>
        <div className={["mt-4 flex items-center gap-2 rounded-xl border px-3 py-2", abertaAgora === false ? "border-neutral-200 bg-neutral-100" : "bg-success/10 border-success/30"].join(" ")}>
          {abertaAgora === false ? (
            <>
              <span className="h-2.5 w-2.5 rounded-full bg-neutral-400" />
              <div>
                <p className="text-sm font-bold text-neutral-600 leading-tight">Loja fechada</p>
                <p className="text-xs text-neutral-500 leading-tight">Fora do horário de funcionamento</p>
              </div>
            </>
          ) : (
            <>
              <span className="h-2.5 w-2.5 rounded-full bg-success" />
              <div>
                <p className="text-sm font-bold text-success leading-tight">Loja aberta</p>
                <p className="text-xs text-neutral-600 leading-tight">Pronta para receber pedidos</p>
              </div>
            </>
          )}
        </div>
        <nav className="mt-5 flex flex-col gap-4" aria-label="Menu do vendedor">
          {grupos.map((g) => (
            <div key={g.rotulo}>
              <p className="px-3 mb-1 text-xs font-bold uppercase tracking-wider text-neutral-400">{g.rotulo}</p>
              <div className="flex flex-col gap-0.5">
                {g.itens.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    className={({ isActive }) => ["flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-semibold transition-colors", isActive ? "bg-primary/10 text-primary border-l-4 border-primary" : "text-neutral-600 hover:bg-neutral-100"].join(" ")}
                  >
                    <Icon name={item.icone} className="h-5 w-5" />
                    {item.titulo}
                    {item.to === "/vendedor/pedidos" && pendentes > 0 ? <span className="ml-auto bg-danger text-white text-xs font-bold rounded-full h-5 min-w-5 px-1 flex items-center justify-center">{String(pendentes)}</span> : null}
                  </NavLink>
                ))}
              </div>
            </div>
          ))}
        </nav>
        <BlocoUsuario email={user?.email} />
      </aside>
      <div className="flex-1 min-w-0">
        <nav className="lg:hidden flex gap-2 overflow-x-auto px-4 py-3 border-b border-neutral-200 bg-white" aria-label="Menu do vendedor">
          {grupos.flatMap((g) => g.itens).map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => ["whitespace-nowrap px-4 py-2 rounded-full text-sm font-semibold", isActive ? "bg-primary text-white" : "bg-white border border-neutral-200 text-neutral-600"].join(" ")}
            >
              {item.titulo}
            </NavLink>
          ))}
        </nav>
        <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8">
          <Outlet />
        </div>
      </div>
    </div>
  )
}
