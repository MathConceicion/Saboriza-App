import { RouterProvider } from "react-router-dom"
import { router } from "./routes"
import { AuthProvider } from "./context/AuthContext"
import { CartProvider } from "./context/CartContext"
import { AddressProvider } from "./context/AddressContext"
import { UiProvider } from "./context/UiContext"

export default function App() {
  return (
    <AuthProvider>
      <AddressProvider>
        <CartProvider>
          <UiProvider>
            <RouterProvider router={router} />
          </UiProvider>
        </CartProvider>
      </AddressProvider>
    </AuthProvider>
  )
}
