import { apiClient } from "../lib/apiClient"

export const produtosAdminService = {
  listar({ busca, lojaId, ativo, aprovado, pagina = 1, tamanho = 20 } = {}) {
    return apiClient.get("/api/admin/produtos-admin", { params: { busca, lojaId, ativo, aprovado, pagina, tamanho } }).then((r) => r.data)
  },
  desativar(produtoId) {
    return apiClient.patch(`/api/admin/produtos-admin/${produtoId}/desativar`).then((r) => r.data)
  },
  ativar(produtoId) {
    return apiClient.patch(`/api/admin/produtos-admin/${produtoId}/ativar`).then((r) => r.data)
  },
}
