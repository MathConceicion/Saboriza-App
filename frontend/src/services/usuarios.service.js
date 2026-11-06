import { apiClient } from "../lib/apiClient"

export const usuariosService = {
  anonimizar(id) {
    return apiClient.delete(`/api/usuarios/${id}/anonimizar`).then((r) => r.data)
  },
  exportarDados(id) {
    return apiClient.get(`/api/usuarios/${id}/exportar-dados`, { responseType: "blob" }).then((r) => r.data)
  },
  revogarConsentimento(tipo, motivo) {
    return apiClient.post(`/api/usuarios/me/consentimentos/${tipo}/revogar`, { motivo }).then((r) => r.data)
  },
  enviarImagem(arquivo) {
    const form = new FormData()
    form.append("arquivo", arquivo)
    return fetch(`${import.meta.env.VITE_API_URL}/api/uploads/imagens`, { method: "POST", body: form, credentials: "include" }).then(async (res) => {
      const data = await res.json().catch(() => null)
      if (!res.ok) {
        const err = new Error(data?.error ?? "Falha no envio da imagem")
        err.response = { status: res.status, data }
        throw err
      }
      return data
    })
  },
}
