import axios from "axios"

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 15000,
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
})

let isRefreshing = false
let failedQueue = []

function processQueue(error) {
  failedQueue.forEach(({ resolve, reject }) => (error ? reject(error) : resolve()))
  failedQueue = []
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const url = original?.url ?? ""
    const ehTentativaAuth = url.includes("/api/auth/login") || url.includes("/api/auth/registro") || url.includes("/api/auth/refresh")
    const ehVerificacaoSessao = url.includes("/api/auth/me")
    if (error.response?.status === 401 && !original._retry && !ehTentativaAuth) {
      if (isRefreshing) return new Promise((resolve, reject) => failedQueue.push({ resolve, reject }))
      original._retry = true
      isRefreshing = true
      try {
        await axios.post(`${import.meta.env.VITE_API_URL}/api/auth/refresh`, {}, { withCredentials: true })
        processQueue(null)
        return apiClient(original)
      } catch (e) {
        processQueue(e)
        const emPaginaAuth = window.location.pathname === "/login" || window.location.pathname === "/registro"
        if (!emPaginaAuth && !ehVerificacaoSessao) window.location.href = "/login"
        return Promise.reject(e)
      } finally {
        isRefreshing = false
      }
    }
    return Promise.reject(error)
  }
)
