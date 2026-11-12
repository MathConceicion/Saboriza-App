export function validarCep(cep) {
  const digits = cep.replace(/\D/g, "")
  return digits.length === 8
}

export function validarPreco(preco) {
  const n = Number(preco)
  return Number.isFinite(n) && n > 0
}

export function validarNome(nome) {
  return typeof nome === "string" && nome.trim().length >= 3 && nome.trim().length <= 150
}

export function validarHora(hora) {
  if (typeof hora !== "string") return false
  const m = hora.match(/^(\d{2}):(\d{2})$/)
  if (!m) return false
  const h = Number(m[1])
  const min = Number(m[2])
  return h >= 0 && h <= 23 && min >= 0 && min <= 59
}

export function validarDiasFuncionamento(dias) {
  return Array.isArray(dias) && dias.length > 0
}

export function parseValidationErrors(error) {
  const data = error?.response?.data
  if (data?.errors && typeof data.errors === "object") return data.errors
  return null
}

export function getFieldError(errors, field) {
  return errors?.[field]?.[0]
}
