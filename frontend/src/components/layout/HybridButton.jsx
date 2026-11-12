import { useNavigate, useLocation } from "react-router-dom"
import { useAuth } from "../../context/AuthContext"
import { Icon } from "../ui/Icon"

export function HybridButton() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isVendorAprovado } = useAuth()

  if (!isVendorAprovado) return null

  const estaNoPainel = location.pathname.startsWith("/vendedor")
  const destino = estaNoPainel ? "/buscar" : "/vendedor/dashboard"
  const label = estaNoPainel ? "Voltar para Loja" : "Ir para Painel do Vendedor"

  function handleClick() {
    navigate(destino)
  }

  return (
    <button
      onClick={handleClick}
      aria-label={label}
      title={label}
      className={[
        "inline-flex shrink-0 items-center justify-center gap-2 rounded-full px-3 py-2.5 text-sm font-semibold transition-all sm:px-5",
        estaNoPainel ? "bg-white text-primary border border-primary hover:bg-neutral-100" : "bg-primary text-white hover:bg-primary-dark shadow-md hover:shadow-lg",
      ].join(" ")}
    >
      <Icon name={estaNoPainel ? "store" : "chart"} className="h-4 w-4 shrink-0" />
      <span className="hidden whitespace-nowrap sm:inline">{label}</span>
    </button>
  )
}
