import { useEffect, useState } from "react"
import { useAuth } from "../context/AuthContext"
import { useUi } from "../context/UiContext"
import { lojasService } from "../services/lojas.service"
import { produtosService } from "../services/produtos.service"
import { Button } from "../components/ui/Button"
import { Input } from "../components/ui/Input"
import { Badge } from "../components/ui/Badge"
import { Modal } from "../components/ui/Modal"
import { Icon } from "../components/ui/Icon"
import { ImagemUpload } from "../components/product/ImagemUpload"
import { Skeleton } from "../components/ui/Skeleton"
import { EmptyState } from "../components/ui/EmptyState"
import { formatPreco } from "../lib/formatters"
import { validarNome, validarPreco, parseValidationErrors, getFieldError } from "../lib/validators"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function ProdutosPage() {
  const { user } = useAuth()
  const { showToast } = useUi()
  const [produtos, setProdutos] = useState([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)
  const [errors, setErrors] = useState(null)
  const [salvando, setSalvando] = useState(false)
  const [imagemArquivo, setImagemArquivo] = useState(null)
  const [formKey, setFormKey] = useState(0)
  const [editando, setEditando] = useState(null)
  const [editArquivo, setEditArquivo] = useState(null)
  const [errosEdicao, setErrosEdicao] = useState(null)
  const [salvandoEdicao, setSalvandoEdicao] = useState(false)
  const [pausandoId, setPausandoId] = useState(null)
  const [confirmandoId, setConfirmandoId] = useState(null)
  const [excluindoId, setExcluindoId] = useState(null)
  const [criando, setCriando] = useState(false)

  function carregar() {
    if (!user?.lojaId) {
      setLoading(false)
      return
    }
    setLoading(true)
    setErro(null)
    lojasService
      .obterProdutos(user.lojaId)
      .then((r) => setProdutos(r.items ?? r ?? []))
      .catch((e) => {
        setProdutos([])
        setErro(mensagemPorStatus(e, "Não foi possível carregar produtos."))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [user?.lojaId])

  function validarPayload(payload) {
    const locais = {}
    if (!validarNome(payload.nome)) locais.Nome = ["Nome precisa de 3 a 150 caracteres."]
    if (!validarPreco(payload.preco)) locais.Preco = ["Preço deve ser maior que zero."]
    if (!Number.isInteger(payload.quantidadeEmEstoque) || payload.quantidadeEmEstoque < 0) locais.QuantidadeEmEstoque = ["Estoque deve ser inteiro maior ou igual a zero."]
    return locais
  }

  function lerForm(form) {
    return {
      nome: String(form.get("nome") ?? "").trim(),
      descricao: String(form.get("descricao") ?? "").trim() || null,
      preco: Number(form.get("preco")),
      quantidadeEmEstoque: Number(form.get("quantidadeEmEstoque")),
      imagemUrl: String(form.get("imagemUrl") ?? "").trim() || null,
    }
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setErrors(null)
    const form = event.currentTarget
    const payload = lerForm(new FormData(form))
    const locais = validarPayload(payload)
    if (Object.keys(locais).length > 0) {
      setErrors(locais)
      return
    }
    setSalvando(true)
    try {
      if (imagemArquivo) {
        const up = await produtosService.enviarImagem(imagemArquivo)
        payload.imagemUrl = up.imagemUrl
      }
      await produtosService.criar(payload)
      form.reset()
      setImagemArquivo(null)
      setFormKey((k) => k + 1)
      setCriando(false)
      showToast("Produto publicado no portfólio", "success")
      carregar()
    } catch (err) {
      const ve = parseValidationErrors(err)
      if (ve) {
        setErrors(ve)
        return
      }
      showToast(mensagemPorStatus(err, "Não foi possível criar."), "danger")
    } finally {
      setSalvando(false)
    }
  }

  async function handleSalvarEdicao(event) {
    event.preventDefault()
    setErrosEdicao(null)
    const payload = lerForm(new FormData(event.currentTarget))
    const locais = validarPayload(payload)
    if (Object.keys(locais).length > 0) {
      setErrosEdicao(locais)
      return
    }
    setSalvandoEdicao(true)
    try {
      if (editArquivo) {
        const up = await produtosService.enviarImagem(editArquivo)
        payload.imagemUrl = up.imagemUrl
      }
      await produtosService.atualizar(editando.id, payload)
      setEditando(null)
      setEditArquivo(null)
      showToast("Produto atualizado", "success")
      carregar()
    } catch (err) {
      const ve = parseValidationErrors(err)
      if (ve) {
        setErrosEdicao(ve)
        return
      }
      showToast(mensagemPorStatus(err, "Não foi possível atualizar."), "danger")
    } finally {
      setSalvandoEdicao(false)
    }
  }

  async function handleAlternar(produto) {
    if (pausandoId !== produto.id) {
      setPausandoId(produto.id)
      setConfirmandoId(null)
      return
    }
    setPausandoId(null)
    try {
      await produtosService.alternar(produto.id, !produto.ativo)
      showToast(produto.ativo ? "Produto pausado" : "Produto publicado", "success")
      carregar()
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível alterar."), "danger")
    }
  }

  async function handleExcluir(produto) {
    if (confirmandoId !== produto.id) {
      setConfirmandoId(produto.id)
      setPausandoId(null)
      return
    }
    setConfirmandoId(null)
    setExcluindoId(produto.id)
    try {
      await produtosService.excluir(produto.id)
      showToast("Produto apagado", "success")
      carregar()
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível apagar."), "danger")
    } finally {
      setExcluindoId(null)
    }
  }

  return (
    <div>
      <div className="flex items-start justify-between gap-3 mb-2">
        <div>
          <h2 className="text-xl font-semibold">Portfólio</h2>
          <p className="text-sm text-neutral-600 mt-1">Itens do seu portfólio, visíveis no cardápio assim que criados.</p>
        </div>
        <Button size="sm" onClick={() => { setErrors(null); setCriando(true) }}>+ Novo produto</Button>
      </div>
      {loading ? <Skeleton variant="card" /> : null}
      {!loading && erro ? (
        <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4 flex items-center justify-between">
          <span>{erro}</span>
          <Button size="sm" variant="ghost" onClick={carregar}>Tentar novamente</Button>
        </div>
      ) : null}
      {!loading && !erro && produtos.length === 0 ? <EmptyState titulo="Nenhum produto" descricao="Clique em Novo produto para cadastrar o primeiro item." actionLabel="Novo produto" onAction={() => setCriando(true)} /> : null}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4 mt-4">
            {produtos.map((p) => (
              <div key={p.id} className="relative bg-white rounded-xl border border-neutral-200 p-4">
                {confirmandoId === p.id ? (
                  <button onClick={() => handleExcluir(p)} disabled={excluindoId === p.id} className="absolute top-2 right-2 text-xs font-bold text-white bg-danger rounded-full px-3 py-1.5 hover:opacity-80 disabled:opacity-50">
                    Excluir?
                  </button>
                ) : (
                  <button onClick={() => handleExcluir(p)} aria-label={`Apagar ${p.nome}`} className="absolute top-2 right-2 text-neutral-400 hover:text-danger rounded-full p-1.5 hover:bg-danger/10">
                    <Icon name="x" className="h-5 w-5" />
                  </button>
                )}
                <div className="flex items-center gap-2 mb-2 pr-8">
                  <h3 className="font-semibold">{p.nome}</h3>
                  {p.ativo ? <Badge tone="success">Publicado</Badge> : <Badge tone="neutral">Pausado</Badge>}
                </div>
                <p className="text-sm text-neutral-600 line-clamp-2">{p.descricao}</p>
                <div className="flex items-center justify-between mt-3 text-sm">
                  <span className="font-bold text-primary">{formatPreco(p.preco)}</span>
                  <span className="text-neutral-600">Estoque: {p.quantidadeEmEstoque}</span>
                </div>
                <div className="flex gap-2 mt-3">
                  <Button size="sm" variant="secondary" fullWidth onClick={() => { setEditando(p); setErrosEdicao(null); setEditArquivo(null); setPausandoId(null); setConfirmandoId(null) }}>Editar</Button>
                  {pausandoId === p.id ? (
                    <Button size="sm" variant="danger" fullWidth onClick={() => handleAlternar(p)}>Confirmar</Button>
                  ) : (
                    <Button size="sm" variant="ghost" fullWidth onClick={() => handleAlternar(p)}>{p.ativo ? "Pausar" : "Publicar"}</Button>
                  )}
                </div>
              </div>
            ))}
      </div>
      <Modal open={criando} onClose={() => setCriando(false)} title="Novo produto">
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input name="nome" label="Nome" required error={getFieldError(errors, "Nome")} />
          <Input name="descricao" label="Descrição" error={getFieldError(errors, "Descricao")} />
          <Input name="preco" label="Preço" type="number" step="0.01" min="0" required error={getFieldError(errors, "Preco")} />
          <Input name="quantidadeEmEstoque" label="Estoque inicial" type="number" step="1" min="0" required error={getFieldError(errors, "QuantidadeEmEstoque")} />
          <ImagemUpload key={formKey} onArquivo={setImagemArquivo} error={getFieldError(errors, "ImagemUrl")} />
          <Input name="imagemUrl" label="Ou cole a URL da imagem" error={getFieldError(errors, "ImagemUrl")} />
          <Button type="submit" loading={salvando} fullWidth>Publicar produto</Button>
        </form>
      </Modal>
      <Modal open={Boolean(editando)} onClose={() => setEditando(null)} title={`Editar ${editando?.nome ?? ""}`}>
        {editando ? (
          <form onSubmit={handleSalvarEdicao} className="space-y-4">
            <Input name="nome" label="Nome" required defaultValue={editando.nome} error={getFieldError(errosEdicao, "Nome")} />
            <Input name="descricao" label="Descrição" defaultValue={editando.descricao ?? ""} error={getFieldError(errosEdicao, "Descricao")} />
            <Input name="preco" label="Preço" type="number" step="0.01" min="0" required defaultValue={editando.preco} error={getFieldError(errosEdicao, "Preco")} />
            <Input name="quantidadeEmEstoque" label="Estoque" type="number" step="1" min="0" required defaultValue={editando.quantidadeEmEstoque} error={getFieldError(errosEdicao, "QuantidadeEmEstoque")} />
            <ImagemUpload key={editando.id} imagemAtual={editando.imagemUrl} onArquivo={setEditArquivo} error={getFieldError(errosEdicao, "ImagemUrl")} />
            <Input name="imagemUrl" label="Ou cole a URL da imagem" defaultValue={editando.imagemUrl ?? ""} error={getFieldError(errosEdicao, "ImagemUrl")} />
            <Button type="submit" loading={salvandoEdicao} fullWidth>Salvar alterações</Button>
          </form>
        ) : null}
      </Modal>
    </div>
  )
}
