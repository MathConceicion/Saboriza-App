import PropTypes from "prop-types"
import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { useCart } from "../context/CartContext"
import { useAddress } from "../context/AddressContext"
import { useUi } from "../context/UiContext"
import { pedidosService } from "../services/pedidos.service"
import { Button } from "../components/ui/Button"
import { EmptyState } from "../components/ui/EmptyState"
import { BackButton } from "../components/ui/BackButton"
import { Badge } from "../components/ui/Badge"
import { Modal } from "../components/ui/Modal"
import { Icon } from "../components/ui/Icon"
import { QuantityStepper } from "../components/cart/QuantityStepper"
import { formatPreco } from "../lib/formatters"
import { mensagemPorStatus } from "../lib/apiErrors"

const METODOS_PAGAMENTO = [
  { id: "pix", nome: "PIX", descricao: "Aprovação imediata", disponivel: true },
  { id: "credito", nome: "Cartão de crédito", descricao: "Em breve", disponivel: false },
  { id: "debito", nome: "Cartão de débito", descricao: "Em breve", disponivel: false },
  { id: "dinheiro", nome: "Dinheiro", descricao: "Em breve", disponivel: false },
  { id: "vale", nome: "Vale-refeição", descricao: "Em breve", disponivel: false },
]

export default function CheckoutPage() {
  const navigate = useNavigate()
  const { lojaId, itens, total, remover, alterarQuantidade, limpar } = useCart()
  const { endereco, cep } = useAddress()
  const { showToast } = useUi()
  const [criando, setCriando] = useState(false)
  const [gerandoPix, setGerandoPix] = useState(false)
  const [erro, setErro] = useState(null)
  const [pedidoId, setPedidoId] = useState(null)
  const [pix, setPix] = useState(null)
  const [modalAberto, setModalAberto] = useState(false)
  const [metodo, setMetodo] = useState("pix")

  async function handleCriarPedido() {
    setErro(null)
    setCriando(true)
    try {
      const res = await pedidosService.criar({ lojaId, itens: itens.map((i) => ({ produtoId: i.produtoId, quantidade: i.quantidade })) })
      setPedidoId(res.pedidoId ?? res.id)
      setModalAberto(true)
      showToast("Pedido criado", "success")
    } catch (e) {
      const msg = mensagemPorStatus(e, "Não foi possível criar o pedido.")
      setErro(msg)
      showToast(msg, "danger")
    } finally {
      setCriando(false)
    }
  }

  async function handleGerarPix() {
    setErro(null)
    setGerandoPix(true)
    try {
      const res = await pedidosService.simularPix(pedidoId)
      setPix(res)
      showToast("PIX gerado", "success")
    } catch (e) {
      const msg = mensagemPorStatus(e, "Não foi possível gerar o PIX.")
      setErro(msg)
      showToast(msg, "danger")
    } finally {
      setGerandoPix(false)
    }
  }

  function handleConcluir() {
    limpar()
    navigate("/meus-pedidos")
  }

  if (itens.length === 0 && !pedidoId) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-8">
        <EmptyState titulo="Carrinho vazio" descricao="Adicione itens de uma loja para finalizar a compra." actionLabel="Ver marketplace" onAction={() => navigate("/buscar")} />
      </div>
    )
  }

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <BackButton onClick={() => navigate(-1)} />
      <h1 className="text-2xl font-bold mb-2">Finalizar compra</h1>
      <EtapasCompra etapa={pedidoId ? 2 : 1} />
      {erro ? <p className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm mb-4">{erro}</p> : null}
      <div className="bg-white rounded-2xl border border-neutral-200 shadow-card p-5 mb-4">
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-semibold text-lg">Sua sacola</h2>
          <Badge tone="neutral">{itens.length} {itens.length === 1 ? "item" : "itens"}</Badge>
        </div>
        <div className="flex flex-col divide-y divide-neutral-100">
          {itens.map((i) => (
            <div key={i.produtoId} className="flex items-center gap-3 py-3">
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold truncate">{i.nome}</p>
                <p className="text-xs text-neutral-600">{formatPreco(i.preco)} cada</p>
              </div>
              {!pedidoId ? (
                <QuantityStepper quantidade={i.quantidade} onAlterar={(q) => alterarQuantidade(i.produtoId, q)} />
              ) : (
                <span className="text-sm font-semibold text-neutral-600">x{i.quantidade}</span>
              )}
              <span className="text-sm font-bold text-primary w-20 text-right">{formatPreco(i.preco * i.quantidade)}</span>
              {!pedidoId ? (
                <button onClick={() => remover(i.produtoId)} aria-label={`Tirar ${i.nome} do carrinho`} className="h-8 w-8 inline-flex items-center justify-center rounded-full text-neutral-400 hover:text-danger hover:bg-danger/10 transition-colors">
                  <Icon name="x" className="h-4 w-4" />
                </button>
              ) : null}
            </div>
          ))}
        </div>
        <div className="flex items-center justify-between font-bold text-lg mt-2 pt-4 border-t border-neutral-200">
          <span>Total</span>
          <span className="text-primary">{formatPreco(total)}</span>
        </div>
      </div>
      <div className="bg-white rounded-2xl border border-neutral-200 shadow-card p-5 mb-6 flex items-center gap-3">
        <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary font-bold">◎</span>
        <div>
          <p className="text-xs text-neutral-600 uppercase font-semibold tracking-wide">Entrega em</p>
          <p className="text-sm font-semibold">{endereco ?? cep ?? "Endereço não informado"}</p>
        </div>
      </div>
      {!pedidoId ? (
        <Button fullWidth loading={criando} onClick={handleCriarPedido}>Criar pedido • {formatPreco(total)}</Button>
      ) : (
        <Button fullWidth variant="secondary" onClick={() => setModalAberto(true)}>Continuar pagamento</Button>
      )}
      <Modal open={modalAberto && Boolean(pedidoId)} onClose={() => setModalAberto(false)} title="Pedido criado">
        <PedidoCriadoCard
          pedidoId={pedidoId}
          total={total}
          metodo={metodo}
          onSelecionarMetodo={setMetodo}
          gerandoPix={gerandoPix}
          onGerarPix={handleGerarPix}
          pix={pix}
          onConcluir={handleConcluir}
        />
      </Modal>
    </div>
  )
}

function EtapasCompra({ etapa }) {
  const passos = ["Sacola", "Pagamento"]
  return (
    <ol className="flex items-center gap-2 mb-6">
      {passos.map((label, idx) => {
        const numero = idx + 1
        const ativo = numero === etapa
        const concluido = numero < etapa
        return (
          <li key={label} className="flex items-center gap-2">
            <span className={["inline-flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold", concluido || ativo ? "bg-primary text-white" : "bg-neutral-200 text-neutral-600"].join(" ")}>
              {concluido ? "✓" : numero}
            </span>
            <span className={["text-sm font-semibold", ativo || concluido ? "text-neutral-900" : "text-neutral-400"].join(" ")}>{label}</span>
            {numero < passos.length ? <span className="mx-1 h-px w-8 bg-neutral-200" /> : null}
          </li>
        )
      })}
    </ol>
  )
}

EtapasCompra.propTypes = {
  etapa: PropTypes.oneOf([1, 2]).isRequired,
}

function PedidoCriadoCard({ pedidoId, total, metodo, onSelecionarMetodo, gerandoPix, onGerarPix, pix, onConcluir }) {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3 bg-success/10 border border-success rounded-xl p-4">
        <span className="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-success text-white">
          <Icon name="check" className="h-6 w-6" />
        </span>
        <div className="min-w-0">
          <p className="font-bold">Pedido #{String(pedidoId).slice(0, 8).toUpperCase()}</p>
          <div className="mt-1"><Badge tone="info">Aguardando pagamento</Badge></div>
        </div>
        <span className="ml-auto font-bold text-primary whitespace-nowrap">{formatPreco(total)}</span>
      </div>
      <div>
        <p className="text-sm font-semibold mb-2">Como quer pagar?</p>
        <div className="flex flex-col gap-2" role="radiogroup" aria-label="Método de pagamento">
          {METODOS_PAGAMENTO.map((m) => (
            <button
              key={m.id}
              type="button"
              role="radio"
              aria-checked={metodo === m.id}
              disabled={!m.disponivel}
              onClick={() => onSelecionarMetodo(m.id)}
              className={["flex items-center gap-3 rounded-xl border p-3 text-left transition-colors", !m.disponivel ? "border-neutral-200 bg-neutral-100 opacity-70 cursor-not-allowed" : metodo === m.id ? "border-primary bg-primary/5 shadow-sm" : "border-neutral-200 hover:border-primary"].join(" ")}
            >
              <span className={["inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-xs font-bold", m.disponivel ? "bg-primary text-white" : "bg-neutral-200 text-neutral-500"].join(" ")}>
                {m.nome.slice(0, 2).toUpperCase()}
              </span>
              <span className="flex-1 min-w-0">
                <span className="block text-sm font-semibold">{m.nome}</span>
                <span className="block text-xs text-neutral-600">{m.descricao}</span>
              </span>
              {!m.disponivel ? <Badge tone="neutral">Em breve</Badge> : <Badge tone="success">Disponível</Badge>}
            </button>
          ))}
        </div>
      </div>
      {metodo !== "pix" || pix ? null : (
        <Button fullWidth loading={gerandoPix} onClick={onGerarPix}>Gerar PIX fictício</Button>
      )}
      {pix ? <PixResultado pix={pix} onConcluir={onConcluir} /> : null}
    </div>
  )
}

PedidoCriadoCard.propTypes = {
  pedidoId: PropTypes.string.isRequired,
  total: PropTypes.number.isRequired,
  metodo: PropTypes.string.isRequired,
  onSelecionarMetodo: PropTypes.func.isRequired,
  gerandoPix: PropTypes.bool.isRequired,
  onGerarPix: PropTypes.func.isRequired,
  pix: PropTypes.shape({
    pixCopiaECola: PropTypes.string.isRequired,
    valorBruto: PropTypes.number.isRequired,
    taxaComissao: PropTypes.number.isRequired,
    valorLiquido: PropTypes.number.isRequired,
    status: PropTypes.string,
  }),
  onConcluir: PropTypes.func.isRequired,
}

function PixResultado({ pix, onConcluir }) {
  return (
    <div className="flex flex-col gap-3 bg-neutral-100 rounded-xl p-4">
      <div className="flex items-center gap-2">
        <Badge tone="success">{pix.status ?? "PAGO_FICTICIO"}</Badge>
      </div>
      <div className="bg-white border border-neutral-200 rounded-lg p-3 break-all text-xs font-mono">{pix.pixCopiaECola}</div>
      <div className="text-sm flex flex-col gap-1">
        <span>Bruto: <strong>{formatPreco(pix.valorBruto)}</strong></span>
        <span>Comissão: <strong>{formatPreco(pix.taxaComissao)}</strong></span>
        <span>Repasse vendedor: <strong>{formatPreco(pix.valorLiquido)}</strong></span>
      </div>
      <Button fullWidth onClick={onConcluir}>Concluir</Button>
    </div>
  )
}

PixResultado.propTypes = {
  pix: PropTypes.shape({
    pixCopiaECola: PropTypes.string.isRequired,
    valorBruto: PropTypes.number.isRequired,
    taxaComissao: PropTypes.number.isRequired,
    valorLiquido: PropTypes.number.isRequired,
    status: PropTypes.string,
  }).isRequired,
  onConcluir: PropTypes.func.isRequired,
}
