import { apiClient } from "../lib/apiClient"

export const categoriasService = {
  listar() {
    return apiClient.get("/api/categorias").then((r) => r.data)
  },
  listarLojas(slug, { lat, lng, raioKm = 50, pagina = 1, tamanho = 20 } = {}) {
    return apiClient.get(`/api/categorias/${slug}/lojas`, { params: { lat, lng, raioKm, pagina, tamanho } }).then((r) => r.data)
  },
}
