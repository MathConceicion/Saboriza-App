import { createBrowserRouter, Navigate } from "react-router-dom"
import HomePage from "../pages/HomePage"
import BuscarPage from "../pages/BuscarPage"
import CategoriaPage from "../pages/CategoriaPage"
import LojaPage from "../pages/LojaPage"
import CheckoutPage from "../pages/CheckoutPage"
import MeusPedidosPage from "../pages/MeusPedidosPage"
import ContaPage from "../pages/ContaPage"
import SolicitarUpgradePage from "../pages/SolicitarUpgradePage"
import LoginPage from "../pages/LoginPage"
import RegistroPage from "../pages/RegistroPage"
import VendedorDashboardPage from "../pages/VendedorDashboardPage"
import AgendaPage from "../pages/AgendaPage"
import PedidosVendedorPage from "../pages/PedidosVendedorPage"
import ProdutosPage from "../pages/ProdutosPage"
import ConfiguracoesPage from "../pages/ConfiguracoesPage"
import { VendedorShell } from "../components/layout/VendedorShell"
import AdminDashboardPage from "../pages/admin/AdminDashboardPage"
import AdminSolicitacoesLojasPage from "../pages/admin/AdminSolicitacoesLojasPage"
import AdminUsuariosPage from "../pages/admin/AdminUsuariosPage"
import AdminLojasPage from "../pages/admin/AdminLojasPage"
import AdminProdutosPage from "../pages/admin/AdminProdutosPage"
import AdminNotificacoesPage from "../pages/admin/AdminNotificacoesPage"
import AdminAuditoriaPage from "../pages/admin/AdminAuditoriaPage"
import AdminConfiguracoesPage from "../pages/admin/AdminConfiguracoesPage"
import AdminShell from "../components/layout/AdminShell"
import ProtectedRoute from "./ProtectedRoute"
import AppShell from "../components/layout/AppShell"

export const router = createBrowserRouter([
  {
    element: <AppShell />,
    children: [
      { path: "/", element: <HomePage /> },
      { path: "/buscar", element: <BuscarPage /> },
      { path: "/categoria/:slug", element: <CategoriaPage /> },
      { path: "/loja/:lojaId", element: <LojaPage /> },
      {
        path: "/checkout",
        element: <ProtectedRoute allowedRoles={["Cliente", "Vendedor", "Admin"]} />,
        children: [{ path: "", element: <CheckoutPage /> }],
      },
      {
        path: "/meus-pedidos",
        element: <ProtectedRoute allowedRoles={["Cliente", "Vendedor", "Admin"]} />,
        children: [{ path: "", element: <MeusPedidosPage /> }],
      },
      {
        path: "/conta",
        element: <ProtectedRoute allowedRoles={["Cliente", "Vendedor", "Admin"]} />,
        children: [{ path: "", element: <ContaPage /> }],
      },
      {
        path: "/vender",
        element: <ProtectedRoute allowedRoles={["Cliente", "Vendedor", "Admin"]} />,
        children: [{ path: "", element: <SolicitarUpgradePage /> }],
      },
      { path: "/login", element: <LoginPage /> },
      { path: "/registro", element: <RegistroPage /> },
      {
        path: "/vendedor",
        element: <ProtectedRoute allowedRoles={["Vendedor", "Admin"]} />,
        children: [
          { index: true, element: <Navigate to="dashboard" replace /> },
          {
            element: <VendedorShell />,
            children: [
              { path: "dashboard", element: <VendedorDashboardPage /> },
              { path: "agenda", element: <AgendaPage /> },
              { path: "produtos", element: <ProdutosPage /> },
              { path: "pedidos", element: <PedidosVendedorPage /> },
              { path: "aprovar", element: <Navigate to="../pedidos" replace /> },
              { path: "configuracoes", element: <ConfiguracoesPage /> },
            ],
          },
        ],
      },
      {
        path: "/admin",
        element: <ProtectedRoute allowedRoles={["Admin"]} />,
        children: [
          {
            element: <AdminShell />,
            children: [
              { index: true, element: <AdminDashboardPage /> },
              { path: "dashboard", element: <AdminDashboardPage /> },
              { path: "solicitacoes-lojas", element: <AdminSolicitacoesLojasPage /> },
              { path: "usuarios", element: <AdminUsuariosPage /> },
              { path: "lojas", element: <AdminLojasPage /> },
              { path: "produtos", element: <AdminProdutosPage /> },
              { path: "notificacoes", element: <AdminNotificacoesPage /> },
              { path: "auditoria", element: <AdminAuditoriaPage /> },
              { path: "configuracoes", element: <AdminConfiguracoesPage /> },
            ],
          },
        ],
      },
    ],
  },
])
