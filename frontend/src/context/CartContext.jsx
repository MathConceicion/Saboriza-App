import { createContext, useContext, useState, useEffect, useCallback, useMemo } from "react"
import PropTypes from "prop-types"

const CartContext = createContext(null)

export function CartProvider({ children }) {
  const [lojaId, setLojaId] = useState(() => localStorage.getItem("saboriza:cartLojaId"))
  const [itens, setItens] = useState(() => {
    const raw = localStorage.getItem("saboriza:cartItens")
    return raw ? JSON.parse(raw) : []
  })

  useEffect(() => {
    localStorage.setItem("saboriza:cartItens", JSON.stringify(itens))
  }, [itens])

  useEffect(() => {
    if (lojaId) localStorage.setItem("saboriza:cartLojaId", lojaId)
    else localStorage.removeItem("saboriza:cartLojaId")
  }, [lojaId])

  const adicionar = useCallback((item) => {
    setItens((prev) => {
      const lojaAtual = localStorage.getItem("saboriza:cartLojaId")
      if (lojaAtual && lojaAtual !== item.lojaId) return prev
      const existe = prev.find((i) => i.produtoId === item.produtoId)
      if (existe) return prev.map((i) => (i.produtoId === item.produtoId ? { ...i, quantidade: i.quantidade + item.quantidade } : i))
      setLojaId(item.lojaId)
      return [...prev, item]
    })
  }, [])

  const remover = useCallback((produtoId) => {
    setItens((prev) => {
      const filtrados = prev.filter((i) => i.produtoId !== produtoId)
      if (filtrados.length === 0) setLojaId(null)
      return filtrados
    })
  }, [])

  const alterarQuantidade = useCallback((produtoId, quantidade) => {
    setItens((prev) => (quantidade <= 0 ? prev.filter((i) => i.produtoId !== produtoId) : prev.map((i) => (i.produtoId === produtoId ? { ...i, quantidade } : i))))
  }, [])

  const limpar = useCallback(() => {
    setItens([])
    setLojaId(null)
  }, [])

  const total = useMemo(() => itens.reduce((acc, i) => acc + i.preco * i.quantidade, 0), [itens])

  const podeAdicionarDeOutraLoja = useCallback((novaLojaId) => !lojaId || lojaId === novaLojaId, [lojaId])

  return (
    <CartContext.Provider value={{ lojaId, itens, adicionar, remover, alterarQuantidade, limpar, total, podeAdicionarDeOutraLoja }}>
      {children}
    </CartContext.Provider>
  )
}

CartProvider.propTypes = {
  children: PropTypes.node.isRequired,
}

export function useCart() {
  const ctx = useContext(CartContext)
  if (!ctx) throw new Error("useCart deve estar dentro de CartProvider")
  return ctx
}
