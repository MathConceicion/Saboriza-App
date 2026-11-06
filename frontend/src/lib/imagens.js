export const imagemFallback =
  "data:image/svg+xml;utf8," +
  encodeURIComponent(
    "<svg width='600' height='400'><rect width='600' height='400' fill='#F8BBD0'/><text x='300' y='205' font-size='30' text-anchor='middle' fill='#5A1A1F'>Saboriza</text></svg>"
  )

export function imagemLoja(lojaId, categoria) {
  let soma = 0
  const chave = String(lojaId ?? "")
  for (let i = 0; i < chave.length; i++) soma += chave.charCodeAt(i)
  return "https://loremflickr.com/600/400/" + temaPorCategoria(categoria) + "?lock=" + (soma % 50 + 1)
}

export function reservaLoja(lojaId) {
  return "https://picsum.photos/seed/" + lojaId + "/600/400"
}

export function reservaProduto(produtoId) {
  return "https://picsum.photos/seed/" + produtoId + "/600/400"
}

export function temaPorCategoria(nomeOuSlug) {
  const texto = (nomeOuSlug ?? "").toLowerCase()
  if (texto.includes("pizza")) return "pizza"
  if (texto.includes("hamb")) return "burger"
  if (texto.includes("jap")) return "sushi"
  if (texto.includes("sorv")) return "icecream"
  if (texto.includes("doc")) return "doughnut"
  if (texto.includes("pad")) return "bakery"
  if (texto.includes("brasil")) return "steak"
  if (texto.includes("saud") || texto.includes("veg")) return "salad"
  return "food"
}

export function tratarErroImagem(event) {
  const alvo = event.currentTarget
  alvo.onerror = null
  alvo.src = imagemFallback
}

export function tratarErroImagemComFallback(event) {
  const alvo = event.currentTarget
  const reserva = alvo.dataset.fallback
  if (reserva && alvo.src !== reserva) {
    alvo.src = reserva
    return
  }
  alvo.onerror = null
  alvo.src = imagemFallback
}

const iconesCategoria = {
  pizzarias: "https://images.unsplash.com/photo-1513104890138-7c749659a591?q=80&w=200&auto=format&fit=crop",
  hamburgueres: "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?q=80&w=200&auto=format&fit=crop",
  "comida-japonesa": "https://images.unsplash.com/photo-1579871494447-9811cf80d66c?q=80&w=200&auto=format&fit=crop",
  sorveterias: "https://images.unsplash.com/photo-1563805042-7684c019e1cb?q=80&w=200&auto=format&fit=crop",
  docerias: "https://images.unsplash.com/photo-1551024601-bec78aea704b?q=80&w=200&auto=format&fit=crop",
  "padarias-artesanais": "https://images.unsplash.com/photo-1509440159596-0249088772ff?q=80&w=200&auto=format&fit=crop",
  "comida-brasileira": "https://images.unsplash.com/photo-1558030006-450675393462?q=80&w=200&auto=format&fit=crop",
  "saudavel-vegana": "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?q=80&w=200&auto=format&fit=crop",
}

export function iconeCategoria(slug) {
  return iconesCategoria[slug] ?? imagemFallback
}

export function reservaCategoria(slug) {
  return "https://picsum.photos/seed/categoria-" + slug + "/200/200"
}
