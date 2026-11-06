import { createContext, useContext, useState, useEffect, useCallback } from "react"
import PropTypes from "prop-types"
import { apiClient } from "../lib/apiClient"

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    apiClient.get("/api/auth/me").then((r) => setUser(r.data)).catch(() => setUser(null)).finally(() => setLoading(false))
  }, [])

  const setSession = useCallback((usuario) => {
    setUser(usuario)
  }, [])

  const clearSession = useCallback(async () => {
    try { await apiClient.post("/api/auth/logout") } catch {}
    setUser(null)
  }, [])

  const hasRole = useCallback((role) => user?.roles.includes(role) ?? false, [user])

  const isVendedor = Boolean(user?.isVendedor ?? user?.roles?.includes("Vendedor"))
  const isVendorAprovado = Boolean(isVendedor && user?.vendorId)

  return (
    <AuthContext.Provider value={{ user, loading, setSession, clearSession, hasRole, isVendorAprovado, isVendedor }}>
      {children}
    </AuthContext.Provider>
  )
}

AuthProvider.propTypes = {
  children: PropTypes.node.isRequired,
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth deve estar dentro de AuthProvider")
  return ctx
}
