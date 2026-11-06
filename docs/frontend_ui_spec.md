# Frontend UI Spec — Especificação de Telas, Validações e Design System (Saboriza) — JavaScript Puro

> **Projeto:** Saboriza — Marketplace de Gastronomia Artesanal
> **Identidade:** Vermelho Cereja #C1272D (primária) + Rosa Pastel + Amarelo Dourado + Semânticas de Dashboard
> **Stack UI:** React 18+ (.jsx) / TailwindCSS 3.4+ / React Router DOM 6+ / PropTypes / Axios / Lucide Icons / Recharts / Framer Motion (opcional)
> **Versão:** 1.1 — 2026-09-09 — **JavaScript puro, sem TypeScript**
> **Changelog 1.1:** Revisado para JavaScript ES6+ com PropTypes, validações JS puras e transições via React Router DOM.

---

## 1. Guia de Estilo — Design System Saboriza

### 1.1. Paleta de Cores — Extração da Logo Cereja

A logo Saboriza apresenta cereja estilizada com arcos concêntricos. Extração normativa:

| Token | Hex | Uso | Contraste com branco |
|---|---|---|---|
| `primary` Cereja | `#C1272D` | Botões primários, ícones ativos, links, header ativo, badge conversão | 5.8:1 (AA ok) |
| `primary-dark` | `#A01E24` | Hover de primary, pressed | 7.1:1 |
| `primary-light` | `#E8A0A3` | Background hover sutil, borda focus | — |
| `secondary` Rosa Pastel | `#F8BBD0` | Badges, fundo de cards categoria selecionada, alertas sutis | 1.4:1 (usar texto `#5A1A1F`) |
| `secondary-dark` | `#F48FB1` | Hover secondary, detalhes | — |
| `accent` Amarelo Dourado/Mostarda | `#F2C14E` | Badge destaque, estrela avaliação, alerta promoção, ícone categoria | 1.9:1 (texto `#3A2A00` sobre) |
| `accent-dark` | `#E6A532` | Hover accent | — |
| `neutral-900` | `#1A1A1A` | Texto primário | — |
| `neutral-600` | `#6B7280` | Texto secundário | 4.6:1 sobre branco |
| `neutral-200` | `#E5E7EB` | Bordas, divisores | — |
| `neutral-100` | `#F9FAFB` | Fundo página, cards neutros | — |
| `success` Dashboard | `#2E7D32` | KPI faturamento, vendas | 4.9:1 |
| `info` Dashboard | `#1976D2` | KPI total vendas hoje | 4.6:1 |
| `danger` Dashboard | `#C62828` | KPI despesas, erro | 5.6:1 |
| `warning` Dashboard | `#EF6C00` | KPI ticket médio | 3.1:1 (texto branco 4.5:1 com font-bold 14px+) |

**Cenário de Exceção — Dashboard:** Métricas analíticas mantêm semânticas clássicas (verde, azul, vermelho, laranja) mesmo dentro da identidade Saboriza, para leitura cognitiva rápida. O header do dashboard permanece em `neutral-900` com logo Saboriza em branco para não competir.

### 1.2. Variáveis Universais (CSS / Tailwind em JS)

`src/styles/globals.css`:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

:root {
  --color-primary: 193 39 45;
  --color-primary-dark: 160 30 36;
  --color-secondary: 248 187 208;
  --color-accent: 242 193 78;
  --color-neutral-900: 26 26 26;
  --color-neutral-600: 107 114 128;
  --color-neutral-200: 229 231 235;
  --color-success: 46 125 50;
  --color-info: 25 118 210;
  --color-danger: 198 40 40;
  --color-warning: 239 108 0;
  --radius-xl: 0.75rem;
  --radius-2xl: 1rem;
  --shadow-card: 0 1px 3px rgba(0,0,0,0.08), 0 4px 12px rgba(0,0,0,0.05);
}

* {
  font-feature-settings: "rlig" 1, "calt" 1;
}

body {
  background-color: #F9FAFB;
  color: #1A1A1A;
  font-family: Inter, system-ui, -apple-system, sans-serif;
}
```

`tailwind.config.js` (JavaScript puro):

```js
export default {
  content: ["./index.html", "./src/**/*.{js,jsx}"],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: "#C1272D",
          dark: "#A01E24",
          light: "#E8A0A3",
        },
        secondary: {
          DEFAULT: "#F8BBD0",
          dark: "#F48FB1",
        },
        accent: {
          DEFAULT: "#F2C14E",
          dark: "#E6A532",
        },
        success: "#2E7D32",
        info: "#1976D2",
        danger: "#C62828",
        warning: "#EF6C00",
      },
      borderRadius: {
        xl: "0.75rem",
        "2xl": "1rem",
      },
      boxShadow: {
        card: "0 1px 3px rgba(0,0,0,0.08), 0 4px 12px rgba(0,0,0,0.05)",
      },
      fontFamily: {
        sans: ["Inter", "system-ui", "sans-serif"],
        display: ["Poppins", "sans-serif"],
      },
    },
  },
  plugins: [],
}
```

**Aplicação correta para contraste e acessibilidade:**

- Texto sobre `primary #C1272D`: sempre branco `#FFFFFF` (5.8:1) — WCAG AA aprovado para 14px.
- Texto sobre `secondary #F8BBD0`: usar `text-[#5A1A1F]` (7.2:1), nunca branco.
- Texto sobre `accent #F2C14E`: usar `text-[#3A2A00]`.
- Bordas: `border-neutral-200` em cards, `border-primary` em focus (`ring-2 ring-primary`).
- Fundos: página `bg-neutral-100`, cards `bg-white`, header `bg-white` com `shadow-sm`, dashboard header `bg-neutral-900`.

### 1.3. Tipografia e Espaçamento

| Elemento | Tailwind | Peso | Uso |
|---|---|---|---|
| `display` logo | `font-display text-2xl font-bold tracking-tight` | 700 | Header Saboriza |
| `h1` | `text-3xl font-bold` | 700 | Título página |
| `h2` | `text-xl font-semibold` | 600 | Seção |
| `body` | `text-base leading-6` | 400 | Descrição produto |
| `caption` | `text-sm text-neutral-600` | 400 | Distância, categoria |
| `price` | `text-lg font-bold text-primary` | 700 | Preço |
| `mono` | `font-mono text-xs text-neutral-500` | 400 | Código produto |

Espaçamento base 4px. Cards com `p-4`, grids com `gap-4` em mobile e `gap-6` em desktop.

### 1.4. Validações de UI (JavaScript Puro)

Sem Zod/TypeScript. Validações feitas com funções JS puras + PropTypes + feedback inline.

Exemplo de validação de CEP e preço em `src/lib/validators.js`:

```js
export function validarCep(cep) {
  const digits = cep.replace(/\D/g, "")
  return digits.length === 8
}

export function validarPreco(preco) {
  const n = Number(preco)
  return Number.isFinite(n) && n > 0
}

export function validarNome(nome) {
  return typeof nome === "string" && nome.trim().length >= 3 && nome.trim().length <= 150
}
```

Componente `Input.jsx` exibe `error` em vermelho `#C1272D` abaixo do campo com `aria-live="polite"` e borda `border-danger`. Ver seção 2 para uso em formulários.

---

## 2. Engenharia de Telas — Step-by-Step UI (JavaScript)

### 2.1. Tela Inicial do Cliente — Grid de Categorias (Estilo Trinks)

**Referência:** Trinks — header com "Buscando em: [CEP/Endereço]" + grid de categorias circulares.

**Layout wireframe (desktop 1440px, mobile 375px):**

```
┌─ Header (sticky, bg-white, border-b) ─────────────────────────────────┐
│ [Logo Saboriza #C1272D]   [Buscando em: ▾ Av. Paulista, 1000 ▼]  [Login] [Carrinho(2)] [HybridButton] │
└────────────────────────────────────────────────────────────────────────┘
┌─ Hero / AddressBar (bg-white, shadow-sm) ──────────────────────────────┐
│  📍 Buscando em: [ Input CEP/Endereço com ícone lupa ] [Buscar]        │
│  Texto auxiliar: "Informe seu endereço para ver lojas próximas"         │
└────────────────────────────────────────────────────────────────────────┘
┌─ Categorias (py-8, bg-neutral-100) ────────────────────────────────────┐
│  Título: "O que você quer comer hoje?"  (text-2xl font-bold)          │
│  Grid 4 col mobile / 6 tablet / 8 desktop de CategoryCard redondos    │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ │
│  │  🍕  │ │  🍔  │ │  🍣  │ │  🍦  │ │  🍩  │ │  🥖  │ │  🥗  │ │  🍛  │ │
│  │Pizzar│ │Hambur│ │Japone│ │Sorvet│ │Doceri│ │Padari│ │Saudav│ │Brasile│ │
│  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ │
│  Cada card: imagem 80x80 rounded-full border-4 border-secondary, nome text-sm │
└────────────────────────────────────────────────────────────────────────┘
┌─ Lojas Próximas (se endereço preenchido) ──────────────────────────────┐
│  Título: "Lojas próximas a você" + [raio 5km ▾]                        │
│  StoreGrid 1 col mobile / 2 tablet / 3 desktop                         │
└────────────────────────────────────────────────────────────────────────┘
```

**Passo a passo de renderização e comportamento (JS + Context):**

1. Ao montar `HomePage.jsx`, `useAddress()` verifica se `cep/lat/lng` já existem em `localStorage` via `AddressContext`. Se sim, preenche `AddressBar` e dispara `lojasService.listarProximas({lat,lng,raioKm:5})` com `useState` + `useEffect`.
2. `CategoryGrid.jsx` busca `GET /api/categorias` com `fetch` dentro de `useEffect` e guarda em `useState`. Enquanto `loading`, renderiza `CategorySkeleton` (8 círculos pulsando com `animate-pulse`).
3. Cada `CategoryCard.jsx` é `button` com `rounded-full` e `border-secondary`. Hover `scale-105 shadow-md`. Ativa `bg-primary text-white`. Clique executa `navigate(`/categoria/${slug}?lat=${lat}&lng=${lng}`)` via `useNavigate` do React Router DOM.
4. Se `AddressBar` vazio (`cep === null`), `StoreGrid` não renderiza. Exibe `EmptyState` com ilustração + "Informe seu CEP para descobrir sabores perto de você" + `CepInput` focado.
5. Ao digitar CEP em `CepInput.jsx`, `useDebounce` (hook JS com `setTimeout`) aguarda 400ms e chama `GET /api/geocoding?cep=` (ou ViaCEP local) para preencher `latitude/longitude` e chamar `definirEndereco(cep, endereco, lat, lng)` do `AddressContext`. Input mostra validação com `validarCep` e erro vermelho se inválido.
6. Responsivo: mobile `grid-cols-3`, tablet `grid-cols-4`, desktop `grid-cols-8`. Gap 12px mobile, 16px desktop.
7. Acessibilidade: cada `CategoryCard` tem `aria-label="Ver lojas de Pizzarias"` e `role="button"`. Navegação por teclado com `Tab` e `Enter`.

### 2.2. Comportamento de Transição — Categoria ➔ Lojas Próximas ➔ Portfólio

**Cadeia de navegação JS pura:**

```
Home (/) ──click Pizzarias (.jsx onSelect)──> /categoria/pizzarias?lat=-23.56&lng=-46.65
  → CategoriaPage.jsx
    → Lê slug via useParams + lat/lng via useSearchParams
    → useEffect busca lojasService.listarPorCategoria(slug, {lat,lng})
    → Renderiza StoreGrid
      ──click StoreCard──> /loja/:lojaId
        → LojaPage.jsx
          → Lê lojaId via useParams
          → Busca lojaService.obterProdutos(lojaId)
          → Renderiza DepartmentSidebar + ProductGrid + CartSidebar
```

**Detalhe da transição Categoria → Lojas:**

1. `HomePage.jsx` armazena `categoriaAtiva` em `useState` local. Ao clicar, `onSelectCategoria(slug)` faz `navigate`.
2. `CategoriaPage.jsx` extrai `slug` com `useParams()` e `lat/lng` com `useSearchParams()`. Sem `lat/lng`, exibe `AddressBar` para preencher antes de listar.
3. Enquanto busca, exibe `Skeleton` de `StoreCard`. Erro de rede exibe `Alert` vermelho com botão Tentar novamente.
4. Cada `StoreCard.jsx` recebe `nome, categoria, distanciaKm, fotoUrl, avaliacao`. Distância vem do backend PostGIS `ST_Distance` e é formatada em `src/lib/formatters.js`:

```js
export function formatDistancia(km) {
  return `${km.toFixed(1).replace(".", ",")} km`
}
```

5. Clique em `StoreCard` navega para `LojaPage.jsx`. Antes de navegar, `CartContext` verifica se `lojaId` é diferente do `cart.lojaId` — se sim, `CartSidebar` não navega direto, mas `ProdutoForm` futuro lidará com conflito.

**Card de Loja (`StoreCard.jsx` wireframe):**

```
┌─ StoreCard (bg-white rounded-xl border hover:shadow-card) ─┐
│ [Foto 80x80 rounded-lg]  Nome: Cantina da Maria            │
│                         Categoria: Pizzarias • 1.2 km       │
│                         ⭐ 4.8 (124 avaliações)            │
│                         🕒 Aberto agora • Fecha 22h        │
│                         [Ver Portfólio →] (text-primary)   │
└─────────────────────────────────────────────────────────────┘
```

PropTypes de `StoreCard.jsx`:

```jsx
import PropTypes from "prop-types"

export function StoreCard({ fotoUrl, nome, categoria, distanciaKm, avaliacao }) {
  return (
    <div className="bg-white rounded-xl border p-4 flex gap-4 hover:shadow-card">
      <img src={fotoUrl} alt={nome} className="h-20 w-20 rounded-lg object-cover" />
      <div className="flex flex-col">
        <h3 className="font-semibold">{nome}</h3>
        <p className="text-sm text-neutral-600">{categoria} • {formatDistancia(distanciaKm)}</p>
        <span className="text-sm">⭐ {avaliacao}</span>
      </div>
    </div>
  )
}

StoreCard.propTypes = {
  fotoUrl: PropTypes.string.isRequired,
  nome: PropTypes.string.isRequired,
  categoria: PropTypes.string.isRequired,
  distanciaKm: PropTypes.number.isRequired,
  avaliacao: PropTypes.number,
}
```

### 2.3. Tela de Portfólio / Vitrine da Loja — Estilo GerencieAqui (Tablet) em JSX

**Layout desktop 1440px:**

```
┌─ Header Saboriza (sticky) ──────────────────────────────────────────────────────────┐
│ Logo | Buscando em: Paulista ▾ | [HybridButton]                                    │
└────────────────────────────────────────────────────────────────────────────────────┘
┌─ Breadcrumb: Início > Pizzarias > Cantina da Maria ────────────────────────────────┐
│ Banner Loja: [Foto cover 16:9] + Avatar + Nome + Avaliação + Distância 1.2km      │
└─────────────────────────────────────────────────────────────────────────────────────┘
┌────────────────────────────────────────────────────────────────────────────────────┐
│ ┌─ DeptSidebar (240px, sticky top-20) ─┐ ┌─ ProductGrid ─┐ ┌─ CartSidebar 360px ┐ │
│ │ Departamentos                        │ │ 2 col tablet  │ │ Meu Carrinho (3)   │ │
│ │ ● Porções e Fritas (12)              │ │ 3 col desktop │ │  Pizza 2x R$...    │ │
│ │   Combos (8)                         │ │ ┌──────────┐  │ │ [ - 2 + ]  [🗑]  │ │
│ │   Bebidas (15)                       │ │ │ProductCard│  │ │ Total: R$ 89,90  │ │
│ │ Ativo bg-primary text-white          │ │ └──────────┘  │ │ [FINALIZAR COMPRA] │ │
│ └──────────────────────────────────────┘ └───────────────┘ └──────────────────┘ │
└────────────────────────────────────────────────────────────────────────────────────┘
```

**Passo a passo JS:**

1. Rota `/loja/:lojaId` carrega `LojaPage.jsx` que usa `useParams` para `lojaId` e dois `useEffect`: um para `GET /api/lojas/{id}` (header) e outro para `GET /api/lojas/{id}/produtos?departamento=` via `produtosService`.
2. `DepartmentSidebar.jsx` recebe `departamentos` com contadores e `departamentoAtivo` via `useState`. Clique atualiza `setDepartamentoAtivo` sem reload. `PropTypes` valida `arrayOf(shape({nome, count}))`.
3. `ProductGrid.jsx` mapeia `produtos` filtrados em `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4`. Cada `ProductCard.jsx` mostra código, nome, descrição `line-clamp-2`, preço formatado, botão `ADICIONAR ITEM` em `bg-primary`.
4. Ao clicar `ADICIONAR ITEM`, chama `const { adicionar, podeAdicionarDeOutraLoja } = useCart()`. Se `!podeAdicionarDeOutraLoja(lojaId)`, abre `Modal.jsx` de confirmação. Caso contrário, `adicionar({ produtoId, nome, preco, quantidade:1, lojaId })` e exibe toast verde.
5. `CartSidebar.jsx` lê `const { itens, total, remover, alterarQuantidade } = useCart()` e renderiza `CartItemRow.jsx` com `QuantityStepper.jsx`. Em mobile, `CartSidebar` vira `Sheet` bottom com `fixed bottom-0` e botão `Ver Carrinho (R$ total)`.
6. Botão `FINALIZAR COMPRA` desabilitado se `itens.length === 0`. Clique navega para `CheckoutPage.jsx` que chama `pedidosService.criar({ lojaId, itens })`. Após `201`, exibe `Modal` com `PixCopiaECola` e botão `Já paguei` que chama `pedidosService.simularPix(pedidoId)`. Sucesso limpa carrinho via `limpar()`.

**Validação JS no portfólio:**

- `validarPreco` aplicado ao renderizar preço; se inválido, não renderiza botão.
- `QuantityStepper` limita `1..99` com validação JS pura.

### 2.4. Dashboard do Vendedor — Estilo OnChef em JSX

**Layout 1440px:**

```
┌─ TopBar (bg-neutral-900 text-white h-16) ──────────────────────────────────────────┐
│ [Logo branco] Cantina da Maria — Olá, Maria! • 09/09/2026 14:30 • Aberto ●       │
└────────────────────────────────────────────────────────────────────────────────────┘
┌─ Nav tabs: [Dashboard] [Agenda] [Pedidos] [Produtos] ──────────────────────────────┐
│ ativo border-b-2 border-primary                                                    │
└────────────────────────────────────────────────────────────────────────────────────┘
┌─ KPIs grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 ────────────────────────────┐
│ [KpiCard blue    R$ 1.240] [KpiCard green R$ 3.420] [KpiCard red R$ 280] [KpiCard orange R$ 42]│
└────────────────────────────────────────────────────────────────────────────────────┘
┌─ Gráficos grid-cols-1 lg:grid-cols-3 gap-6 ────────────────────────────────────────┐
│ [RevenueLineChart col-span-2]  [SalesPieChart]                                      │
└────────────────────────────────────────────────────────────────────────────────────┘
┌─ AgendaKanban grid-cols-1 md:grid-cols-3 gap-4 ───────────────────────────────────┐
│ [Em Preparo 3]  [Pronto p/ Retirada 1]  [Finalizado 1]                             │
└────────────────────────────────────────────────────────────────────────────────────┘
```

**Passo a passo JS puro:**

1. `VendedorDashboardPage.jsx` usa `useAuth()` para pegar `user.lojaId` e dispara três `useEffect` paralelos: `agendaService.listarDoDia(lojaId)`, `pedidosService.metricas(lojaId)`, `pedidosService.faturamento(lojaId)`. Cada um guarda em `useState` local.
2. `KpiCard.jsx` recebe `titulo, valor, variacao, tone` com `PropTypes`. Tons fixos: `blue #1976D2`, `green #2E7D32`, `red #C62828`, `orange #EF6C00`.
3. `RevenueLineChart.jsx` usa `Recharts` com `Line` `stroke="#C1272D"` e `SalesPieChart.jsx` com `Cell` `fill` primário/accent/secondary.
4. `AgendaKanban.jsx` consome lista ordenada por `AprovadoEmUtc ASC` (ver `features_spec.md §4.4`) e renderiza colunas por `Status`. Cada `AgendaCard` exibe `PosicaoFila`, `PedidoId` truncado, `ClienteNome`, `ItensResumo`, `HH:mm`. Botão `Pronto →` chama `agendaService.avancar(agendaId, "ProntoParaRetirada")`.

---

## 3. Fluxo do Botão Híbrido — Chaveamento de Contexto via React Router DOM (JS)

### 3.1. Visão e Regra

Usuário com `vendor_id` (vendedor aprovado, ver `docs/auth_roles.md`) vê botão no `Header.jsx`. Clique alterna entre interface pública (`/`) e painel (`/vendedor/dashboard`) sem re-login, mantendo o mesmo JWT em cookie `HttpOnly` `__Host-access_token` (enviado via `withCredentials:true`, nunca em `localStorage`).

### 3.2. Detecção da Claim em JavaScript Puro

`src/context/AuthContext.jsx` já expõe `isVendorAprovado`:

```jsx
import { useAuth } from "../context/AuthContext"

export function useIsVendorAprovado() {
  const { isVendorAprovado } = useAuth()
  return isVendorAprovado
}
```

Lógica central usa `user.isVendedor && user.vendorId !== null`. Sem `vendorId`, botão não renderiza mesmo se `role` contém `Vendedor` pendente.

### 3.3. `HybridButton.jsx` — Comportamento Visual e Rota JS

```jsx
import PropTypes from "prop-types"
import { useNavigate, useLocation } from "react-router-dom"
import { useAuth } from "../context/AuthContext"

export function HybridButton() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isVendorAprovado } = useAuth()

  if (!isVendorAprovado) return null

  const estaNoPainel = location.pathname.startsWith("/vendedor")
  const destino = estaNoPainel ? "/" : "/vendedor/dashboard"
  const label = estaNoPainel ? "Voltar para Loja" : "Ir para Painel do Vendedor"

  function handleClick() {
    navigate(destino)
  }

  return (
    <button
      onClick={handleClick}
      className={[
        "inline-flex items-center gap-2 rounded-full px-5 py-2.5 text-sm font-semibold transition-all",
        estaNoPainel ? "bg-white text-primary border border-primary hover:bg-neutral-100" : "bg-primary text-white hover:bg-primary-dark shadow-md hover:shadow-lg",
      ].join(" ")}
    >
      <span>{estaNoPainel ? "🏪" : "📊"}</span>
      {label}
    </button>
  )
}
```

**Uso no `Header.jsx`:**

```jsx
import { useAuth } from "../context/AuthContext"
import { HybridButton } from "./HybridButton"

export function Header() {
  const { user } = useAuth()
  return (
    <header className="sticky top-0 z-40 bg-white border-b border-neutral-200">
      <div className="mx-auto max-w-7xl px-4 h-16 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <img src="/logo-saboriza.svg" alt="Saboriza" className="h-8" />
          <span className="font-display font-bold text-primary text-xl">Saboriza</span>
        </div>
        <nav className="flex items-center gap-3">
          <HybridButton />
          {user ? <span className="text-sm">Olá, {user.nome}</span> : <a href="/login" className="text-primary font-semibold">Entrar</a>}
        </nav>
      </div>
    </header>
  )
}
```

**Transição suave via React Router DOM:**

- `AppShell.jsx` envolve `Outlet` com `transition` CSS `duration-200` ou `Framer Motion` se instalado.
- `HybridButton` inverte variante com `transition-colors duration-200`.
- `ProtectedRoute.jsx` valida `isVendorAprovado` antes de renderizar `/vendedor/*`. Se `vendorId` ausente (pendente), redireciona para `/` com `UiContext` toast "Sua loja está em aprovação".

### 3.4. Fluxo de Upgrade e Reemissão (JS)

1. Cliente clica "Quero vender" → `POST /api/vendedor/solicitar-upgrade` → status `PendenteAprovacao`, sem `vendor_id`, `HybridButton` não aparece.
2. Admin aprova → frontend chama `POST /api/auth/refresh` com `refreshToken` do `AuthContext`. Novo JWT contém `vendor_id`.
3. `setSession({ accessToken, refreshToken })` atualiza `user` → `isVendorAprovado` vira true → `HybridButton` aparece por re-render do Context.
4. Primeiro clique leva a `/vendedor/dashboard` com onboarding.

### 3.5. Validação de PropTypes no Botão

```jsx
HybridButton.propTypes = {}
```

`Header.jsx` e `HybridButton.jsx` não recebem props externas, apenas consomem Context, por isso `propTypes` vazio. Para componentes com props, sempre declarar `PropTypes` conforme exemplos de `CategoryCard.jsx` e `StoreCard.jsx`.

---

## 4. Acessibilidade, Responsividade e Performance (JS)

- **Contraste:** todos os pares validados com `axe-core` → AA mínimo.
- **Teclado:** `CategoryCard` e `ProductCard` focáveis com `focus-visible:ring-2 ring-primary`.
- **Responsivo:** breakpoints Tailwind `sm 640`, `md 768`, `lg 1024`, `xl 1280`. Grids e sidebars colapsam conforme descrito.
- **Performance:** `React.lazy` + `Suspense` para `DashboardPage` e `LojaPage` em `.jsx`, `Skeleton`, imagens `loading="lazy"`.

---

## 5. Checklist UI Spec (JavaScript Puro)

- [ ] Configurar `tailwind.config.js` com paleta Saboriza `#C1272D/#F8BBD0/#F2C14E` e `globals.css` com variáveis
- [ ] Implementar `Header.jsx` + `HybridButton.jsx` com `useAuth().isVendorAprovado` e navegação `useNavigate` do React Router DOM
- [ ] Criar `CategoryGrid.jsx` 8 col desktop com PropTypes e navegação `navigate(/categoria/slug)`
- [ ] Criar layout GerencieAqui 3 colunas com `DepartmentSidebar.jsx` sticky, `ProductGrid.jsx` e `CartSidebar.jsx` via `CartContext`
- [ ] Criar `CartSidebar.jsx` com `QuantityStepper.jsx`, `total` tempo real e `FINALIZAR COMPRA` validado com JS puro
- [ ] Criar `Dashboard` com 4 `KpiCard.jsx` semânticos + `RevenueLineChart.jsx` + `SalesPieChart.jsx` + `AgendaKanban.jsx`
- [ ] Adicionar `PropTypes` em todos os componentes `.jsx` e validar `validarCep/validarPreco` com mensagens vermelhas
- [ ] Validar contraste WCAG AA e navegação por teclado em todos os fluxos
- [ ] Garantir nenhum `.ts/.tsx` no projeto e nenhum `//` nos `.js/.jsx`
