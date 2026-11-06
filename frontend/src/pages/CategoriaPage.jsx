import { useEffect, useState } from "react"
import { useParams, useSearchParams, useNavigate } from "react-router-dom"
import { lojasService } from "../services/lojas.service"
import { StoreCard } from "../components/store/StoreCard"
import { Skeleton } from "../components/ui/Skeleton"
import { EmptyState } from "../components/ui/EmptyState"
import { Button } from "../components/ui/Button"
import { imagemLoja } from "../lib/imagens"
import { BackButton } from "../components/ui/BackButton"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function CategoriaPage() {
  const { slug } = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [lojas, setLojas] = useState([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)

  const lat = searchParams.get("lat")
  const lng = searchParams.get("lng")

  function carregar() {
    setLoading(true)
    setErro(null)
    lojasService
      .listarPorCategoria(slug, { lat: lat ? Number(lat) : undefined, lng: lng ? Number(lng) : undefined })
      .then((r) => setLojas(r.items ?? r ?? []))
      .catch((e) => {
        setLojas([])
        setErro(mensagemPorStatus(e, "Erro ao carregar lojas"))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [slug, lat, lng])

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <BackButton onClick={() => navigate("/buscar")} />
      <h1 className="text-2xl font-bold capitalize mb-6">{slug.replace("-", " ")}</h1>
      {loading ? <Skeleton variant="card" /> : null}
      {!loading && erro ? (
        <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm flex items-center justify-between">
          <span>{erro}</span>
          <Button size="sm" variant="ghost" onClick={carregar}>Tentar novamente</Button>
        </div>
      ) : null}
      {!loading && !erro && lojas.length === 0 ? <EmptyState titulo="Nenhuma loja nesta categoria" descricao="Volte ao marketplace e explore outras categorias." actionLabel="Ver marketplace" onAction={() => navigate("/buscar")} /> : null}
      {!loading && !erro ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mt-6">
          {lojas.map((l) => (
            <StoreCard
              key={l.lojaId}
              fotoUrl={l.fotoUrl ?? imagemLoja(l.lojaId, slug)}
              nome={l.nome}
              categoria={l.categoria}
              distanciaKm={l.distanciaKm}
              avaliacao={l.avaliacao}
              endereco={l.endereco}
              descricao={l.descricao}
              abertoAgora={l.abertoAgora}
              horarioAbertura={l.horarioAbertura}
              horarioFechamento={l.horarioFechamento}
              onSelect={() => navigate(`/loja/${l.lojaId}`)}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}
