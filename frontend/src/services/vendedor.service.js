import { apiClient } from "../lib/apiClient"

export const vendedorService = {
  solicitarUpgrade({ nomeLoja, categoriaLojaId, enderecoComercial, cep, telefone, cnpj, fotoUrl, horarioAbertura, horarioFechamento }) {
    return apiClient.post("/api/vendedor/solicitar-upgrade", { nomeLoja, categoriaLojaId, enderecoComercial, cep, telefone, cnpj, fotoUrl, horarioAbertura, horarioFechamento }).then((r) => r.data)
  },
  obterLoja() {
    return apiClient.get("/api/vendedor/loja").then((r) => r.data)
  },
  atualizarLoja({ nome, descricao, fotoUrl, horarioAbertura, horarioFechamento }) {
    return apiClient.put("/api/vendedor/loja", { nome, descricao, fotoUrl, horarioAbertura, horarioFechamento }).then((r) => r.data)
  },
}
