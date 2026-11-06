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
import AdminPage from "../pages/AdminPage"
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
        children: [{ path: "", element: <AdminPage /> }],
      },
    ],
  },
])
