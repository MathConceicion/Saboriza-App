import { apiClient } from "../lib/apiClient"

export const solicitacoesLojasService = {
  listar({ status, busca, pagina = 1, tamanho = 20 } = {}) {
    return apiClient.get("/api/admin/solicitacoes-lojas", { params: { status, busca, pagina, tamanho } }).then((r) => r.data)
  },
  aprovar(vendedorId) {
    return apiClient.patch(`/api/admin/solicitacoes-lojas/${vendedorId}/aprovar`).then((r) => r.data)
  },
  recusar(vendedorId, motivo) {
    return apiClient.patch(`/api/admin/solicitacoes-lojas/${vendedorId}/recusar`, { motivo }).then((r) => r.data)
  },
  definirEmAnalise(vendedorId) {
    return apiClient.patch(`/api/admin/solicitacoes-lojas/${vendedorId}/analise`).then((r) => r.data)
  },
}
