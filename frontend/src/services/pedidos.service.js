import { apiClient } from "../lib/apiClient"

export const pedidosService = {
  criar({ lojaId, itens }) {
    return apiClient.post("/api/pedidos", { lojaId, itens }).then((r) => r.data)
  },
  listarPendentes() {
    return apiClient.get("/api/vendedor/pedidos-pendentes").then((r) => r.data)
  },
  listarMeus() {
    return apiClient.get("/api/pedidos/meus").then((r) => r.data)
  },
  aprovar(pedidoId) {
    return apiClient.post(`/api/pedidos/${pedidoId}/aprovar`).then((r) => r.data)
  },
  simularPix(pedidoId) {
    return apiClient.post(`/api/pedidos/${pedidoId}/simular-pix`).then((r) => r.data)
  },
}
