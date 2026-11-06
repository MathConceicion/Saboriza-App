import { createContext, useContext, useState, useEffect } from "react"
import PropTypes from "prop-types"

const AddressContext = createContext(null)

export function AddressProvider({ children }) {
  const [address, setAddress] = useState(() => {
    const raw = localStorage.getItem("saboriza:address")
    return raw ? JSON.parse(raw) : { cep: null, endereco: null, latitude: null, longitude: null }
  })

  useEffect(() => {
    localStorage.setItem("saboriza:address", JSON.stringify(address))
  }, [address])

  function definirEndereco(cep, endereco, latitude, longitude) {
    setAddress({ cep, endereco, latitude, longitude })
  }

  function limpar() {
    setAddress({ cep: null, endereco: null, latitude: null, longitude: null })
  }

  return <AddressContext.Provider value={{ ...address, definirEndereco, limpar }}>{children}</AddressContext.Provider>
}

AddressProvider.propTypes = {
  children: PropTypes.node.isRequired,
}

export function useAddress() {
  const ctx = useContext(AddressContext)
  if (!ctx) throw new Error("useAddress deve estar dentro de AddressProvider")
  return ctx
}
