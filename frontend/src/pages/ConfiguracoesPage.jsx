import { useEffect, useState } from "react"
import { vendedorService } from "../services/vendedor.service"
import { usuariosService } from "../services/usuarios.service"
import { useUi } from "../context/UiContext"
import { Button } from "../components/ui/Button"
import { Input } from "../components/ui/Input"
import { ImagemUpload } from "../components/product/ImagemUpload"
import { Skeleton } from "../components/ui/Skeleton"
import { parseValidationErrors, getFieldError, validarNome, validarCep, validarDiasFuncionamento, validarHora } from "../lib/validators"
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

export default function ConfiguracoesPage() {
  const { showToast } = useUi()
  const [loja, setLoja] = useState(null)
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)
  const [errors, setErrors] = useState(null)
  const [salvando, setSalvando] = useState(false)
  const [fotoArquivo, setFotoArquivo] = useState(null)
  const [fotoKey, setFotoKey] = useState(0)
  const [diasFuncionamento, setDiasFuncionamento] = useState([])
  const [aberta, setAberta] = useState(true)

  function carregar() {
    setLoading(true)
    setErro(null)
    vendedorService
      .obterLoja()
      .then((r) => {
        setLoja(r)
        setDiasFuncionamento(Array.isArray(r?.diasFuncionamento) ? r.diasFuncionamento : [])
        setAberta(Boolean(r?.aberta ?? true))
      })
      .catch((e) => {
        setLoja(null)
        setErro(mensagemPorStatus(e, "Não foi possível carregar os dados da loja."))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [])

  function toggleDia(chave) {
    setDiasFuncionamento((prev) => (prev.includes(chave) ? prev.filter((d) => d !== chave) : [...prev, chave]))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setErrors(null)
    const form = new FormData(event.currentTarget)
    const payload = {
      nome: String(form.get("nome") ?? "").trim(),
      descricao: String(form.get("descricao") ?? "").trim() || null,
      fotoUrl: loja?.fotoUrl ?? null,
      cnpj: String(form.get("cnpj") ?? "").trim() || null,
      telefone: String(form.get("telefone") ?? "").trim() || null,
      enderecoComercial: String(form.get("enderecoComercial") ?? "").trim(),
      cep: String(form.get("cep") ?? "").trim(),
      diasFuncionamento,
      aberta,
      horarioAbertura: String(form.get("horarioAbertura") ?? "").trim() || null,
      horarioFechamento: String(form.get("horarioFechamento") ?? "").trim() || null,
    }
    if (!validarNome(payload.nome)) {
      setErrors({ Nome: ["Nome da loja precisa de 3 a 150 caracteres."] })
      return
    }
    if (!validarCep(payload.cep)) {
      setErrors({ Cep: ["CEP inválido. Digite os 8 números."] })
      return
    }
    if (!validarDiasFuncionamento(payload.diasFuncionamento)) {
      setErrors({ DiasFuncionamento: ["Selecione ao menos um dia de funcionamento."] })
      return
    }
    if (Boolean(payload.horarioAbertura) !== Boolean(payload.horarioFechamento)) {
      setErrors({ HorarioAbertura: ["Informe abertura e fechamento juntos."] })
      return
    }
    if (payload.horarioAbertura && !validarHora(payload.horarioAbertura)) {
      setErrors({ HorarioAbertura: ["Abertura inválida. Use HH:mm."] })
      return
    }
    if (payload.horarioFechamento && !validarHora(payload.horarioFechamento)) {
      setErrors({ HorarioFechamento: ["Fechamento inválido. Use HH:mm."] })
      return
    }
    setSalvando(true)
    try {
      if (fotoArquivo) {
        const up = await usuariosService.enviarImagem(fotoArquivo)
        payload.fotoUrl = up.imagemUrl
      }
      await vendedorService.atualizarLoja(payload)
      setFotoArquivo(null)
      setFotoKey((k) => k + 1)
      showToast("Loja atualizada", "success")
      carregar()
    } catch (err) {
      const ve = parseValidationErrors(err)
      if (ve) {
        setErrors(ve)
        return
      }
      showToast(mensagemPorStatus(err, "Não foi possível salvar."), "danger")
    } finally {
      setSalvando(false)
    }
  }

  if (loading) return <Skeleton lines={5} />
  if (erro || !loja) {
    return (
      <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm flex items-center justify-between">
        <span>{erro ?? "Loja não encontrada."}</span>
        <Button size="sm" variant="ghost" onClick={carregar}>Tentar novamente</Button>
      </div>
    )
  }

  return (
    <div className="max-w-xl">
      <h2 className="text-xl font-semibold mb-2">Configurações da loja</h2>
      <p className="text-sm text-neutral-600 mb-4">{loja.categoriaNome} • {loja.enderecoComercial} — {loja.cep}</p>
      <form onSubmit={handleSubmit} className="space-y-4 bg-white p-6 rounded-xl border border-neutral-200">
        <Input name="nome" label="Nome da loja" required defaultValue={loja.nome} error={getFieldError(errors, "Nome")} />
        <Input name="descricao" label="Descrição" defaultValue={loja.descricao ?? ""} error={getFieldError(errors, "Descricao")} />
        <div className="grid grid-cols-2 gap-3">
          <Input name="cnpj" label="CNPJ da loja" defaultValue={loja.cnpj ?? ""} error={getFieldError(errors, "Cnpj")} />
          <Input name="telefone" label="Telefone para contato" defaultValue={loja.telefone ?? ""} error={getFieldError(errors, "Telefone")} />
        </div>
        <Input name="enderecoComercial" label="Endereço completo" required defaultValue={loja.enderecoComercial} error={getFieldError(errors, "EnderecoComercial")} />
        <Input name="cep" label="CEP" required inputMode="numeric" defaultValue={loja.cep} error={getFieldError(errors, "Cep")} />
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
          <span className="ml-auto text-xs text-neutral-500">Independente do horário habitual</span>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Input name="horarioAbertura" label="Abre às (horário habitual)" type="time" defaultValue={loja.horarioAbertura ?? ""} error={getFieldError(errors, "HorarioAbertura")} />
          <Input name="horarioFechamento" label="Fecha às (horário habitual)" type="time" defaultValue={loja.horarioFechamento ?? ""} error={getFieldError(errors, "HorarioFechamento")} />
        </div>
        <ImagemUpload key={`${loja.lojaId}-${fotoKey}`} rotulo="Foto da loja" imagemAtual={loja.fotoUrl} onArquivo={setFotoArquivo} error={getFieldError(errors, "FotoUrl")} />
        <Button type="submit" loading={salvando} fullWidth>Salvar alterações</Button>
      </form>
    </div>
  )
}
