import { apiClient } from "../lib/apiClient"

export const adminDashboardService = {
  estatisticas() {
    return apiClient.get("/api/admin/dashboard/estatisticas").then((r) => r.data)
  },
  crescimento(meses = 6) {
    return apiClient.get("/api/admin/dashboard/crescimento", { params: { meses } }).then((r) => r.data)
  },
}
