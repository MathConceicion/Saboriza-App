import { Outlet, useLocation } from "react-router-dom"
import { Header } from "./Header"
import { Footer } from "./Footer"
import { Toast } from "../ui/Toast"

export default function AppShell() {
  const location = useLocation()
  const semRodape = location.pathname === "/login" || location.pathname === "/registro"
  return (
    <div className="min-h-screen flex flex-col">
      <Header />
      <main className="flex-1">
        <Outlet />
      </main>
      {!semRodape ? <Footer /> : null}
      <Toast />
    </div>
  )
}
