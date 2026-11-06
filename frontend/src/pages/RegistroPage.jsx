import { useState } from "react"
import { useNavigate, Link, useSearchParams } from "react-router-dom"
import { authService } from "../services/auth.service"
import { useAuth } from "../context/AuthContext"
import { Icon } from "../components/ui/Icon"
import { parseValidationErrors, getFieldError } from "../lib/validators"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function RegistroPage() {
  const navigate = useNavigate()
  const { setSession } = useAuth()
  const [searchParams] = useSearchParams()
  const emailPreenchido = (searchParams.get("email") ?? "").trim()
  const [emailEditavel, setEmailEditavel] = useState(false)
  const emailTravado = emailPreenchido.length > 0 && !emailEditavel
  const [errors, setErrors] = useState(null)
  const [global, setGlobal] = useState(null)
  const [loading, setLoading] = useState(false)
  const [mostrarSenha, setMostrarSenha] = useState(false)

  async function handleSubmit(e) {
    e.preventDefault()
    setErrors(null)
    setGlobal(null)
    setLoading(true)
    const form = new FormData(e.currentTarget)
    const payload = { nome: form.get("nome"), email: form.get("email"), senha: form.get("senha"), cpf: form.get("cpf") || null }
    try {
      const res = await authService.registrar(payload)
      setSession(res.user)
      navigate("/buscar")
    } catch (err) {
      const ve = parseValidationErrors(err)
      if (ve) {
        setErrors(ve)
        return
      }
      setGlobal(mensagemPorStatus(err, "Erro ao registrar"))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="relative overflow-hidden bg-neutral-900 bg-cover bg-center min-h-[calc(100vh-4rem)] flex items-center" style={{ backgroundImage: "url(/images/register-fundo.jpg)" }}>
      <div aria-hidden="true" className="absolute inset-0 bg-neutral-900/55" />
      <div className="relative max-w-md w-full mx-auto px-4 py-14">
        <h1 className="font-display text-4xl font-bold text-center text-white">Cadastro</h1>
        <p className="text-sm text-white/80 text-center mt-2 mb-8">Crie sua conta e descubra <strong className="text-secondary">sabores artesanais</strong>!</p>
        <div className="bg-white rounded-3xl shadow-lg p-8">
          {global ? <p className="bg-danger/10 text-danger border border-danger rounded-2xl p-3 text-sm mb-4">{global}</p> : null}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <label className="flex items-center gap-2 rounded-full border border-transparent bg-neutral-100 px-4 h-12 focus-within:ring-2 focus-within:ring-primary focus-within:border-primary">
              <Icon name="user" className="h-5 w-5 text-neutral-400" />
              <span className="sr-only">Nome completo</span>
              <input name="nome" required placeholder="Nome completo" className="bg-transparent flex-1 outline-none text-sm placeholder:text-neutral-400" />
            </label>
            {getFieldError(errors, "Nome") ? <p className="text-danger text-sm -mt-2 ml-4">{getFieldError(errors, "Nome")}</p> : null}
            <label className="flex items-center gap-2 rounded-full border border-transparent bg-neutral-100 px-4 h-12 focus-within:ring-2 focus-within:ring-primary focus-within:border-primary">
              <Icon name="mail" className="h-5 w-5 text-neutral-400" />
              <span className="sr-only">Email</span>
              <input name="email" type="email" required placeholder="Email" defaultValue={emailPreenchido} readOnly={emailTravado} className="bg-transparent flex-1 outline-none text-sm placeholder:text-neutral-400 disabled:text-neutral-500" />
              {emailTravado ? (
                <button type="button" onClick={() => setEmailEditavel(true)} aria-label="Editar e-mail" className="text-neutral-400 hover:text-primary">
                  <span aria-hidden="true">✎</span>
                </button>
              ) : null}
            </label>
            {emailTravado ? <p className="text-xs text-neutral-500 -mt-2 ml-4">E-mail vindo do cadastro inicial. Toque no lápis para ajustar.</p> : null}
            {getFieldError(errors, "Email") ? <p className="text-danger text-sm -mt-2 ml-4">{getFieldError(errors, "Email")}</p> : null}
            <label className="flex items-center gap-2 rounded-full border border-transparent bg-neutral-100 px-4 h-12 focus-within:ring-2 focus-within:ring-primary focus-within:border-primary">
              <Icon name="lock" className="h-5 w-5 text-neutral-400" />
              <span className="sr-only">Senha</span>
              <input name="senha" type={mostrarSenha ? "text" : "password"} required placeholder="Senha" className="bg-transparent flex-1 outline-none text-sm placeholder:text-neutral-400" />
              <button type="button" onClick={() => setMostrarSenha((v) => !v)} aria-label={mostrarSenha ? "Ocultar senha" : "Mostrar senha"} className="text-neutral-400 hover:text-primary">
                <Icon name={mostrarSenha ? "eyeOff" : "eye"} className="h-5 w-5" />
              </button>
            </label>
            {getFieldError(errors, "Senha") ? <p className="text-danger text-sm -mt-2 ml-4">{getFieldError(errors, "Senha")}</p> : null}
            <label className="flex items-center gap-2 rounded-full border border-transparent bg-neutral-100 px-4 h-12 focus-within:ring-2 focus-within:ring-primary focus-within:border-primary">
              <Icon name="id" className="h-5 w-5 text-neutral-400" />
              <span className="sr-only">CPF (opcional)</span>
              <input name="cpf" placeholder="CPF (opcional)" inputMode="numeric" className="bg-transparent flex-1 outline-none text-sm placeholder:text-neutral-400" />
            </label>
            {getFieldError(errors, "Cpf") ? <p className="text-danger text-sm -mt-2 ml-4">{getFieldError(errors, "Cpf")}</p> : null}
            <button type="submit" disabled={loading} className="h-12 rounded-full bg-primary text-white text-sm font-bold tracking-widest hover:bg-primary-dark disabled:opacity-60 transition-colors">
              {loading ? "CRIANDO..." : "CADASTRAR"}
            </button>
          </form>
        </div>
        <p className="text-sm text-white/80 mt-6 text-center">
          Já tem conta? <Link to="/login" className="text-secondary font-semibold">Entrar</Link>
        </p>
      </div>
    </div>
  )
}
