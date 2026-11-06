import { useState } from "react"
import { useNavigate, Link } from "react-router-dom"
import { authService } from "../services/auth.service"
import { useAuth } from "../context/AuthContext"
import { Icon } from "../components/ui/Icon"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function LoginPage() {
  const navigate = useNavigate()
  const { setSession } = useAuth()
  const [erro, setErro] = useState(null)
  const [loading, setLoading] = useState(false)
  const [mostrarSenha, setMostrarSenha] = useState(false)

  async function handleSubmit(e) {
    e.preventDefault()
    setErro(null)
    setLoading(true)
    const form = new FormData(e.currentTarget)
    const payload = { email: form.get("email"), senha: form.get("senha") }
    try {
      const res = await authService.login(payload)
      setSession(res.user)
      navigate("/buscar")
    } catch (err) {
      setErro(mensagemPorStatus(err, "Erro ao fazer login"))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="relative overflow-hidden bg-neutral-900 bg-cover bg-center min-h-[calc(100vh-4rem)] flex items-center" style={{ backgroundImage: "url(/images/login-fundo.jpg)" }}>
      <div aria-hidden="true" className="absolute inset-0 bg-neutral-900/55" />
      <div className="relative max-w-md w-full mx-auto px-4 py-14">
        <h1 className="font-display text-4xl font-bold text-center text-white">Login</h1>
        <p className="text-sm text-white/80 text-center mt-2 mb-8">Sabores artesanais de <strong className="text-secondary">várias categorias</strong> perto de você!</p>
        <div className="bg-white rounded-3xl shadow-lg p-8">
          {erro ? <p className="bg-danger/10 text-danger border border-danger rounded-2xl p-3 text-sm mb-4">{erro}</p> : null}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <label className={["flex items-center gap-2 rounded-full border bg-neutral-100 px-4 h-12 focus-within:ring-2 focus-within:ring-primary focus-within:border-primary", erro ? "border-danger" : "border-transparent"].join(" ")}>
              <Icon name="mail" className="h-5 w-5 text-neutral-400" />
              <span className="sr-only">Email</span>
              <input name="email" type="email" required placeholder="Email" className="bg-transparent flex-1 outline-none text-sm placeholder:text-neutral-400" />
            </label>
            <label className={["flex items-center gap-2 rounded-full border bg-neutral-100 px-4 h-12 focus-within:ring-2 focus-within:ring-primary focus-within:border-primary", erro ? "border-danger" : "border-transparent"].join(" ")}>
              <Icon name="lock" className="h-5 w-5 text-neutral-400" />
              <span className="sr-only">Senha</span>
              <input name="senha" type={mostrarSenha ? "text" : "password"} required placeholder="Senha" className="bg-transparent flex-1 outline-none text-sm placeholder:text-neutral-400" />
              <button type="button" onClick={() => setMostrarSenha((v) => !v)} aria-label={mostrarSenha ? "Ocultar senha" : "Mostrar senha"} className="text-neutral-400 hover:text-primary">
                <Icon name={mostrarSenha ? "eyeOff" : "eye"} className="h-5 w-5" />
              </button>
            </label>
            <button type="submit" disabled={loading} className="h-12 rounded-full bg-primary text-white text-sm font-bold tracking-widest hover:bg-primary-dark disabled:opacity-60 transition-colors">
              {loading ? "ENTRANDO..." : "LOGIN"}
            </button>
          </form>
        </div>
        <p className="text-sm text-white/80 mt-6 text-center">
          Não tem conta? <Link to="/registro" className="text-secondary font-semibold">Cadastre-se</Link>
        </p>
      </div>
    </div>
  )
}
