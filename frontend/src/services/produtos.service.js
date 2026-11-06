import { apiClient } from "../lib/apiClient"

export const produtosService = {
  criar({ nome, descricao, preco, quantidadeEmEstoque, imagemUrl }) {
    return apiClient.post("/api/produtos", { nome, descricao, preco, quantidadeEmEstoque, imagemUrl }).then((r) => r.data)
  },
  atualizar(produtoId, { nome, descricao, preco, quantidadeEmEstoque, imagemUrl }) {
    return apiClient.put(`/api/produtos/${produtoId}`, { nome, descricao, preco, quantidadeEmEstoque, imagemUrl }).then((r) => r.data)
  },
  alternar(produtoId, ativo) {
    const acao = ativo ? "ativar" : "desativar"
    return apiClient.patch(`/api/produtos/${produtoId}/${acao}`, {}).then((r) => r.data)
  },
  excluir(produtoId) {
    return apiClient.delete(`/api/produtos/${produtoId}`).then((r) => r.data)
  },
  enviarImagem(arquivo) {
    const form = new FormData()
    form.append("arquivo", arquivo)
    return fetch(`${import.meta.env.VITE_API_URL}/api/produtos/imagens`, { method: "POST", body: form, credentials: "include" }).then(async (res) => {
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
