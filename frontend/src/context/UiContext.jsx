import { createContext, useContext, useState } from "react"
import PropTypes from "prop-types"

const UiContext = createContext(null)

export function UiProvider({ children }) {
  const [toast, setToast] = useState(null)
  const [modal, setModal] = useState(null)

  function showToast(message, type = "success") {
    setToast({ message, type })
    setTimeout(() => setToast(null), 3000)
  }

  return (
    <UiContext.Provider value={{ toast, showToast, modal, setModal }}>
      {children}
    </UiContext.Provider>
  )
}

UiProvider.propTypes = {
  children: PropTypes.node.isRequired,
}

export function useUi() {
  const ctx = useContext(UiContext)
  if (!ctx) throw new Error("useUi deve estar dentro de UiProvider")
  return ctx
}
