# Frontend Architecture — Arquitetura Frontend e Estado em JavaScript (Saboriza)

> **Projeto:** Saboriza — Marketplace de Gastronomia Artesanal
> **Stack Frontend:** React 18+ (Componentes Funcionais + Hooks) / JavaScript ES6+ (.js/.jsx) / Vite 5+ / TailwindCSS 3.4+ / React Router DOM 6+ / PropTypes / Axios (ou Fetch) / CSS Modules
> **Backend:** ASP.NET Core 10 WebAPI (CQRS + MediatR) — ver `docs/architecture.md`
> **Versão:** 1.1 — 2026-09-09 — **JavaScript puro com React Context API**
> **Changelog 1.1:** Migrado de TypeScript/Zustand/TanStack para JavaScript puro (.jsx) + Context API nativa + PropTypes, conforme diretrizes técnicas do frontend.

---

## 1. Visão Geral e Princípios

### 1.1. Objetivos

- **JavaScript puro:** Nenhum `.ts` ou `.tsx`. Todo código em `.js` e `.jsx` com validação via PropTypes e JSDoc opcional.
- **Sem Redux/Zustand:** Estado global exclusivamente via React Context API + `useReducer` quando necessário.
- **Fonte da verdade no backend:** Cálculos financeiros, concorrência `xmin`, geolocalização e comissões vivem na WebAPI. Frontend apenas orquestra interação e cache local.
- **SPA Vite:** Build ultrarrápido, HMR, sem SSR no MVP. Estrutura desacoplada permite migração futura sem reescrever `context/` e `services/`.

### 1.2. Fluxo de Dados JS

```
Usuario clica em CategoryCard
  → Componente chama hook de contexto (useCart, useAuth, useAddress)
    → Context atualiza estado global (setState) e persiste localStorage (apenas Cart/Address; Auth em cookie HttpOnly)
    → Service chama Axios (apiClient.post("/api/pedidos", payloadJS) com withCredentials:true)
      → Cookie HttpOnly __Host-access_token enviado automaticamente
      → WebAPI valida JWT/Policy/FluentValidation e retorna 200 | 400 {errors} | 409
    ← Service retorna objeto JS puro para o componente
  ← Componente consome novo estado do Context e re-renderiza
← UI exibe FieldError vermelho sob inputs ou toast de sucesso
```

---

## 2. Estrutura de Pastas Física — React Clássico (.jsx)

```
/
├── index.html
├── vite.config.js
├── tailwind.config.js
├── postcss.config.js
├── jsconfig.json
├── .env.example
├── .env.development
├── public/
│   ├── favicon.svg
│   ├── logo-saboriza.svg
│   └── images/placeholders/
├── src/
│   ├── main.jsx
│   ├── App.jsx
│   ├── routes.jsx
│   ├── assets/
│   │   ├── icons/
│   │   └── images/
│   ├── styles/
│   │   ├── globals.css
│   │   └── tailwind.css
│   ├── lib/
│   │   ├── apiClient.js
│   │   ├── formatters.js
│   │   ├── money.js
│   │   └── jwt.js
│   ├── context/
│   │   ├── AuthContext.jsx
│   │   ├── CartContext.jsx
│   │   ├── AddressContext.jsx
│   │   └── UiContext.jsx
│   ├── services/
│   │   ├── auth.service.js
│   │   ├── lojas.service.js
│   │   ├── produtos.service.js
│   │   ├── pedidos.service.js
│   │   ├── agenda.service.js
│   │   └── geocoding.service.js
│   ├── hooks/
│   │   ├── useDebounce.js
│   │   └── useMediaQuery.js
│   ├── components/
│   │   ├── ui/
│   │   │   ├── Button.jsx
│   │   │   ├── Input.jsx
│   │   │   ├── Badge.jsx
│   │   │   ├── Card.jsx
│   │   │   ├── Modal.jsx
│   │   │   ├── Sheet.jsx
│   │   │   ├── EmptyState.jsx
│   │   │   └── Skeleton.jsx
│   │   ├── layout/
│   │   │   ├── Header.jsx
│   │   │   ├── HybridButton.jsx
│   │   │   ├── Footer.jsx
│   │   │   ├── SidebarDashboard.jsx
│   │   │   └── AppShell.jsx
│   │   ├── category/
│   │   │   ├── CategoryCard.jsx
│   │   │   ├── CategoryGrid.jsx
│   │   │   └── CategorySkeleton.jsx
│   │   ├── store/
│   │   │   ├── StoreCard.jsx
│   │   │   ├── StoreGrid.jsx
│   │   │   └── DistanceBadge.jsx
│   │   ├── product/
│   │   │   ├── ProductCard.jsx
│   │   │   ├── ProductGrid.jsx
│   │   │   ├── DepartmentSidebar.jsx
│   │   │   └── PriceTag.jsx
│   │   ├── cart/
│   │   │   ├── CartSidebar.jsx
│   │   │   ├── CartItemRow.jsx
│   │   │   ├── CartSummary.jsx
│   │   │   └── QuantityStepper.jsx
│   │   ├── dashboard/
│   │   │   ├── KpiCard.jsx
│   │   │   ├── RevenueLineChart.jsx
│   │   │   ├── SalesPieChart.jsx
│   │   │   └── AgendaKanban.jsx
│   │   └── address/
│   │       ├── AddressBar.jsx
│   │       └── CepInput.jsx
│   ├── pages/
│   │   ├── HomePage.jsx
│   │   ├── CategoriaPage.jsx
│   │   ├── LojaPage.jsx
│   │   ├── CheckoutPage.jsx
│   │   ├── VendedorDashboardPage.jsx
│   │   └── AdminPage.jsx
│   └── routes/
│       ├── index.jsx
│       └── ProtectedRoute.jsx
└── tests/
    ├── unit/
    └── e2e/
```

Regras de colocação:

- Todo componente reutilizável em `src/components` com extensão `.jsx`.
- Páginas em `src/pages`, cada arquivo `.jsx` exporta default.
- Contextos em `src/context`, cada arquivo expõe `Context`, `Provider` e `useNomeContext`.
- Serviços em `src/services` retornam Promises com objetos JS puros, sem classes.
- `src/lib` contém utilitários puros sem JSX.

---

## 3. Estrutura de Componentes e Páginas

### 3.1. Componentes Reutilizáveis (`src/components/ui`)

| Componente | Props principais (PropTypes) | Uso |
|---|---|---|
| `Button` | `variant: primary|secondary|ghost|danger`, `size: sm|md|lg`, `loading`, `fullWidth` | Todas as ações. `primary` usa `#C1272D` |
| `Input` | `label`, `error`, `leftIcon`, `mask` | Formulários, CEP, busca |
| `Badge` | `tone: neutral|success|warning|danger|info`, `size` | Status pedido, distância |
| `Card` | `padding`, `hoverable` | Base para `StoreCard`, `ProductCard`, `KpiCard` |
| `Modal` | `open`, `onClose`, `title` | PIX fictício, confirmação |
| `Sheet` | `side: left|right|bottom`, `open` | `CartSidebar` mobile |
| `Skeleton` | `lines`, `variant` | Loading de grids |

**Exemplo `Button.jsx` com PropTypes e sem comentários no código:**

```jsx
import PropTypes from "prop-types"
import { forwardRef } from "react"

const variantClasses = {
  primary: "bg-primary text-white hover:bg-primary-dark focus-visible:ring-primary disabled:bg-neutral-300",
  secondary: "bg-secondary text-primary border border-primary hover:bg-secondary-dark",
  ghost: "bg-transparent text-primary hover:bg-primary/10",
  danger: "bg-danger text-white hover:bg-danger-dark",
}

const sizeClasses = {
  sm: "h-9 px-3 text-sm",
  md: "h-11 px-5 text-base",
  lg: "h-12 px-6 text-lg",
}

export const Button = forwardRef(function Button(
  { variant = "primary", size = "md", loading, fullWidth, className, children, disabled, ...rest },
  ref
) {
  return (
    <button
      ref={ref}
      disabled={disabled || loading}
      className={[
        "inline-flex items-center justify-center rounded-xl font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2",
        variantClasses[variant],
        sizeClasses[size],
        fullWidth ? "w-full" : "",
        className ?? "",
      ].join(" ")}
      {...rest}
    >
      {loading ? <span className="animate-pulse">Carregando...</span> : children}
    </button>
  )
})

Button.propTypes = {
  variant: PropTypes.oneOf(["primary", "secondary", "ghost", "danger"]),
  size: PropTypes.oneOf(["sm", "md", "lg"]),
  loading: PropTypes.bool,
  fullWidth: PropTypes.bool,
  className: PropTypes.string,
  children: PropTypes.node,
  disabled: PropTypes.bool,
}

Button.displayName = "Button"
```

### 3.2. Componentes de Domínio

**`CategoryCard.jsx` / `CategoryGrid.jsx` (Home Trinks):**

```jsx
import PropTypes from "prop-types"

export function CategoryCard({ nome, slug, iconeUrl, ativa, onSelect }) {
  return (
    <button
      onClick={() => onSelect(slug)}
      className={[
        "flex flex-col items-center gap-2 p-2 rounded-2xl transition-all",
        ativa ? "bg-primary text-white shadow-md scale-105" : "bg-white hover:shadow-md hover:-translate-y-0.5",
      ].join(" ")}
      aria-label={`Ver lojas de ${nome}`}
    >
      <img src={iconeUrl} alt={nome} className="h-20 w-20 rounded-full object-cover border-4 border-secondary" />
      <span className="text-sm font-medium">{nome}</span>
    </button>
  )
}

CategoryCard.propTypes = {
  nome: PropTypes.string.isRequired,
  slug: PropTypes.string.isRequired,
  iconeUrl: PropTypes.string.isRequired,
  ativa: PropTypes.bool,
  onSelect: PropTypes.func.isRequired,
}

export function CategoryGrid({ categorias, categoriaAtiva, onSelectCategoria }) {
  return (
    <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-4">
      {categorias.map((c) => (
        <CategoryCard key={c.slug} nome={c.nome} slug={c.slug} iconeUrl={c.iconeUrl} ativa={c.slug === categoriaAtiva} onSelect={onSelectCategoria} />
      ))}
    </div>
  )
}

CategoryGrid.propTypes = {
  categorias: PropTypes.arrayOf(
    PropTypes.shape({
      nome: PropTypes.string.isRequired,
      slug: PropTypes.string.isRequired,
      iconeUrl: PropTypes.string.isRequired,
    })
  ).isRequired,
  categoriaAtiva: PropTypes.string,
  onSelectCategoria: PropTypes.func.isRequired,
}
```

**`ProductCard.jsx` (GerencieAqui):**

```jsx
import PropTypes from "prop-types"

export function ProductCard({ id, nome, descricao, preco, imagemUrl, onAdd }) {
  const precoFormatado = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(preco)
  return (
    <div className="bg-white rounded-xl border border-neutral-200 overflow-hidden flex flex-col hover:shadow-lg transition-shadow">
      <img src={imagemUrl} alt={nome} className="h-40 w-full object-cover" />
      <div className="p-4 flex flex-col gap-2 flex-1">
        <span className="text-xs font-mono text-neutral-500">{id.slice(0, 8).toUpperCase()}</span>
        <h3 className="font-semibold leading-tight">{nome}</h3>
        <p className="text-sm text-neutral-600 line-clamp-2">{descricao}</p>
        <div className="mt-auto flex items-center justify-between">
          <span className="text-lg font-bold text-primary">{precoFormatado}</span>
          <button onClick={() => onAdd(id)} className="bg-primary text-white px-4 py-2 rounded-full text-sm font-semibold hover:bg-primary-dark">
            ADICIONAR ITEM
          </button>
        </div>
      </div>
    </div>
  )
}

ProductCard.propTypes = {
  id: PropTypes.string.isRequired,
  nome: PropTypes.string.isRequired,
  descricao: PropTypes.string,
  preco: PropTypes.number.isRequired,
  imagemUrl: PropTypes.string.isRequired,
  onAdd: PropTypes.func.isRequired,
}
```

**`KpiCard.jsx` (OnChef):**

```jsx
import PropTypes from "prop-types"

const toneClasses = {
  blue: "bg-blue-600 text-white",
  green: "bg-emerald-600 text-white",
  red: "bg-red-600 text-white",
  orange: "bg-orange-500 text-white",
}

export function KpiCard({ titulo, valor, variacao, tone, icon }) {
  return (
    <div className={["rounded-xl p-5 flex items-center justify-between shadow-sm", toneClasses[tone]].join(" ")}>
      <div>
        <p className="text-sm opacity-90">{titulo}</p>
        <p className="text-2xl font-bold mt-1">{valor}</p>
        {variacao ? <p className="text-xs opacity-80 mt-1">{variacao}</p> : null}
      </div>
      <div className="bg-white/20 p-3 rounded-full">{icon}</div>
    </div>
  )
}

KpiCard.propTypes = {
  titulo: PropTypes.string.isRequired,
  valor: PropTypes.string.isRequired,
  variacao: PropTypes.string,
  tone: PropTypes.oneOf(["blue", "green", "red", "orange"]).isRequired,
  icon: PropTypes.node,
}
```

### 3.3. Páginas e Rotas (React Router DOM)

`src/routes/index.jsx`:

```jsx
import { createBrowserRouter } from "react-router-dom"
import AppShell from "../components/layout/AppShell"
import ProtectedRoute from "./ProtectedRoute"
import HomePage from "../pages/HomePage"
import CategoriaPage from "../pages/CategoriaPage"
import LojaPage from "../pages/LojaPage"
import VendedorDashboardPage from "../pages/VendedorDashboardPage"

export const router = createBrowserRouter([
  {
    element: <AppShell />,
    children: [
      { path: "/", element: <HomePage /> },
      { path: "/categoria/:slug", element: <CategoriaPage /> },
      { path: "/loja/:lojaId", element: <LojaPage /> },
      {
        path: "/vendedor",
        element: <ProtectedRoute allowedRoles={["Vendedor", "Admin"]} />,
        children: [
          { path: "dashboard", element: <VendedorDashboardPage /> },
          { path: "agenda", element: <VendedorDashboardPage /> },
        ],
      },
    ],
  },
])
```

`src/routes/ProtectedRoute.jsx`:

```jsx
import PropTypes from "prop-types"
import { Navigate, Outlet } from "react-router-dom"
import { useAuth } from "../context/AuthContext"

export default function ProtectedRoute({ allowedRoles }) {
  const { user } = useAuth()
  if (!user) return <Navigate to="/login" replace />
  const temPermissao = allowedRoles.some((role) => user.roles.includes(role)) || user.isAdmin
  if (!temPermissao) return <Navigate to="/" replace />
  return <Outlet />
}

ProtectedRoute.propTypes = {
  allowedRoles: PropTypes.arrayOf(PropTypes.string).isRequired,
}
```

---

## 4. Gerenciamento de Estado Global com React Context API

### 4.1. Decisão: Context API Nativa (sem Redux/Zustand)

| Estado | Contexto | Persistência | Provedor envolve | Quando invalida |
|---|---|---|---|---|
| **Auth (JWT, user, roles, vendor_id)** | `AuthContext` | `HttpOnly Cookie` (`__Host-access_token` + `__Host-refresh_token`) + memória (`user`) | `App.jsx` raiz | logout, refresh, 401 |
| **Carrinho** | `CartContext` | `localStorage` | `App.jsx` | finalizar, trocar loja |
| **Endereço/CEP** | `AddressContext` | `localStorage` | `App.jsx` | usuário altera CEP |
| **UI (sheet, modal, toast)** | `UiContext` | memória | `App.jsx` | — |

`AuthContext` usa `HttpOnly` cookies (não acessível via JS, mitiga XSS) + `withCredentials:true`. `CartContext`/`AddressContext` continuam em `localStorage` pois são dados não sensíveis. Nenhum token em `localStorage`. Nenhum pacote externo de estado.

`src/App.jsx` envolve provedores na ordem:

```jsx
import { BrowserRouter } from "react-router-dom"
import { AuthProvider } from "./context/AuthContext"
import { CartProvider } from "./context/CartContext"
import { AddressProvider } from "./context/AddressContext"
import { UiProvider } from "./context/UiContext"

export default function App({ children }) {
  return (
    <AuthProvider>
      <AddressProvider>
        <CartProvider>
          <UiProvider>{children}</UiProvider>
        </CartProvider>
      </AddressProvider>
    </AuthProvider>
  )
}
```

### 4.2. `AuthContext.jsx` — Sessão Híbrida JWT via HttpOnly Cookies (máxima segurança)

> **Decisão normativa de segurança:** Tokens **nunca** em `localStorage` (vulnerável a XSS). Backend entrega JWT em cookies `HttpOnly`, `SameSite=Strict`, `Secure` em produção. Frontend mantém apenas `user` decodificado em memória, obtido via `/api/auth/me` com `withCredentials:true`.

```jsx
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

  const isVendorAprovado = Boolean(user?.isVendedor && user?.vendorId)

  return (
    <AuthContext.Provider value={{ user, loading, setSession, clearSession, hasRole, isVendorAprovado }}>
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
```

Regras normativas: `vendorId` nunca é inventado no frontend, vem do cookie `__Host-access_token` decodificado no backend e retornado por `/api/auth/me`. `isVendorAprovado` decide renderização do `HybridButton`. Em `login`, backend faz `Set-Cookie` HttpOnly e retorna `{ user }`; frontend apenas chama `setSession(user)`.

### 4.3. `CartContext.jsx` — Carrinho com Regra de Loja Única

```jsx
import { createContext, useContext, useState, useEffect, useCallback, useMemo } from "react"
import PropTypes from "prop-types"

const CartContext = createContext(null)

export function CartProvider({ children }) {
  const [lojaId, setLojaId] = useState(() => localStorage.getItem("saboriza:cartLojaId"))
  const [itens, setItens] = useState(() => {
    const raw = localStorage.getItem("saboriza:cartItens")
    return raw ? JSON.parse(raw) : []
  })

  useEffect(() => {
    localStorage.setItem("saboriza:cartItens", JSON.stringify(itens))
  }, [itens])

  useEffect(() => {
    if (lojaId) localStorage.setItem("saboriza:cartLojaId", lojaId)
    else localStorage.removeItem("saboriza:cartLojaId")
  }, [lojaId])

  const adicionar = useCallback((item) => {
    setItens((prev) => {
      const lojaAtual = localStorage.getItem("saboriza:cartLojaId")
      if (lojaAtual && lojaAtual !== item.lojaId) return prev
      const existe = prev.find((i) => i.produtoId === item.produtoId)
      if (existe) return prev.map((i) => (i.produtoId === item.produtoId ? { ...i, quantidade: i.quantidade + item.quantidade } : i))
      setLojaId(item.lojaId)
      return [...prev, item]
    })
  }, [])

  const remover = useCallback((produtoId) => {
    setItens((prev) => {
      const filtrados = prev.filter((i) => i.produtoId !== produtoId)
      if (filtrados.length === 0) setLojaId(null)
      return filtrados
    })
  }, [])

  const alterarQuantidade = useCallback((produtoId, quantidade) => {
    setItens((prev) => (quantidade <= 0 ? prev.filter((i) => i.produtoId !== produtoId) : prev.map((i) => (i.produtoId === produtoId ? { ...i, quantidade } : i))))
  }, [])

  const limpar = useCallback(() => {
    setItens([])
    setLojaId(null)
  }, [])

  const total = useMemo(() => itens.reduce((acc, i) => acc + i.preco * i.quantidade, 0), [itens])

  const podeAdicionarDeOutraLoja = useCallback((novaLojaId) => !lojaId || lojaId === novaLojaId, [lojaId])

  return (
    <CartContext.Provider value={{ lojaId, itens, adicionar, remover, alterarQuantidade, limpar, total, podeAdicionarDeOutraLoja }}>
      {children}
    </CartContext.Provider>
  )
}

CartProvider.propTypes = {
  children: PropTypes.node.isRequired,
}

export function useCart() {
  const ctx = useContext(CartContext)
  if (!ctx) throw new Error("useCart deve estar dentro de CartProvider")
  return ctx
}
```

Fluxo ao tentar adicionar de outra loja: `PodeAdicionarDeOutraLoja` retorna false → componente abre `Modal` de confirmação com `onConfirm: limpar + adicionar`.

### 4.4. `AddressContext.jsx` — Endereço Selecionado

```jsx
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
```

`AddressBar` lê `useAddress()` para exibir `Buscando em: [endereco]`. Se `cep === null`, exibe CTA.

---

## 5. Comunicação com o Backend via Axios / Fetch — Convenções HTTP CQRS

### 5.1. `apiClient.js` — Axios com HttpOnly Cookies + Refresh Automático + CSRF

```jsx
import axios from "axios"

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 15000,
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
})

let isRefreshing = false
let failedQueue = []

function processQueue(error) {
  failedQueue.forEach(({ resolve, reject }) => (error ? reject(error) : resolve()))
  failedQueue = []
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    if (error.response?.status === 401 && !original._retry) {
      if (isRefreshing) return new Promise((resolve, reject) => failedQueue.push({ resolve, reject }))
      original._retry = true
      isRefreshing = true
      try {
        await axios.post(`${import.meta.env.VITE_API_URL}/api/auth/refresh`, {}, { withCredentials: true })
        processQueue(null)
        return apiClient(original)
      } catch (e) {
        processQueue(e)
        window.location.href = "/login"
        return Promise.reject(e)
      } finally {
        isRefreshing = false
      }
    }
    return Promise.reject(error)
  }
)
```

`withCredentials:true` envia automaticamente `__Host-access_token` e `__Host-refresh_token` (HttpOnly). Nenhum `Authorization` manual, nenhum `localStorage`. Em `POST/PUT/PATCH/DELETE`, interceptor adicional lê `XSRF-TOKEN` (cookie não-HttpOnly) e envia `X-CSRF-TOKEN` header.

Alternativa `Fetch` pura:

```jsx
export async function apiFetch(path, { method = "GET", body, headers = {} } = {}) {
  const res = await fetch(`${import.meta.env.VITE_API_URL}${path}`, {
    method,
    headers: { "Content-Type": "application/json", ...headers },
    body: body ? JSON.stringify(body) : undefined,
    credentials: "include",
  })
  const data = await res.json().catch(() => null)
  if (!res.ok) {
    const err = new Error(data?.error ?? "Erro na requisição")
    err.status = res.status
    err.data = data
    throw err
  }
  return data
}
```

### 5.2. Estrutura de Payloads — Objetos JavaScript Dinâmicos

Cada serviço constrói objeto JS puro sem classes. Nomes de chaves em `camelCase` no frontend são convertidos para `PascalCase` se a WebAPI esperar, mas este projeto padroniza `camelCase` em ambos para evitar mapeamento.

`src/services/lojas.service.js`:

```jsx
import { apiClient } from "../lib/apiClient"

export const lojasService = {
  listarProximas({ lat, lng, raioKm = 5, categoriaId, pagina = 1 }) {
    return apiClient.get("/api/lojas/proximas", { params: { lat, lng, raioKm, categoriaId, pagina } }).then((r) => r.data)
  },
  listarPorCategoria(slug, { lat, lng } = {}) {
    return apiClient.get(`/api/categorias/${slug}/lojas`, { params: { lat, lng } }).then((r) => r.data)
  },
  obterProdutos(lojaId, { departamento } = {}) {
    return apiClient.get(`/api/lojas/${lojaId}/produtos`, { params: { departamento } }).then((r) => r.data)
  },
}
```

`src/services/pedidos.service.js`:

```jsx
import { apiClient } from "../lib/apiClient"

export const pedidosService = {
  criar({ lojaId, itens }) {
    return apiClient.post("/api/pedidos", { lojaId, itens }).then((r) => r.data)
  },
  aprovar(pedidoId) {
    return apiClient.post(`/api/pedidos/${pedidoId}/aprovar`).then((r) => r.data)
  },
  simularPix(pedidoId) {
    return apiClient.post(`/api/pedidos/${pedidoId}/simular-pix`).then((r) => r.data)
  },
}
```

`src/services/auth.service.js`:

```jsx
import { apiClient } from "../lib/apiClient"

export const authService = {
  login({ email, senha }) {
    return apiClient.post("/api/auth/login", { email, senha }).then((r) => r.data)
  },
  registrar({ nome, email, senha, cpf }) {
    return apiClient.post("/api/auth/registro", { nome, email, senha, cpf }).then((r) => r.data)
  },
}
```

### 5.3. Mapeamento de Erros — FluentValidation → FieldError Vermelho

Backend retorna `400` com `errors` por campo:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Erro de validação",
  "status": 400,
  "errors": {
    "Nome": ["Nome é obrigatório."],
    "Preco": ["Preço deve ser maior que zero."]
  }
}
```

**Utilitário `src/lib/validators.js` sem comentários:**

```jsx
export function parseValidationErrors(error) {
  const data = error?.response?.data
  if (data?.errors && typeof data.errors === "object") return data.errors
  return null
}

export function getFieldError(errors, field) {
  return errors?.[field]?.[0]
}
```

**Uso em formulário JSX com PropTypes e alertas vermelhos:**

```jsx
import { useState } from "react"
import PropTypes from "prop-types"
import { parseValidationErrors, getFieldError } from "../../lib/validators"
import { Input } from "../../components/ui/Input"
import { Button } from "../../components/ui/Button"

export function ProdutoForm({ onSubmit }) {
  const [errors, setErrors] = useState(null)

  async function handleSubmit(event) {
    event.preventDefault()
    const formData = new FormData(event.currentTarget)
    const payload = Object.fromEntries(formData)
    try {
      await onSubmit(payload)
      setErrors(null)
    } catch (err) {
      const ve = parseValidationErrors(err)
      if (ve) {
        setErrors(ve)
        return
      }
      const msg = err?.response?.data?.error ?? "Erro inesperado"
      setErrors({ _global: [msg] })
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {errors?._global ? <p className="bg-danger/10 text-danger border border-danger rounded-lg p-3 text-sm">{errors._global[0]}</p> : null}
      <Input name="nome" label="Nome" error={getFieldError(errors, "Nome")} />
      <Input name="preco" label="Preço" type="number" step="0.01" error={getFieldError(errors, "Preco")} />
      <Button type="submit">Salvar</Button>
    </form>
  )
}

ProdutoForm.propTypes = {
  onSubmit: PropTypes.func.isRequired,
}
```

Padrões visuais: `Input` com `error` exibe borda `border-danger` e texto `text-danger text-sm mt-1` com `aria-live="polite"`. `409` de concorrência exibe `Alert` amarelo + botão Recarregar.

---

## 6. Roteamento e Proteção por Roles (React Router DOM)

`src/routes/index.jsx` já mostrado. `ProtectedRoute` valida `user.roles` do `AuthContext`.

Navegação híbrida é feita pelo `HybridButton` lendo `isVendorAprovado`.

---

## 7. Variáveis de Ambiente e Build (Vite)

Arquivo `.env.example`:

```
VITE_API_URL=https://api.saboriza.com
VITE_ENV=production
```

`vite.config.js`:

```jsx
import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"
import path from "path"

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": "http://localhost:5000",
    },
  },
  build: {
    outDir: "dist",
    sourcemap: false,
  },
})
```

`src/main.jsx`:

```jsx
import React from "react"
import ReactDOM from "react-dom/client"
import { RouterProvider } from "react-router-dom"
import { router } from "./routes"
import { AuthProvider } from "./context/AuthContext"
import { CartProvider } from "./context/CartContext"
import { AddressProvider } from "./context/AddressContext"
import "./styles/globals.css"

ReactDOM.createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <AuthProvider>
      <AddressProvider>
        <CartProvider>
          <RouterProvider router={router} />
        </CartProvider>
      </AddressProvider>
    </AuthProvider>
  </React.StrictMode>
)
```

---

## 8. Checklist de Implementação Frontend (JavaScript Puro)

- [ ] Criar projeto Vite com template `react` (JS, não TS) + Tailwind + React Router DOM + PropTypes + Axios
- [ ] Configurar `vite.config.js` com alias `@` e proxy `/api`
- [ ] Implementar `apiClient.js` com `withCredentials:true` + refresh via cookie HttpOnly + `VITE_API_URL`
- [ ] Implementar `AuthContext.jsx` com cookie HttpOnly (memória, sem localStorage) e `CartContext.jsx`/`AddressContext.jsx` com `localStorage` + PropTypes, sem Redux
- [ ] Criar serviços `auth.service.js`, `lojas.service.js`, `pedidos.service.js` retornando objetos JS puros
- [ ] Implementar `parseValidationErrors` + `FieldError` vermelho + toast para todos os formulários
- [ ] Criar componentes base `Button.jsx`, `Input.jsx`, `Badge.jsx`, `Modal.jsx` com PropTypes
- [ ] Criar `ProtectedRoute.jsx` por roles e `HybridButton.jsx` lendo `useAuth`
- [ ] Garantir nenhum `//` em `.js/.jsx` e nenhum `.ts/.tsx` no projeto
