import { NavLink, useLocation } from "react-router-dom"
import { useAuth } from "../../context/AuthContext"
import { Icon } from "../ui/Icon"
import { useUi } from "../../context/UiContext"

const menuItens = [
  {
    grupo: "Visão Geral",
    itens: [
      { id: "dashboard", titulo: "Dashboard", icone: "chart", rota: "/admin/dashboard" },
    ],
  },
  {
    grupo: "Solicitações",
    itens: [
      { id: "solicitacoes-lojas", titulo: "Solicitações de Lojas", icone: "store", rota: "/admin/solicitacoes-lojas" },
    ],
  },
  {
    grupo: "Usuários",
    itens: [
      { id: "usuarios", titulo: "Usuários", icone: "users", rota: "/admin/usuarios" },
    ],
  },
  {
    grupo: "Lojas",
    itens: [
      { id: "lojas", titulo: "Lojas", icone: "store", rota: "/admin/lojas" },
    ],
  },
  {
    grupo: "Produtos",
    itens: [
      { id: "produtos", titulo: "Produtos", icone: "clipboard", rota: "/admin/produtos" },
    ],
  },
  {
    grupo: "Notificações",
    itens: [
      { id: "notificacoes", titulo: "Notificações", icone: "bell", rota: "/admin/notificacoes" },
    ],
  },
  {
    grupo: "Sistema",
    itens: [
      { id: "auditoria", titulo: "Auditoria e Logs", icone: "eye", rota: "/admin/auditoria" },
      { id: "configuracoes", titulo: "Configurações", icone: "settings", rota: "/admin/configuracoes" },
    ],
  },
]

export function AdminSidebar() {
  const { user } = useAuth()
  const { showToast } = useUi()
  const location = useLocation()

  function handleSair() {
    showToast("Saindo do painel administrativo", "info")
  }

  return (
    <aside className="hidden lg:flex w-64 shrink-0 flex-col bg-white border-r border-neutral-200">
      <div className="px-6 py-5">
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 rounded-xl bg-primary flex items-center justify-center text-white">
            <Icon name="store" className="h-5 w-5" />
          </div>
          <div>
            <p className="font-display font-bold text-primary leading-tight">Saboriza Admin</p>
            <p className="text-xs text-neutral-500 leading-tight">Painel do Super Administrador</p>
          </div>
        </div>
      </div>

      <nav className="mt-4 flex flex-col gap-1 px-3" aria-label="Menu do super admin">
        {menuItens.map((g) => (
          <div key={g.grupo}>
            <p className="px-3 mb-1 text-xs font-bold uppercase tracking-wider text-neutral-400">{g.grupo}</p>
            <div className="flex flex-col gap-0.5">
              {g.itens.map((m) => {
                const ativo = location.pathname === m.rota
                return (
                  <NavLink
                    key={m.id}
                    to={m.rota}
                    className={({ isActive }) => {
                      const active = isActive
                      return [
                        "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold transition-colors",
                        active
                          ? "bg-primary/10 text-primary border-l-4 border-primary"
                          : "text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900",
                      ].join(" ")
                    }}
                  >
                    <Icon name={m.icone} className="h-5 w-5" />
                    {m.titulo}
                  </NavLink>
                )
              })}
            </div>
          </div>
        ))}
      </nav>

      <div className="mt-auto pt-6 pb-4 border-t border-neutral-200 px-3">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-full bg-neutral-200 flex items-center justify-center text-neutral-600">
            <Icon name="user" className="h-5 w-5" />
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold truncate">{user?.nome || user?.email}</p>
            <p className="text-xs text-neutral-500 truncate">{user?.email}</p>
            <span className="text-xs font-bold bg-primary text-white px-2 py-0.5 rounded">SUPER ADMIN</span>
          </div>
        </div>
        <button
          onClick={handleSair}
          className="mt-3 w-full flex items-center gap-2 rounded-xl px-3 py-2 text-sm font-semibold text-danger hover:bg-danger/10 transition-colors"
        >
          <Icon name="logout" className="h-4 w-4" />
          Sair
        </button>
      </div>
    </aside>
  )
}
