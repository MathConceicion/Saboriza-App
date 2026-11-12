import { apiClient } from "../lib/apiClient"

export const lojasAdminService = {
  listar({ busca, ativo, aprovado, pagina = 1, tamanho = 20 } = {}) {
    return apiClient.get("/api/admin/lojas", { params: { busca, ativo, aprovado, pagina, tamanho } }).then((r) => r.data)
  },
  suspender(lojaId) {
    return apiClient.patch(`/api/admin/lojas/${lojaId}/suspender`).then((r) => r.data)
  },
  reativar(lojaId) {
    return apiClient.patch(`/api/admin/lojas/${lojaId}/reativar`).then((r) => r.data)
  },
}
