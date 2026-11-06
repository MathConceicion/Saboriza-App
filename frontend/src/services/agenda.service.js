import { apiClient } from "../lib/apiClient"

export const agendaService = {
  listarDoDia({ data, status } = {}) {
    return apiClient.get("/api/vendedor/agenda", { params: { data, status } }).then((r) => r.data)
  },
  avancar(agendaId, novoStatus) {
    return apiClient.patch(`/api/vendedor/agenda/${agendaId}/avancar`, { novoStatus }).then((r) => r.data)
  },
}
