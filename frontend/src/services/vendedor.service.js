import { apiClient } from "../lib/apiClient"

export const vendedorService = {
  solicitarUpgrade({ nomeLoja, categoriaLojaId, enderecoComercial, cep, telefone, cnpj, fotoUrl, horarioAbertura, horarioFechamento, diasFuncionamento, aberta }) {
    return apiClient.post("/api/vendedor/solicitar-upgrade", { nomeLoja, categoriaLojaId, enderecoComercial, cep, telefone, cnpj, fotoUrl, horarioAbertura, horarioFechamento, diasFuncionamentoJson: diasFuncionamento ? JSON.stringify(diasFuncionamento) : null, aberta }).then((r) => r.data)
  },
  obterLoja() {
    return apiClient.get("/api/vendedor/loja").then((r) => r.data)
  },
  atualizarLoja({ nome, descricao, fotoUrl, cnpj, telefone, enderecoComercial, cep, diasFuncionamento, aberta, horarioAbertura, horarioFechamento }) {
    return apiClient.put("/api/vendedor/loja", { nome, descricao, fotoUrl, cnpj, telefone, enderecoComercial, cep, diasFuncionamentoJson: diasFuncionamento ? JSON.stringify(diasFuncionamento) : null, aberta, horarioAbertura, horarioFechamento }).then((r) => r.data)
  },
}
