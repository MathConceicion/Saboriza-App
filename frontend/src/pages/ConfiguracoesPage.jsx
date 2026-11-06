import { useEffect, useState } from "react"
import { vendedorService } from "../services/vendedor.service"
import { usuariosService } from "../services/usuarios.service"
import { useUi } from "../context/UiContext"
import { Button } from "../components/ui/Button"
import { Input } from "../components/ui/Input"
import { ImagemUpload } from "../components/product/ImagemUpload"
import { Skeleton } from "../components/ui/Skeleton"
import { parseValidationErrors, getFieldError, validarNome } from "../lib/validators"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function ConfiguracoesPage() {
  const { showToast } = useUi()
  const [loja, setLoja] = useState(null)
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)
  const [errors, setErrors] = useState(null)
  const [salvando, setSalvando] = useState(false)
  const [fotoArquivo, setFotoArquivo] = useState(null)
  const [fotoKey, setFotoKey] = useState(0)

  function carregar() {
    setLoading(true)
    setErro(null)
    vendedorService
      .obterLoja()
      .then((r) => setLoja(r))
      .catch((e) => {
        setLoja(null)
        setErro(mensagemPorStatus(e, "Não foi possível carregar os dados da loja."))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [])

  async function handleSubmit(event) {
    event.preventDefault()
    setErrors(null)
    const form = new FormData(event.currentTarget)
    const payload = {
      nome: String(form.get("nome") ?? "").trim(),
      descricao: String(form.get("descricao") ?? "").trim() || null,
      fotoUrl: loja?.fotoUrl ?? null,
      horarioAbertura: String(form.get("horarioAbertura") ?? "").trim() || null,
      horarioFechamento: String(form.get("horarioFechamento") ?? "").trim() || null,
    }
    if (!validarNome(payload.nome)) {
      setErrors({ Nome: ["Nome da loja precisa de 3 a 150 caracteres."] })
      return
    }
    if (Boolean(payload.horarioAbertura) !== Boolean(payload.horarioFechamento)) {
      setErrors({ HorarioAbertura: ["Informe abertura e fechamento juntos."] })
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
          <Input name="horarioAbertura" label="Abre às" type="time" defaultValue={loja.horarioAbertura ?? ""} error={getFieldError(errors, "HorarioAbertura")} />
          <Input name="horarioFechamento" label="Fecha às" type="time" defaultValue={loja.horarioFechamento ?? ""} error={getFieldError(errors, "HorarioFechamento")} />
        </div>
        <ImagemUpload key={`${loja.lojaId}-${fotoKey}`} rotulo="Foto da loja" imagemAtual={loja.fotoUrl} onArquivo={setFotoArquivo} error={getFieldError(errors, "FotoUrl")} />
        <Button type="submit" loading={salvando} fullWidth>Salvar alterações</Button>
      </form>
    </div>
  )
}
