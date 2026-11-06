import { useEffect, useState } from "react"
import { useNavigate, Navigate } from "react-router-dom"
import { CategoryGrid } from "../components/category/CategoryCard"
import { AddressBar } from "../components/address/AddressBar"
import { StoreCard } from "../components/store/StoreCard"
import { Skeleton } from "../components/ui/Skeleton"
import { EmptyState } from "../components/ui/EmptyState"
import { Button } from "../components/ui/Button"
import { useAddress } from "../context/AddressContext"
import { useAuth } from "../context/AuthContext"
import { useUi } from "../context/UiContext"
import { lojasService } from "../services/lojas.service"
import { categoriasService } from "../services/categorias.service"
import { validarCep } from "../lib/validators"
import { buscarCepDemo, listarCepsDemo } from "../lib/ceps"
import { iconeCategoria, reservaCategoria, imagemLoja } from "../lib/imagens"
import { mensagemPorStatus } from "../lib/apiErrors"

const categoriasFallback = [
  { nome: "Pizzarias", slug: "pizzarias" },
  { nome: "Hambúrguerias", slug: "hamburguerias" },
  { nome: "Japonesa", slug: "comida-japonesa" },
  { nome: "Sorveterias", slug: "sorveterias" },
  { nome: "Docerias", slug: "docerias" },
  { nome: "Padarias", slug: "padarias-artesanais" },
  { nome: "Brasileira", slug: "comida-brasileira" },
  { nome: "Saudável", slug: "saudavel-vegana" },
]

function comImagensLocais(lista) {
  return (lista ?? []).map((c) => ({
    nome: c.nome,
    slug: c.slug,
    iconeUrl: c.iconeUrl ?? iconeCategoria(c.slug),
    reservaUrl: reservaCategoria(c.slug),
  }))
}

export default function BuscarPage() {
  const navigate = useNavigate()
  const { latitude, longitude, cep, endereco, definirEndereco, limpar } = useAddress()
  const { user, loading: authLoading } = useAuth()
  const { showToast } = useUi()
  const [categorias, setCategorias] = useState(comImagensLocais(categoriasFallback))
  const [carregandoCategorias, setCarregandoCategorias] = useState(true)
  const [erroCategorias, setErroCategorias] = useState(null)
  const [lojas, setLojas] = useState([])
  const [carregandoLojas, setCarregandoLojas] = useState(false)
  const [erroLojas, setErroLojas] = useState(null)
  const [categoriaAtiva, setCategoriaAtiva] = useState(null)
  const [erroCep, setErroCep] = useState(null)
  const [raioKm, setRaioKm] = useState(5)

  function carregarCategorias() {
    setCarregandoCategorias(true)
    setErroCategorias(null)
    categoriasService
      .listar()
      .then((r) => {
        const items = r.items ?? r
        if (Array.isArray(items) && items.length > 0) setCategorias(comImagensLocais(items))
      })
      .catch((e) => setErroCategorias(mensagemPorStatus(e, "Não foi possível carregar categorias.")))
      .finally(() => setCarregandoCategorias(false))
  }

  useEffect(() => {
    carregarCategorias()
  }, [])

  useEffect(() => {
    if (latitude && longitude) {
      setCarregandoLojas(true)
      setErroLojas(null)
      lojasService
        .listarProximas({ lat: latitude, lng: longitude, raioKm })
        .then((r) => setLojas(r.items ?? r ?? []))
        .catch((e) => {
          setLojas([])
          setErroLojas(mensagemPorStatus(e, "Não foi possível carregar lojas."))
        })
        .finally(() => setCarregandoLojas(false))
    }
  }, [latitude, longitude, raioKm])

  function handleSelectCategoria(slug) {
    setCategoriaAtiva(slug)
    const qs = latitude && longitude ? `?lat=${latitude}&lng=${longitude}` : ""
    navigate(`/categoria/${slug}${qs}`)
  }

  function handleBuscarCep(cepDigitado) {
    if (!validarCep(cepDigitado)) {
      setErroCep("CEP inválido. Digite os 8 números.")
      return
    }
    const achado = buscarCepDemo(cepDigitado)
    if (!achado) {
      setErroCep("CEP fora da área de demonstração. Tente " + listarCepsDemo().join(", ") + ".")
      return
    }
    setErroCep(null)
    definirEndereco(achado.cep, achado.endereco, achado.latitude, achado.longitude)
    showToast("Endereço atualizado", "success")
  }

  function handleLimparCep() {
    limpar()
    setErroCep(null)
    setLojas([])
    setErroLojas(null)
  }

  if (!authLoading && user?.isAdmin) return <Navigate to="/admin" replace />

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <AddressBar
        valorInicial={cep ?? ""}
        erro={erroCep}
        onBuscar={handleBuscarCep}
        onLimpar={handleLimparCep}
        mostrarLimpar={Boolean(cep)}
        textoAjuda={cep ? `Mostrando lojas próximas a ${endereco ?? cep}` : "Informe seu endereço para ver lojas próximas"}
      />
      <h1 className="text-2xl font-bold mt-8 mb-6">O que você quer comer hoje?</h1>
      {carregandoCategorias ? <Skeleton variant="circle" /> : null}
      {!carregandoCategorias && erroCategorias ? (
        <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4 flex items-center justify-between">
          <span>{erroCategorias}</span>
          <Button size="sm" variant="ghost" onClick={carregarCategorias}>Tentar novamente</Button>
        </div>
      ) : null}
      {!carregandoCategorias ? <CategoryGrid categorias={categorias} categoriaAtiva={categoriaAtiva} onSelectCategoria={handleSelectCategoria} /> : null}
      {cep ? (
        <div className="mt-8">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold">Lojas próximas a você</h2>
            <label className="text-sm text-neutral-600 flex items-center gap-2">
              Raio
              <select value={raioKm} onChange={(e) => setRaioKm(Number(e.target.value))} className="h-9 rounded-lg border border-neutral-200 bg-white px-2">
                <option value={5}>5 km</option>
                <option value={10}>10 km</option>
                <option value={20}>20 km</option>
                <option value={50}>50 km</option>
              </select>
            </label>
          </div>
          {carregandoLojas ? <Skeleton variant="card" /> : null}
          {!carregandoLojas && erroLojas ? (
            <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4 flex items-center justify-between">
              <span>{erroLojas}</span>
              <Button size="sm" variant="ghost" onClick={() => setRaioKm((v) => v)}>Tentar novamente</Button>
            </div>
          ) : null}
          {!carregandoLojas && !erroLojas && lojas.length === 0 ? <EmptyState titulo="Nenhuma loja por aqui" descricao="Tente aumentar o raio de busca ou outro CEP." /> : null}
          {!carregandoLojas && !erroLojas ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {lojas.map((l) => (
                <StoreCard
                  key={l.lojaId}
                  fotoUrl={l.fotoUrl ?? imagemLoja(l.lojaId, l.categoria)}
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
      ) : (
        <div className="mt-8">
          <EmptyState titulo="Descubra sabores perto de você" descricao="Informe seu CEP acima para listar lojas próximas." />
        </div>
      )}
    </div>
  )
}
