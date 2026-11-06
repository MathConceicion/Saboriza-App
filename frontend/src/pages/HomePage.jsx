import { Navigate } from "react-router-dom"
import { useAuth } from "../context/AuthContext"
import { HeroParceiro, FaixaEstatisticas } from "../components/landing/HeroParceiro"
import { VantagensParceiro, PlanoParceiro, PassosAbertura, PerguntasFrequentes } from "../components/landing/SecoesParceiro"

export default function HomePage() {
  const { user, loading: authLoading } = useAuth()

  if (!authLoading && user?.isAdmin) return <Navigate to="/admin" replace />

  return (
    <div>
      <HeroParceiro />
      <FaixaEstatisticas />
      <VantagensParceiro />
      <PlanoParceiro />
      <PassosAbertura />
      <PerguntasFrequentes />
    </div>
  )
}
