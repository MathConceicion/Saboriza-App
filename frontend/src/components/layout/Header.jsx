import { Link, useNavigate } from "react-router-dom"
import { useAuth } from "../../context/AuthContext"
import { useCart } from "../../context/CartContext"
import { HybridButton } from "./HybridButton"
import { StoreSearch } from "./StoreSearch"
import { Icon } from "../ui/Icon"

export function Header() {
  const { user, clearSession, isVendorAprovado, isVendedor } = useAuth()
  const { itens } = useCart()
  const navigate = useNavigate()
  const qtdCarrinho = itens.reduce((acc, i) => acc + i.quantidade, 0)

  async function handleSair() {
    await clearSession()
    navigate("/")
  }

  return (
    <header className="sticky top-0 z-40 bg-white border-b border-neutral-200">
      <div className="flex flex-wrap items-center gap-x-2 gap-y-2 px-3 py-2 sm:h-16 sm:flex-nowrap sm:gap-x-4 sm:px-6 sm:py-0 lg:px-12">
        <Link to="/buscar" aria-label="Saboriza - marketplace" className="order-1 flex shrink-0 items-center">
          <img src="/favicon.ico" alt="Logo Saboriza" className="h-9 w-9 rounded-full object-cover sm:h-10 sm:w-10" />
        </Link>
        <div className="order-3 w-full min-w-0 sm:order-2 sm:w-auto sm:flex-1 sm:max-w-xl">
          <StoreSearch />
        </div>
        <nav className="order-2 ml-auto flex min-w-0 items-center gap-1.5 sm:order-3 sm:gap-3">
          <HybridButton />
          {user ? (
            <>
              {!user.isAdmin && !isVendorAprovado && !isVendedor ? (
                <Link to="/vender" aria-label="Quero vender" title="Quero vender" className="inline-flex shrink-0 items-center justify-center gap-2 rounded-full bg-primary px-3 py-2.5 text-sm font-semibold text-white shadow-md transition-all hover:bg-primary-dark hover:shadow-lg sm:px-5">
                  <Icon name="store" className="h-4 w-4 shrink-0" />
                  <span className="hidden whitespace-nowrap sm:inline">Quero vender</span>
                </Link>
              ) : null}
              {!user.isAdmin ? (
                <Link to="/checkout" aria-label={`Abrir carrinho com ${qtdCarrinho} itens`} className="relative shrink-0 rounded-full border border-neutral-200 p-2 text-neutral-600 hover:text-primary">
                  <Icon name="cart" className="h-5 w-5" />
                  {qtdCarrinho > 0 ? <span className="absolute -right-2 -top-2 flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1 text-xs text-white">{qtdCarrinho}</span> : null}
                </Link>
              ) : null}
              <div className="group relative shrink-0">
                <Link to="/conta" aria-label="Minha conta" className="block rounded-full border border-neutral-200 p-2 text-neutral-600 hover:text-primary">
                  <Icon name="user" className="h-5 w-5" />
                </Link>
                <div className="absolute right-0 top-full hidden pt-2 group-hover:block">
                  <div className="w-44 rounded-xl border border-neutral-200 bg-white py-1 shadow-card">
                    <Link to="/buscar" className="block px-4 py-2 text-sm font-semibold text-neutral-900 hover:bg-neutral-100 hover:text-primary">Pedir</Link>
                    <Link to="/meus-pedidos" className="block px-4 py-2 text-sm font-semibold text-neutral-900 hover:bg-neutral-100 hover:text-primary">Meus pedidos</Link>
                    <Link to="/conta" className="block px-4 py-2 text-sm text-neutral-600 hover:bg-neutral-100 hover:text-primary">Minha conta</Link>
                    {user.isAdmin ? <Link to="/admin" className="block px-4 py-2 text-sm font-semibold text-danger hover:bg-neutral-100">Admin</Link> : null}
                  </div>
                </div>
              </div>
              <button onClick={handleSair} aria-label="Sair" className="shrink-0 rounded-full border border-neutral-200 p-2 text-danger hover:opacity-70">
                <Icon name="logout" className="h-5 w-5" />
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="whitespace-nowrap text-sm font-semibold text-primary">Entrar</Link>
              <Link to="/registro" className="whitespace-nowrap rounded-full bg-primary px-4 py-2 text-sm font-semibold text-white hover:bg-primary-dark">Cadastrar</Link>
            </>
          )}
        </nav>
      </div>
    </header>
  )
}
