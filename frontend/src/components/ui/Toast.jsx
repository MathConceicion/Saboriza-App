import { useUi } from "../../context/UiContext"

const typeClasses = {
  success: "bg-success text-white",
  danger: "bg-danger text-white",
  warning: "bg-warning text-white",
  info: "bg-info text-white",
}

export function Toast() {
  const { toast } = useUi()
  if (!toast) return null
  return (
    <div aria-live="polite" className={["fixed bottom-4 left-1/2 -translate-x-1/2 z-50 px-4 py-3 rounded-xl shadow-lg text-sm font-semibold", typeClasses[toast.type] ?? typeClasses.success].join(" ")}>
      {toast.message}
    </div>
  )
}
