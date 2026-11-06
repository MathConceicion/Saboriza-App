import { apiClient } from "../lib/apiClient"

export const adminService = {
  listarVendedores(status) {
    return apiClient.get("/api/admin/vendedores", { params: { status } }).then((r) => r.data)
  },
  listarProdutos(aprovado) {
    return apiClient.get("/api/admin/produtos", { params: { aprovado } }).then((r) => r.data)
  },  aprovarVendedor(vendedorId) {
    return apiClient.patch(`/api/admin/vendedores/${vendedorId}/aprovar`, {}).then((r) => r.data)
  },
  aprovarProduto(produtoId) {
    return apiClient.patch(`/api/admin/produtos/${produtoId}/aprovar`, {}).then((r) => r.data)
  },
  atualizarTaxa(novaTaxaPercentual) {
    return apiClient.patch("/api/admin/configuracoes/taxa-comissao", { novaTaxaPercentual }).then((r) => r.data)
  },
  listarAuditLogs({ entidade, entidadeId, usuarioId } = {}) {
    return apiClient.get("/api/admin/audit-logs", { params: { entidade, entidadeId, usuarioId } }).then((r) => r.data)
  },
  faturamento({ de, ate }) {
    return apiClient.get("/api/admin/relatorios/faturamento", { params: { de, ate } }).then((r) => r.data)
  },
}
