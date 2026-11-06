export function formatDistancia(km) {
  return `${km.toFixed(1).replace(".", ",")} km`
}

export function formatPreco(valor) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(valor)
}

export function formatDataHora(iso) {
  return new Date(iso).toLocaleString("pt-BR")
}
