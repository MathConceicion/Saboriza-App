const cepsDemo = {
  "01310-100": { endereco: "Av. Paulista, 1000 - Sao Paulo/SP", latitude: -23.561414, longitude: -46.656013 },
  "01311-000": { endereco: "Rua Augusta, 2000 - Sao Paulo/SP", latitude: -23.55658, longitude: -46.662321 },
  "04567-000": { endereco: "Vila Mariana - Sao Paulo/SP", latitude: -23.58805, longitude: -46.63207 },
  "05001-000": { endereco: "Perdizes - Sao Paulo/SP", latitude: -23.53405, longitude: -46.68207 },
  "05410-002": { endereco: "Pinheiros - Sao Paulo/SP", latitude: -23.56712, longitude: -46.6875 },
}

export function normalizarCep(cep) {
  const digitos = (cep ?? "").replace(/\D/g, "")
  if (digitos.length === 8) return digitos.slice(0, 5) + "-" + digitos.slice(5)
  return (cep ?? "").trim()
}

export function buscarCepDemo(cep) {
  const chave = normalizarCep(cep)
  const achado = cepsDemo[chave]
  return achado ? { cep: chave, endereco: achado.endereco, latitude: achado.latitude, longitude: achado.longitude } : null
}

export function listarCepsDemo() {
  return Object.keys(cepsDemo)
}
