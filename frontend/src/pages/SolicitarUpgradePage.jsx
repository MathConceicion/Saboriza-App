import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { useAuth } from "../context/AuthContext"
import { useUi } from "../context/UiContext"
import { vendedorService } from "../services/vendedor.service"
import { categoriasService } from "../services/categorias.service"
import { authService } from "../services/auth.service"
import { usuariosService } from "../services/usuarios.service"
import { Button } from "../components/ui/Button"
import { Input } from "../components/ui/Input"
import { ImagemUpload } from "../components/product/ImagemUpload"
import { Badge } from "../components/ui/Badge"
import { validarCep, validarNome, validarHora, validarDiasFuncionamento, parseValidationErrors, getFieldError } from "../lib/validators"
import { mensagemPorStatus } from "../lib/apiErrors"

const DIAS_SEMANA = [
  { chave: "seg", rotulo: "Segunda" },
  { chave: "ter", rotulo: "Terça" },
  { chave: "qua", rotulo: "Quarta" },
  { chave: "qui", rotulo: "Quinta" },
  { chave: "sex", rotulo: "Sexta" },
  { chave: "sab", rotulo: "Sábado" },
  { chave: "dom", rotulo: "Domingo" },
]

export default function SolicitarUpgradePage() {
  const navigate = useNavigate()
  const { user, setSession, isVendorAprovado, isVendedor } = useAuth()
  const { showToast } = useUi()
  const [categorias, setCategorias] = useState([])
  const [errors, setErrors] = useState(null)
  const [global, setGlobal] = useState(null)
  const [loading, setLoading] = useState(false)
  const [enviado, setEnviado] = useState(false)
  const [fotoArquivo, setFotoArquivo] = useState(null)
  const [fotoKey, setFotoKey] = useState(0)
  const [diasFuncionamento, setDiasFuncionamento] = useState(["seg", "ter", "qua", "qui", "sex"])
  const [aberta, setAberta] = useState(true)

  useEffect(() => {
    categoriasService.listar().then((r) => setCategorias(r.items ?? r ?? [])).catch(() => setCategorias([]))
  }, [])

  function toggleDia(chave) {
    setDiasFuncionamento((prev) => (prev.includes(chave) ? prev.filter((d) => d !== chave) : [...prev, chave]))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setErrors(null)
    setGlobal(null)
    const form = new FormData(event.currentTarget)
    const payload = {
      nomeLoja: String(form.get("nomeLoja") ?? "").trim(),
      categoriaLojaId: String(form.get("categoriaLojaId") ?? ""),
      enderecoComercial: String(form.get("enderecoComercial") ?? "").trim(),
      cep: String(form.get("cep") ?? "").trim(),
      telefone: String(form.get("telefone") ?? "").trim(),
      cnpj: String(form.get("cnpj") ?? "").trim() || null,
      fotoUrl: null,
      horarioAbertura: String(form.get("horarioAbertura") ?? "").trim() || null,
      horarioFechamento: String(form.get("horarioFechamento") ?? "").trim() || null,
      diasFuncionamento,
      aberta,
    }
    const locais = {}
    if (!validarNome(payload.nomeLoja)) locais.NomeLoja = ["Nome da loja precisa de 3 a 150 caracteres."]
    if (!payload.categoriaLojaId) locais.CategoriaLojaId = ["Escolha a categoria."]
    if (!validarNome(payload.enderecoComercial)) locais.EnderecoComercial = ["Endereço comercial é obrigatório."]
    if (!validarCep(payload.cep)) locais.Cep = ["CEP inválido. Digite os 8 números."]
    if (!validarDiasFuncionamento(payload.diasFuncionamento)) locais.DiasFuncionamento = ["Selecione ao menos um dia de funcionamento."]
    if (Boolean(payload.horarioAbertura) !== Boolean(payload.horarioFechamento)) locais.HorarioAbertura = ["Informe abertura e fechamento juntos."]
    if (payload.horarioAbertura && !validarHora(payload.horarioAbertura)) locais.HorarioAbertura = ["Abertura inválida. Use HH:mm."]
    if (payload.horarioFechamento && !validarHora(payload.horarioFechamento)) locais.HorarioFechamento = ["Fechamento inválido. Use HH:mm."]
    if (Object.keys(locais).length > 0) {
      setErrors(locais)
      return
    }
    setLoading(true)
    try {
      if (fotoArquivo) {
        const up = await usuariosService.enviarImagem(fotoArquivo)
        payload.fotoUrl = up.imagemUrl
      }
      await vendedorService.solicitarUpgrade(payload)
      setEnviado(true)
      showToast("Pedido de upgrade enviado", "success")
    } catch (err) {
      const ve = parseValidationErrors(err)
      if (ve) {
        setErrors(ve)
        return
      }
      const msg = mensagemPorStatus(err, "Não foi possível enviar o pedido.")
      setGlobal(msg)
      showToast(msg, "danger")
    } finally {
      setLoading(false)
    }
  }

  async function handleAtualizarSessao() {
    try {
      const res = await authService.refresh()
      if (res?.user) setSession(res.user)
      showToast("Sessão atualizada", "success")
    } catch (e) {
      showToast(mensagemPorStatus(e, "Ainda sem aprovação."), "warning")
    }
  }

  if (isVendorAprovado) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-8">
        <div className="flex items-center gap-2 mb-4">
          <h1 className="text-2xl font-bold">Quero vender</h1>
          <Badge tone="success">Aprovado</Badge>
        </div>
        <p className="text-neutral-600 mb-4">Sua loja já está aprovada.</p>
        <Button onClick={() => navigate("/vendedor/dashboard")}>Ir para o painel</Button>
      </div>
    )
  }

  if (enviado || (isVendedor && !isVendorAprovado)) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-8">
        <div className="flex items-center gap-2 mb-4">
          <h1 className="text-2xl font-bold">Pedido recebido</h1>
          <Badge tone="warning">Pendente de aprovação</Badge>
        </div>
        <p className="text-sm text-neutral-600 mb-4">Olá, {user?.nome ?? user?.email}. Um admin precisa aprovar sua loja. Depois da aprovação, atualize a sessão para liberar o painel do vendedor.</p>
        <div className="flex gap-3">
          <Button variant="secondary" onClick={handleAtualizarSessao}>Atualizar sessão</Button>
          <Button variant="ghost" onClick={() => navigate("/")}>Voltar ao início</Button>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">Quero vender</h1>
      {global ? <p className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4">{global}</p> : null}
      <form onSubmit={handleSubmit} className="space-y-4 bg-white p-6 rounded-xl border border-neutral-200">
        <Input name="nomeLoja" label="Nome da loja" required error={getFieldError(errors, "NomeLoja")} />
        <label className="text-sm flex flex-col gap-1">
          <span className="font-medium">Categoria</span>
          <select name="categoriaLojaId" required className="h-11 rounded-xl border border-neutral-200 bg-white px-3">
            <option value="">Selecione...</option>
            {categorias.map((c) => (<option key={c.id} value={c.id}>{c.nome}</option>))}
          </select>
          {getFieldError(errors, "CategoriaLojaId") ? <span className="text-danger text-sm">{getFieldError(errors, "CategoriaLojaId")}</span> : null}
        </label>
        <Input name="enderecoComercial" label="Endereço completo" required error={getFieldError(errors, "EnderecoComercial")} />
        <div className="grid grid-cols-2 gap-3">
          <Input name="cep" label="CEP" required inputMode="numeric" error={getFieldError(errors, "Cep")} />
          <Input name="telefone" label="Telefone para contato" required error={getFieldError(errors, "Telefone")} />
        </div>
        <Input name="cnpj" label="CNPJ da loja (opcional)" error={getFieldError(errors, "Cnpj")} />
        <fieldset className="border border-neutral-200 rounded-xl p-4">
          <legend className="text-sm font-medium text-neutral-900 px-2">Dias de funcionamento</legend>
          <div className="grid grid-cols-4 sm:grid-cols-7 gap-2 mt-2">
            {DIAS_SEMANA.map((dia) => (
              <label key={dia.chave} className="flex flex-col items-center gap-1 cursor-pointer rounded-lg border p-2 text-sm hover:bg-neutral-50 transition-colors">
                <input
                  type="checkbox"
                  checked={diasFuncionamento.includes(dia.chave)}
                  onChange={() => toggleDia(dia.chave)}
                  className="w-4 h-4 rounded text-primary ring-offset-white focus:ring-primary"
                />
                <span className={diasFuncionamento.includes(dia.chave) ? "font-semibold text-primary" : "text-neutral-600"}>{dia.rotulo}</span>
              </label>
            ))}
          </div>
          {getFieldError(errors, "DiasFuncionamento") ? <p className="text-danger text-sm mt-2">{getFieldError(errors, "DiasFuncionamento")}</p> : null}
        </fieldset>
        <div className="flex items-center gap-3 rounded-xl border border-neutral-200 bg-white p-3">
          <input
            id="toggle-aberta"
            type="checkbox"
            checked={aberta}
            onChange={() => setAberta(!aberta)}
            className="w-5 h-5 rounded text-primary ring-offset-white focus:ring-primary"
          />
          <label htmlFor="toggle-aberta" className="text-sm font-medium text-neutral-900 cursor-pointer">Loja aberta</label>
          <span className="ml-auto text-xs text-neutral-500">Status de funcionamento (independente do horário habitual)</span>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Input name="horarioAbertura" label="Abre às (horário habitual)" type="time" error={getFieldError(errors, "HorarioAbertura")} />
          <Input name="horarioFechamento" label="Fecha às (horário habitual)" type="time" error={getFieldError(errors, "HorarioFechamento")} />
        </div>
        <ImagemUpload key={fotoKey} rotulo="Foto da loja" onArquivo={setFotoArquivo} error={getFieldError(errors, "FotoUrl")} />
        <Button type="submit" loading={loading} fullWidth>Enviar para aprovação</Button>
      </form>
    </div>
  )
}
