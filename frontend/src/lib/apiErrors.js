export function extrairMensagemErro(error, fallback = "Erro inesperado. Tente novamente.") {
  const direto = error?.response?.data?.error
  if (typeof direto === "string" && direto.trim().length > 0) return direto
  const titulo = error?.response?.data?.title
  if (typeof titulo === "string" && titulo.trim().length > 0) return titulo
  if (typeof error?.message === "string" && error.message.trim().length > 0) return error.message
  return fallback
}

export function statusHttp(error) {
  return error?.response?.status ?? null
}

export function ehErroStatus(error, status) {
  return statusHttp(error) === status
}

export function mensagemPorStatus(error) {
  const status = statusHttp(error)
  if (status === 401) return "Sessão expirada. Entre novamente."
  if (status === 403) return "Você não tem permissão para esta ação."
  if (status === 404) return "Registro não encontrado."
  if (status === 409) return "Conflito de concorrência. Recarregue e tente de novo."
  if (status === 429) return "Muitas tentativas. Aguarde um minuto."
  return extrairMensagemErro(error)
}
