import { apiClient } from "../lib/apiClient"

export const usuariosAdminService = {
  listar({ busca, papel, bloqueado, pagina = 1, tamanho = 20 } = {}) {
    return apiClient.get("/api/admin/usuarios", { params: { busca, papel, bloqueado, pagina, tamanho } }).then((r) => r.data)
  },
  bloquear(usuarioId, motivo) {
    return apiClient.patch(`/api/admin/usuarios/${usuarioId}/bloquear`, { motivo }).then((r) => r.data)
  },
  desbloquear(usuarioId) {
    return apiClient.patch(`/api/admin/usuarios/${usuarioId}/desbloquear`).then((r) => r.data)
  },
}
