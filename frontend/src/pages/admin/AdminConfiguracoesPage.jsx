import { useState } from "react"
import PropTypes from "prop-types"
import { useUi } from "../../context/UiContext"
import { adminService } from "../../services/admin.service"
import { Button } from "../../components/ui/Button"
import { Input } from "../../components/ui/Input"
import { mensagemPorStatus } from "../../lib/apiErrors"

export default function AdminConfiguracoesPage() {
  const { showToast } = useUi()
  const [taxa, setTaxa] = useState("8.5")
  const [loading, setLoading] = useState(false)

  async function handleSalvar(e) {
    e.preventDefault()
    const n = Number(String(taxa).replace(",", "."))
    if (!Number.isFinite(n) || n < 0 || n > 30) {
      showToast("Taxa deve estar entre 0 e 30%.", "danger")
      return
    }
    setLoading(true)
    try {
      await adminService.atualizarTaxa(n)
      showToast("Taxa de comissão atualizada com sucesso.", "success")
    } catch (err) {
      showToast(mensagemPorStatus(err, "Não foi possível atualizar a taxa."), "danger")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-2xl">
      <div className="mb-6">
        <h1 className="font-display text-2xl font-bold text-neutral-900">Configurações da Plataforma</h1>
        <p className="text-neutral-600">Configure as regras gerais do marketplace Saboriza.</p>
      </div>

      <div className="bg-white rounded-xl border border-neutral-200 p-6">
        <h2 className="text-lg font-semibold mb-4">Taxa de Comissão</h2>
        <p className="text-sm text-neutral-600 mb-4">
          Percentual cobrado sobre o valor bruto de cada pedido finalizado. O restante é repassado ao vendedor.
        </p>
        <form onSubmit={handleSalvar} className="flex flex-col gap-4 max-w-xs">
          <Input
            label="Taxa de Comissão (%)"
            type="number"
            step="0.01"
            min="0"
            max="30"
            value={taxa}
            onChange={(e) => setTaxa(e.target.value)}
            required
          />
          <p className="text-xs text-neutral-500">
            Exemplo: 8.5% significa que a plataforma retém 8,5% do valor de cada venda.
          </p>
          <Button type="submit" loading={loading} variant="primary">
            Salvar Taxa
          </Button>
        </form>
      </div>

      <div className="mt-6 bg-neutral-50 rounded-xl border border-neutral-200 p-6">
        <h3 className="text-sm font-semibold text-neutral-900 mb-3">Informações da Plataforma</h3>
        <div className="space-y-2 text-sm text-neutral-600">
          <div className="flex justify-between">
            <span>Nome da Plataforma:</span>
            <span className="font-medium">Saboriza Marketplace</span>
          </div>
          <div className="flex justify-between">
            <span>Versão:</span>
            <span className="font-medium">1.0.0</span>
          </div>
          <div className="flex justify-between">
            <span>Ambiente:</span>
            <span className="font-medium">Produção</span>
          </div>
        </div>
      </div>
    </div>
  )
}

AdminConfiguracoesPage.propTypes = {}
