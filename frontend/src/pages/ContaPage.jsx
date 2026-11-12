import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { useAuth } from "../context/AuthContext"
import { useUi } from "../context/UiContext"
import { usuariosService } from "../services/usuarios.service"
import { authService } from "../services/auth.service"
import { Button } from "../components/ui/Button"
import { Input } from "../components/ui/Input"
import { Card } from "../components/ui/Card"
import { Icon } from "../components/ui/Icon"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function ContaPage() {
  const navigate = useNavigate()
  const { user, setSession, clearSession } = useAuth()
  const { showToast } = useUi()
  const [erro, setErro] = useState(null)
  const [ocupado, setOcupado] = useState(false)
  const [tipo, setTipo] = useState("DadosSaude")
  const [motivo, setMotivo] = useState("")
  const [confirmando, setConfirmando] = useState(false)

  async function handleExportar() {
    setErro(null)
    setOcupado(true)
    try {
      const blob = await usuariosService.exportarDados(user.id)
      const url = window.URL.createObjectURL(new Blob([blob], { type: "application/json" }))
      const a = document.createElement("a")
      a.href = url
      a.download = `dados-portabilidade-${user.id}.json`
      a.click()
      window.URL.revokeObjectURL(url)
      showToast("Arquivo de portabilidade baixado", "success")
    } catch (e) {
      const msg = mensagemPorStatus(e, "Não foi possível exportar seus dados.")
      setErro(msg)
      showToast(msg, "danger")
    } finally {
      setOcupado(false)
    }
  }

  async function handleRevogarConsentimento() {
    setErro(null)
    setOcupado(true)
    try {
      await usuariosService.revogarConsentimento(tipo, motivo || null)
      showToast("Consentimento revogado", "success")
      setMotivo("")
    } catch (e) {
      const msg = mensagemPorStatus(e, "Não foi possível revogar.")
      setErro(msg)
      showToast(msg, "danger")
    } finally {
      setOcupado(false)
    }
  }

  async function handleAnonimizar() {
    setErro(null)
    setOcupado(true)
    try {
      await usuariosService.anonimizar(user.id)
      await clearSession()
      navigate("/")
    } catch (e) {
      const msg = mensagemPorStatus(e, "Não foi possível anonimizar.")
      setErro(msg)
      showToast(msg, "danger")
      setOcupado(false)
      setConfirmando(false)
    }
  }

  async function handleAtualizarSessao() {
    try {
      const res = await authService.refresh()
      if (res?.user) setSession(res.user)
      else {
        const me = await authService.me()
        setSession(me)
      }
      showToast("Sessão atualizada", "success")
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível atualizar."), "danger")
    }
  }

  return (
    <div className="max-w-3xl mx-auto px-4 py-8 bg-neutral-50 min-h-screen">
      <header className="mb-8 border-b border-neutral-200 pb-6">
        <h1 className="text-3xl font-bold tracking-tight text-neutral-900 mb-2">Minha conta</h1>
        <p className="text-neutral-600 text-sm">Gerencie seus dados e configurações</p>
      </header>

      <main className="space-y-6">
        {/* Dados do usuário */}
        <section>
          <Card className="p-6 md:p-8 space-y-6">
            <h2 className="font-display text-2xl font-bold tracking-tight text-neutral-900">
              Dados da conta
            </h2>

            <div className="space-y-4">
              {/* Nome */}
              <div className="flex items-start gap-3">
                <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full border border-neutral-200 bg-neutral-100 text-neutral-500">
                  <Icon name="user" className="h-5 w-5" />
                </div>
                <div>
                  <p className="text-sm text-neutral-500">Nome</p>
                  <p className="font-medium text-neutral-900 line-clamp-2">
                    {user?.nome ?? user?.email}
                  </p>
                </div>
              </div>

              {/* Email */}
              <div className="flex items-start gap-3">
                <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full border border-neutral-200 bg-neutral-100 text-neutral-500">
                  <Icon name="mail" className="h-5 w-5" />
                </div>
                <div>
                  <p className="text-sm text-neutral-500">Email</p>
                  <p className="font-medium text-neutral-900">{user?.email}</p>
                </div>
              </div>
            </div>

            <div className="mt-6 pt-6 border-t border-neutral-200 flex flex-wrap gap-3">
              <Button size="sm" variant="ghost" onClick={handleAtualizarSessao}>
                Atualizar sessão
              </Button>
              <Button
                size="sm"
                variant="secondary"
                onClick={handleExportar}
              >
                Exportar meus dados
              </Button>
            </div>
          </Card>
        </section>

        {/* Revogar consentimento */}
        <section>
          <Card className="p-6">
            <h2 className="font-semibold text-neutral-900 mb-4">Revogar consentimento</h2>

            <p className="text-neutral-600 text-sm mb-4">
              Você pode revogar o consentimento para tipos específicos de dados. A revogação
              é permanente e os dados serão removidos de nossos sistemas.
            </p>

            <div className="space-y-4">
              <label className="text-sm font-medium text-neutral-900">
                Tipo de consentimento
                <select
                  value={tipo}
                  onChange={(e) => setTipo(e.target.value)}
                  className="mt-1 block w-full rounded-xl border border-neutral-200 bg-white py-2 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                >
                  <option value="DadosSaude">Dados de saúde</option>
                  <option value="Geolocalizacao">Geolocalização</option>
                  <option value="Marketing">Marketing</option>
                </select>
              </label>

              <Input
                label="Motivo (opcional)"
                value={motivo}
                onChange={(e) => setMotivo(e.target.value)}
                placeholder="Por que está revogando este consentimento?"
                disabled={ocupado}
              />

              <div className="flex gap-3 flex-wrap">
                <Button
                  size="sm"
                  variant="ghost"
                  fullWidth
                  onClick={() => setConfirmando(false)}
                >
                  Cancelar
                </Button>
                <Button
                  size="sm"
                  variant="danger"
                  fullWidth
                  loading={ocupado}
                  onClick={handleRevogarConsentimento}
                >
                  Revogar
                </Button>
              </div>
            </div>
          </Card>
        </section>

        {/* Zona de risco - Anonimização */}
        <section>
          <Card className="p-6 border border-danger">
            <h2 className="font-semibold text-danger mb-3">
              <span className="text-danger">Zona de risco</span>
            </h2>

            {!confirmando ? (
              <Button
                variant="danger"
                onClick={() => setConfirmando(true)}
                className="w-full justify-center"
              >
                Anonimizar minha conta
              </Button>
            ) : (
              <div className="flex flex-col gap-4 pt-4">
                <p className="text-neutral-600 text-sm">
                  Isso apaga seu nome, email, CPF e dados de saúde sem volta. Esta ação não pode
                  ser desfeita. Confirmar?
                </p>

                <div className="flex gap-3">
                  <Button
                    variant="ghost"
                    fullWidth
                    onClick={() => setConfirmando(false)}
                  >
                    Cancelar
                  </Button>
                  <Button
                    variant="danger"
                    fullWidth
                    loading={ocupado}
                    onClick={handleAnonimizar}
                  >
                    Confirmar
                  </Button>
                </div>
              </div>
            )}
          </Card>
        </section>
      </main>
    </div>
  )
}

ContaPage.propTypes = {}