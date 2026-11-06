import { useState } from "react"
import PropTypes from "prop-types"
import { useNavigate } from "react-router-dom"
import { useAuth } from "../../context/AuthContext"
import { Button } from "../ui/Button"

const estatisticas = [
  { valor: "8", rotulo: "categorias gastronômicas" },
  { valor: "10", rotulo: "lojas artesanais na demo" },
  { valor: "8,5%", rotulo: "comissão única transparente" },
  { valor: "5 km", rotulo: "raio padrão de proximidade" },
]

function emailValido(valor) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(valor.trim())
}

export function HeroParceiro() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [email, setEmail] = useState("")
  const [erroEmail, setErroEmail] = useState(null)

  function handleQueroVender() {
    if (!user) {
      navigate("/registro")
      return
    }
    navigate("/vender")
  }

  function handlePedirAgora() {
    navigate("/buscar")
  }

  function handleCadastrarAgora(event) {
    event.preventDefault()
    if (user) {
      navigate("/vender")
      return
    }
    if (!emailValido(email)) {
      setErroEmail("Digite um e-mail válido para continuar.")
      return
    }
    setErroEmail(null)
    navigate(`/registro?email=${encodeURIComponent(email.trim())}`)
  }

  return (
    <section className="relative text-white overflow-hidden">
      <img
        src="/images/hero-section.webp"
        alt=""
        aria-hidden="true"
        className="absolute inset-0 h-full w-full object-cover object-center"
      />
      <div aria-hidden="true" className="absolute inset-0 bg-gradient-to-r from-[#2B0608]/95 via-[#4A0B0E]/75 to-[#A01E24]/25" />
      <div className="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t from-[#2B0608]/70 to-transparent" aria-hidden="true" />
      <div className="relative mx-auto max-w-7xl px-4 py-16 lg:py-0 min-h-[640px] lg:min-h-[780px] grid gap-10 lg:grid-cols-2 items-center">
        <div className="py-6">
          <h1 className="font-display text-4xl lg:text-6xl font-bold leading-tight mt-3 drop-shadow-lg">
            Conecte sua cozinha a milhares de clientes
          </h1>
          <p className="mt-4 text-base lg:text-xl text-white/90">
            Do bairro para a cidade: vitrine por proximidade, agenda de produção e comissão única de 8,5%. Sem taxa de
            adesão na demonstração.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Button size="lg" onClick={handleQueroVender}>Cadastrar minha cozinha</Button>
            <button
              type="button"
              onClick={handlePedirAgora}
              className="inline-flex items-center justify-center rounded-xl font-semibold h-12 px-6 text-base bg-white text-primary hover:bg-neutral-100 transition-colors"
            >
              Pedir agora
            </button>
          </div>
        </div>
        <div className="justify-self-center lg:justify-self-end w-full max-w-md py-6">
          <form onSubmit={handleCadastrarAgora} className="bg-white text-neutral-900 rounded-2xl shadow-card p-6 lg:p-8">
            <h2 className="text-xl lg:text-2xl font-bold">Cadastre sua cozinha na Saboriza</h2>
            <p className="text-sm text-neutral-600 mt-1">Entre e venda para milhares de clientes!</p>
            <label className="block text-sm font-semibold mt-5">
              E-mail*
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="email@email.com.br"
                aria-label="E-mail"
                className="mt-1 h-12 w-full rounded-xl border border-neutral-200 bg-white px-4 text-sm font-normal outline-none focus:border-primary focus:ring-2 focus:ring-primary/30"
              />
            </label>
            {erroEmail ? <p className="text-danger text-sm mt-2">{erroEmail}</p> : null}
            <Button type="submit" fullWidth size="lg" className="mt-4">Cadastrar agora</Button>
            <p className="text-xs text-neutral-500 mt-4">
              Ao continuar, você concorda em receber comunicações da Saboriza. Confira nossa Declaração de Privacidade.
            </p>
          </form>
        </div>
      </div>
    </section>
  )
}

HeroParceiro.propTypes = {}

export function FaixaEstatisticas() {
  return (
    <section aria-label="Números da Saboriza" className="bg-white border-b border-neutral-200">
      <div className="mx-auto max-w-7xl px-4 py-8 grid grid-cols-2 lg:grid-cols-4 gap-6 text-center">
        {estatisticas.map((e) => (
          <div key={e.rotulo}>
            <p className="font-display text-3xl lg:text-4xl font-bold text-primary">{e.valor}</p>
            <p className="text-sm text-neutral-600 mt-1">{e.rotulo}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

FaixaEstatisticas.propTypes = {}
