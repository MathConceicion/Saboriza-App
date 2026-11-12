import { useAuth } from "../../context/AuthContext"
import { notificacoesService } from "../../services/notificacoes.service.js"
import { useState, useEffect } from "react"
import { Icon } from "../ui/Icon"

export function AdminHeader() {
  const { user, clearSession } = useAuth()
  const [naoLidas, setNaoLidas] = useState(0)
  const [notificacoes, setNotificacoes] = useState([])
  const [menuAberto, setMenuAberto] = useState(false)

  useEffect(() => {
    carregarNaoLidas()
    carregarNotificacoes()
    const timer = setInterval(carregarNaoLidas, 30000)
    return () => clearInterval(timer)
  }, [])

  async function carregarNaoLidas() {
    try {
      const r = await notificacoesService.contarNaoLidas()
      setNaoLidas(r.naoLidas ?? 0)
    } catch {}
  }

  async function carregarNotificacoes() {
    try {
      const r = await notificacoesService.listar({ tamanho: 5 })
      setNotificacoes(r.items ?? r ?? [])
    } catch {}
  }

  async function handleSair() {
    await clearSession()
    window.location.href = "/"
  }

  return (
    <header className="sticky top-0 z-40 bg-white border-b border-neutral-200">
      <div className="px-4 sm:px-6 lg:px-12 h-16 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button className="lg:hidden text-neutral-600 hover:text-primary" aria-label="Abrir menu">
            <Icon name="chart" className="h-6 w-6" />
          </button>
          <div className="flex items-center gap-2">
            <div className="h-8 w-8 rounded-xl bg-primary flex items-center justify-center text-white">
              <Icon name="store" className="h-5 w-5" />
            </div>
            <span className="font-display font-bold text-primary text-lg">Saboriza Admin</span>
          </div>
        </div>

        <div className="flex items-center gap-4">
          <div className="relative hidden lg:block">
            <button
              onClick={() => carregarNotificacoes()}
              className="relative p-2 text-neutral-600 hover:text-primary hover:bg-neutral-100 rounded-xl transition-colors"
              aria-label="Notificações"
            >
              <Icon name="bell" className="h-5 w-5" />
              {naoLidas > 0 ? (
                <span className="absolute -top-1 -right-1 bg-danger text-white text-xs font-bold rounded-full h-5 min-w-5 px-1 flex items-center justify-center">{naoLidas}</span>
              ) : null}
            </button>
          </div>

          <div className="relative group">
            <button
              onClick={() => setMenuAberto(!menuAberto)}
              className="flex items-center gap-2 p-2 text-neutral-600 hover:text-primary hover:bg-neutral-100 rounded-xl transition-colors"
              aria-label="Menu do usuário"
            >
              <div className="h-8 w-8 rounded-full bg-neutral-200 flex items-center justify-center text-neutral-600">
                <Icon name="user" className="h-5 w-5" />
              </div>
              <span className="text-sm font-semibold text-neutral-900 hidden sm:block">{user?.nome}</span>
            </button>
            {menuAberto ? (
              <div className="absolute right-0 top-14 pt-2 w-48 bg-white rounded-xl border border-neutral-200 shadow-card py-2">
                <div className="px-4 py-2 border-b border-neutral-100">
                  <p className="text-sm font-semibold text-neutral-900">{user?.nome}</p>
                  <p className="text-xs text-neutral-500">{user?.email}</p>
                  <span className="text-xs font-bold bg-primary text-white px-2 py-0.5 rounded mt-1">SUPER ADMIN</span>
                </div>
                <button onClick={handleSair} className="w-full flex items-center gap-2 px-4 py-2 text-sm text-danger hover:bg-danger/10 hover:text-danger-dark rounded-lg transition-colors">
                  <Icon name="logout" className="h-4 w-4" />
                  Sair
                </button>
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </header>
  )
}
