import PropTypes from "prop-types"
import { Navigate, Outlet } from "react-router-dom"
import { useAuth } from "../context/AuthContext"

export default function ProtectedRoute({ allowedRoles }) {
  const { user, loading } = useAuth()
  if (loading) return <p className="max-w-7xl mx-auto px-4 py-8 text-neutral-600">Verificando sessão...</p>
  if (!user) return <Navigate to="/login" replace />
  const temPermissao = allowedRoles.some((role) => user.roles.includes(role)) || user.isAdmin
  if (!temPermissao) return <Navigate to="/" replace />
  return <Outlet />
}

ProtectedRoute.propTypes = {
  allowedRoles: PropTypes.arrayOf(PropTypes.string).isRequired,
}
