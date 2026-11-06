import { apiClient } from "../lib/apiClient"

export const lojasService = {
  listarProximas({ lat, lng, raioKm = 5, categoriaId, pagina = 1 }) {
    return apiClient.get("/api/lojas/proximas", { params: { lat, lng, raioKm, categoriaId, pagina } }).then((r) => r.data)
  },
  listarPorCategoria(slug, { lat, lng } = {}) {
    return apiClient.get(`/api/categorias/${slug}/lojas`, { params: { lat, lng } }).then((r) => r.data)
  },
  obterProdutos(lojaId, { departamento } = {}) {
    return apiClient.get(`/api/lojas/${lojaId}/produtos`, { params: { departamento } }).then((r) => r.data)
  },
  obter(lojaId) {
    return apiClient.get(`/api/lojas/${lojaId}`).then((r) => r.data)
  },
  buscar(termo, { signal } = {}) {
    return apiClient.get("/api/lojas/buscar", { params: { termo: termo ?? "", limite: 50 }, signal }).then((r) => r.data)
  },
}
