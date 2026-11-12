import { Outlet, useLocation } from "react-router-dom"
import { AdminSidebar } from "./AdminSidebar"
import { AdminHeader } from "./AdminHeader"
import { Toast } from "../ui/Toast"

export default function AdminShell() {
  const location = useLocation()
  return (
    <div className="min-h-screen flex flex-col bg-neutral-100">
      <AdminHeader />
      <div className="flex-1 flex">
        <AdminSidebar />
        <main className="flex-1 overflow-y-auto p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
      <Toast />
    </div>
  )
}
