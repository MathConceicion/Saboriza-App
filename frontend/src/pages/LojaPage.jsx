import { useEffect, useState } from "react"
import { useParams, useNavigate } from "react-router-dom"
import { lojasService } from "../services/lojas.service"
import { useCart } from "../context/CartContext"
import { useUi } from "../context/UiContext"
import { ProductCard } from "../components/product/ProductCard"
import { CartSidebar } from "../components/cart/CartSidebar"
import { Modal } from "../components/ui/Modal"
import { Sheet } from "../components/ui/Sheet"
import { Skeleton } from "../components/ui/Skeleton"
import { EmptyState } from "../components/ui/EmptyState"
import { Button } from "../components/ui/Button"
import { BackButton } from "../components/ui/BackButton"
import { imagemFallback } from "../lib/imagens"
import { formatPreco } from "../lib/formatters"
import { mensagemPorStatus } from "../lib/apiErrors"

export default function LojaPage() {
  const { lojaId } = useParams()
  const navigate = useNavigate()
  const [produtos, setProdutos] = useState([])
  const [loja, setLoja] = useState(null)
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState(null)
  const { adicionar, limpar, podeAdicionarDeOutraLoja, itens, total } = useCart()
  const { showToast } = useUi()
  const [conflito, setConflito] = useState(null)
  const [carrinhoAberto, setCarrinhoAberto] = useState(false)

  function carregar() {
    setLoading(true)
    setErro(null)
    Promise.all([
      lojasService.obterProdutos(lojaId).then((r) => r.items ?? r ?? []),
      lojasService.obter(lojaId).catch(() => null),
    ])
      .then(([prods, info]) => {
        setProdutos(prods)
        setLoja(info)
      })
      .catch((e) => {
        setProdutos([])
        setErro(mensagemPorStatus(e, "Erro ao carregar produtos"))
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    carregar()
  }, [lojaId])

  function adicionarProduto(produto) {
    if ((produto.quantidadeEmEstoque ?? 1) <= 0) {
      showToast("Produto esgotado", "warning")
      return
    }
    adicionar({ produtoId: produto.id, nome: produto.nome, preco: produto.preco, quantidade: 1, lojaId })
    showToast(`${produto.nome} adicionado`, "success")
  }

  function handleAdd(produtoId) {
    const produto = produtos.find((p) => p.id === produtoId)
    if (!produto) return
    if (!podeAdicionarDeOutraLoja(lojaId)) {
      setConflito(produto)
      return
    }
    adicionarProduto(produto)
  }

  function confirmarTrocaDeLoja() {
    if (!conflito) return
    limpar()
    adicionar({ produtoId: conflito.id, nome: conflito.nome, preco: conflito.preco, quantidade: 1, lojaId })
    setConflito(null)
    showToast("Carrinho trocado para esta loja", "success")
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <BackButton onClick={() => navigate("/buscar")} />
      <div className="flex items-center gap-2 flex-wrap mb-2">
        <h1 className="text-2xl font-bold">{loja?.nome ?? "Cardápio"}</h1>
        {loja?.abertoAgora === true ? <span className="bg-success/10 text-success text-xs font-bold rounded-full px-2 py-0.5">Aberto agora</span> : null}
        {loja?.abertoAgora === false ? <span className="bg-neutral-200 text-neutral-600 text-xs font-bold rounded-full px-2 py-0.5">Fechado</span> : null}
      </div>
      <p className="text-sm text-neutral-600 mb-6">
        {loja ? `${loja.categoria} • ${loja.endereco}` : `Loja ${String(lojaId).slice(0, 8)}`}
        {loja?.horarioAbertura && loja?.horarioFechamento ? ` • ${loja.horarioAbertura} às ${loja.horarioFechamento}` : null}
      </p>
      {loading ? <Skeleton variant="card" /> : null}
      {!loading && erro ? (
        <div className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4 flex items-center justify-between">
          <span>{erro}</span>
          <Button size="sm" variant="ghost" onClick={carregar}>Tentar novamente</Button>
        </div>
      ) : null}
      {!loading && !erro && produtos.length === 0 ? <EmptyState titulo="Sem produtos por enquanto" descricao="Esta loja ainda não publicou itens no cardápio." /> : null}
      {!loading && !erro && produtos.length > 0 ? (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 grid grid-cols-1 sm:grid-cols-2 gap-4">
            {produtos.map((p) => (
              <ProductCard key={p.id} id={p.id} nome={p.nome} descricao={p.descricao} preco={p.preco} imagemUrl={p.imagemUrl ?? imagemFallback} onAdd={handleAdd} />
            ))}
          </div>
          <div className="hidden lg:block">
            <div className="sticky top-20">
              <CartSidebar onFinalizar={() => navigate("/checkout")} />
            </div>
          </div>
        </div>
      ) : null}
      {itens.length > 0 ? (
        <div className="lg:hidden fixed bottom-4 left-4 right-4 z-30">
          <Button fullWidth onClick={() => setCarrinhoAberto(true)}>Ver carrinho • {formatPreco(total)}</Button>
        </div>
      ) : null}
      <Sheet open={carrinhoAberto} onClose={() => setCarrinhoAberto(false)} side="bottom" title="Meu carrinho">
        <CartSidebar onFinalizar={() => { setCarrinhoAberto(false); navigate("/checkout") }} />
      </Sheet>
      <Modal open={Boolean(conflito)} onClose={() => setConflito(null)} title="Trocar de loja?">
        <p className="text-sm text-neutral-600">Seu carrinho tem itens de outra loja. Confirmar esvazia o carrinho e adiciona {conflito?.nome}.</p>
        <div className="flex gap-3 mt-4">
          <Button variant="ghost" fullWidth onClick={() => setConflito(null)}>Cancelar</Button>
          <Button fullWidth onClick={confirmarTrocaDeLoja}>Trocar</Button>
        </div>
      </Modal>
    </div>
  )
}
