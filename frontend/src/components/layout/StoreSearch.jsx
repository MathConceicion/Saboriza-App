import { useCallback, useEffect, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import { lojasService } from "../../services/lojas.service"
import { tratarErroImagem } from "../../lib/imagens"
import { Icon } from "../ui/Icon"

const INTERVALO_DEBOUNCE_MS = 250

export function StoreSearch() {
  const navigate = useNavigate()
  const containerRef = useRef(null)
  const inputRef = useRef(null)
  const requisicaoRef = useRef(null)
  const recomendacoesRef = useRef(null)
  const [termo, setTermo] = useState("")
  const [aberto, setAberto] = useState(false)
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState(false)
  const [lojas, setLojas] = useState([])

  const buscarLojas = useCallback(async (texto, signal) => {
    setCarregando(true)
    setErro(false)
    try {
      const dados = await lojasService.buscar(texto, { signal })
      if (signal?.aborted) return
      const lista = Array.isArray(dados) ? dados : []
      setLojas(lista)
      if (!texto) recomendacoesRef.current = lista
    } catch (error) {
      if (signal?.aborted || error?.code === "ERR_CANCELED") return
      setLojas([])
      setErro(true)
    } finally {
      if (!signal?.aborted) setCarregando(false)
    }
  }, [])

  const fechar = useCallback(() => {
    requisicaoRef.current?.abort()
    requisicaoRef.current = null
    setAberto(false)
    setCarregando(false)
  }, [])

  useEffect(() => {
    if (!aberto) return
    const texto = termo.trim()
    if (!texto && recomendacoesRef.current) {
      requisicaoRef.current?.abort()
      requisicaoRef.current = null
      setLojas(recomendacoesRef.current)
      setCarregando(false)
      setErro(false)
      return
    }
    const timer = setTimeout(() => {
      requisicaoRef.current?.abort()
      const controller = new AbortController()
      requisicaoRef.current = controller
      buscarLojas(texto, controller.signal)
    }, texto ? INTERVALO_DEBOUNCE_MS : 0)
    return () => clearTimeout(timer)
  }, [aberto, termo, buscarLojas])

  useEffect(() => {
    if (!aberto) return
    function aoPressionarTecla(event) {
      if (event.key === "Escape") {
        fechar()
        inputRef.current?.blur()
      }
    }
    function aoClicarFora(event) {
      if (containerRef.current && !containerRef.current.contains(event.target)) fechar()
    }
    document.addEventListener("keydown", aoPressionarTecla)
    document.addEventListener("pointerdown", aoClicarFora)
    return () => {
      document.removeEventListener("keydown", aoPressionarTecla)
      document.removeEventListener("pointerdown", aoClicarFora)
    }
  }, [aberto, fechar])

  useEffect(() => () => requisicaoRef.current?.abort(), [])

  function selecionarLoja(lojaId) {
    setTermo("")
    fechar()
    inputRef.current?.blur()
    navigate(`/loja/${lojaId}`)
  }

  return (
    <div ref={containerRef} className="relative w-full">
      <div className="relative">
        <Icon name="search" className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-neutral-500" />
        <input
          ref={inputRef}
          type="search"
          value={termo}
          onChange={(event) => setTermo(event.target.value)}
          onFocus={() => setAberto(true)}
          onClick={() => setAberto(true)}
          placeholder="Buscar lojas..."
          aria-label="Buscar lojas"
          aria-expanded={aberto}
          aria-controls="busca-lojas-resultados"
          autoComplete="off"
          className="h-10 w-full rounded-xl border border-neutral-200 bg-neutral-50 pl-9 pr-3 text-sm text-neutral-900 outline-none transition-colors placeholder:text-neutral-500 focus:border-primary focus:bg-white focus:ring-2 focus:ring-primary/20"
        />
      </div>
      <div
        id="busca-lojas-resultados"
        aria-hidden={!aberto}
        className={[
          "absolute left-0 right-0 top-full z-50 mt-2 origin-top overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-card transition-all duration-150",
          aberto ? "visible translate-y-0 opacity-100" : "pointer-events-none invisible -translate-y-1 opacity-0",
        ].join(" ")}
      >
        <div className="max-h-72 overflow-y-auto py-1 sm:max-h-80">
          {carregando ? <p className="px-4 py-2 text-xs font-medium text-neutral-500">Buscando lojas...</p> : null}
          {!carregando && erro ? <p className="px-4 py-3 text-sm text-danger">Não foi possível buscar lojas. Tente novamente.</p> : null}
          {!carregando && !erro && lojas.length === 0 ? <p className="px-4 py-3 text-sm text-neutral-500">Nenhuma loja encontrada.</p> : null}
          {lojas.map((loja) => (
            <button
              key={loja.lojaId}
              type="button"
              onClick={() => selecionarLoja(loja.lojaId)}
              className="flex w-full items-center gap-3 px-3 py-2 text-left transition-colors hover:bg-neutral-100 focus-visible:bg-neutral-100 focus-visible:outline-none"
            >
              {loja.fotoUrl ? (
                <img src={loja.fotoUrl} onError={tratarErroImagem} alt="" loading="lazy" className="h-10 w-10 shrink-0 rounded-lg object-cover" />
              ) : (
                <span aria-hidden="true" className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary-light/40 text-base font-semibold text-primary">
                  {loja.nome.charAt(0).toUpperCase()}
                </span>
              )}
              <span className="min-w-0">
                <span className="block truncate text-sm font-semibold text-neutral-900">{loja.nome}</span>
                <span className="block truncate text-xs text-neutral-500">{loja.categoria || "Loja"}</span>
              </span>
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
