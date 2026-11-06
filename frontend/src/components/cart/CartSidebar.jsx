import PropTypes from "prop-types"
import { useCart } from "../../context/CartContext"
import { formatPreco } from "../../lib/formatters"
import { Button } from "../ui/Button"
import { Badge } from "../ui/Badge"
import { Icon } from "../ui/Icon"
import { QuantityStepper } from "./QuantityStepper"

export function CartSidebar({ onFinalizar }) {
  const { itens, total, remover, alterarQuantidade, limpar } = useCart()
  if (itens.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-dashed border-neutral-200 shadow-card p-6 flex flex-col items-center gap-2 text-center">
        <span className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-neutral-100 text-neutral-400">
          <Icon name="cart" className="h-6 w-6" />
        </span>
        <h3 className="font-semibold">Meu Carrinho</h3>
        <p className="text-sm text-neutral-600">Sua sacola está vazia por enquanto.</p>
      </div>
    )
  }
  return (
    <div className="bg-white rounded-2xl border border-neutral-200 shadow-card p-5 flex flex-col gap-2">
      <div className="flex items-center gap-2 pb-2 border-b border-neutral-100">
        <span className="inline-flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary">
          <Icon name="cart" className="h-5 w-5" />
        </span>
        <h3 className="font-semibold">Meu Carrinho</h3>
        <Badge tone="neutral">{itens.length}</Badge>
        <button onClick={limpar} className="ml-auto text-xs text-neutral-500 hover:text-danger font-semibold transition-colors">Limpar tudo</button>
      </div>
      <div className="flex flex-col divide-y divide-neutral-100">
        {itens.map((item) => (
          <div key={item.produtoId} className="flex items-center gap-2 py-3">
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold truncate">{item.nome}</p>
              <p className="text-xs text-neutral-600">{formatPreco(item.preco)} cada</p>
              <p className="text-sm text-primary font-bold mt-0.5">{formatPreco(item.preco * item.quantidade)}</p>
            </div>
            <QuantityStepper quantidade={item.quantidade} onAlterar={(q) => alterarQuantidade(item.produtoId, q)} />
            <button onClick={() => remover(item.produtoId)} aria-label={`Tirar ${item.nome} do carrinho`} className="h-8 w-8 shrink-0 inline-flex items-center justify-center rounded-full text-neutral-400 hover:text-danger hover:bg-danger/10 transition-colors">
              <Icon name="x" className="h-4 w-4" />
            </button>
          </div>
        ))}
      </div>
      <div className="flex items-center justify-between font-bold text-lg pt-3 border-t border-neutral-200">
        <span>Total</span>
        <span className="text-primary">{formatPreco(total)}</span>
      </div>
      <Button fullWidth onClick={onFinalizar} disabled={itens.length === 0}>Finalizar compra</Button>
    </div>
  )
}

CartSidebar.propTypes = {
  onFinalizar: PropTypes.func.isRequired,
}
