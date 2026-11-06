import { Link, useNavigate } from "react-router-dom"
import { useAuth } from "../../context/AuthContext"
import { useCart } from "../../context/CartContext"
import { HybridButton } from "./HybridButton"
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
      <div className="px-4 sm:px-6 lg:px-12 h-16 flex items-center justify-between">
        <Link to="/buscar" aria-label="Saboriza - marketplace" className="flex items-center">
          <img src="/favicon.ico" alt="Logo Saboriza" className="h-10 w-10 rounded-full object-cover" />
        </Link>
        <nav className="flex items-center gap-2 sm:gap-3">
          <HybridButton />
          {user ? (
            <>
              {!user.isAdmin ? (
                <Link to="/checkout" aria-label={`Abrir carrinho com ${qtdCarrinho} itens`} className="relative text-neutral-600 hover:text-primary border border-neutral-200 rounded-full p-2">
                  <Icon name="cart" className="h-5 w-5" />
                  {qtdCarrinho > 0 ? <span className="absolute -top-2 -right-2 bg-primary text-white text-xs rounded-full h-5 min-w-5 px-1 flex items-center justify-center">{qtdCarrinho}</span> : null}
                </Link>
              ) : null}
              <div className="relative group">
                <Link to="/conta" aria-label="Minha conta" className="block text-neutral-600 hover:text-primary border border-neutral-200 rounded-full p-2">
                  <Icon name="user" className="h-5 w-5" />
                </Link>
                <div className="absolute right-0 top-full pt-2 hidden group-hover:block">
                  <div className="bg-white rounded-xl border border-neutral-200 shadow-card py-1 w-44">
                    <Link to="/buscar" className="block px-4 py-2 text-sm font-semibold text-neutral-900 hover:bg-neutral-100 hover:text-primary">Pedir</Link>
                    <Link to="/meus-pedidos" className="block px-4 py-2 text-sm font-semibold text-neutral-900 hover:bg-neutral-100 hover:text-primary">Meus pedidos</Link>
                    <Link to="/conta" className="block px-4 py-2 text-sm text-neutral-600 hover:bg-neutral-100 hover:text-primary">Minha conta</Link>
                    {!user.isAdmin && !isVendorAprovado && !isVendedor ? <Link to="/vender" className="block px-4 py-2 text-sm text-primary font-semibold hover:bg-neutral-100">Quero vender</Link> : null}
                    {user.isAdmin ? <Link to="/admin" className="block px-4 py-2 text-sm font-semibold text-danger hover:bg-neutral-100">Admin</Link> : null}
                  </div>
                </div>
              </div>
              <button onClick={handleSair} aria-label="Sair" className="text-danger hover:opacity-70 border border-neutral-200 rounded-full p-2">
                <Icon name="logout" className="h-5 w-5" />
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="text-primary font-semibold text-sm">Entrar</Link>
              <Link to="/registro" className="bg-primary text-white rounded-full px-4 py-2 text-sm font-semibold hover:bg-primary-dark">Cadastrar</Link>
            </>
          )}
        </nav>
      </div>
    </header>
  )
}
