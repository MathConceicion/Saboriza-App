import { apiClient } from "../lib/apiClient"

export const notificacoesService = {
  listar({ apenasNaoLidas, pagina = 1, tamanho = 20 } = {}) {
    return apiClient.get("/api/admin/notificacoes", { params: { apenasNaoLidas, pagina, tamanho } }).then((r) => r.data)
  },
  contarNaoLidas() {
    return apiClient.get("/api/admin/notificacoes/contar-nao-lidas").then((r) => r.data)
  },
  marcarLida(notificacaoId) {
    return apiClient.patch(`/api/admin/notificacoes/${notificacaoId}/ler`).then((r) => r.data)
  },
}
