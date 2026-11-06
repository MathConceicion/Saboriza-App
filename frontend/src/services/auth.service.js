import { apiClient } from "../lib/apiClient"

export const authService = {
  login({ email, senha }) {
    return apiClient.post("/api/auth/login", { email, senha }).then((r) => r.data)
  },
  registrar({ nome, email, senha, cpf }) {
    return apiClient.post("/api/auth/registro", { nome, email, senha, cpf }).then((r) => r.data)
  },
  me() {
    return apiClient.get("/api/auth/me").then((r) => r.data)
  },
  refresh() {
    return apiClient.post("/api/auth/refresh", {}).then((r) => r.data)
  },
  logout() {
    return apiClient.post("/api/auth/logout").then((r) => r.data)
  },
}
