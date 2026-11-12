import { Link } from "react-router-dom"

const colunas = [
  {
    titulo: "Comprar",
    links: [
      { to: "/", rotulo: "Início" },
      { to: "/buscar", rotulo: "Pedir" },
      { to: "/login", rotulo: "Entrar" },
      { to: "/registro", rotulo: "Cadastrar" },
    ],
  },
  {
    titulo: "Vender",
    links: [
      { to: "/vender", rotulo: "Quero vender" },
      { to: "/vendedor/dashboard", rotulo: "Painel do vendedor" },
    ],
  },
  {
    titulo: "Conta",
    links: [
      { to: "/conta", rotulo: "Minha conta" },
      { to: "/checkout", rotulo: "Meu carrinho" },
    ],
  },
]

export function Footer() {
  return (
    <footer className="mt-8 border-t border-neutral-200 bg-white sm:mt-12">
      <div className="mx-auto grid max-w-7xl grid-cols-3 gap-x-5 gap-y-8 px-4 py-8 sm:px-6 sm:py-10 md:grid-cols-5 md:gap-8 lg:px-12">
        <div className="col-span-3 min-w-0 md:col-span-2">
          <Link to="/" className="flex items-center gap-2">
            <img src="/favicon.ico" alt="Logo Saboriza" className="h-8 w-8 rounded-full object-cover" />
            <span className="font-display text-xl font-bold text-primary">Saboriza</span>
          </Link>
          <p className="mt-3 max-w-xs text-sm text-neutral-600">Marketplace de gastronomia artesanal. Descubra sabores perto de você.</p>
        </div>
        {colunas.map((col) => (
          <nav key={col.titulo} aria-label={col.titulo} className="min-w-0">
            <h3 className="mb-3 text-sm font-bold text-neutral-900">{col.titulo}</h3>
            <ul className="flex flex-col gap-2">
              {col.links.map((l) => (
                <li key={l.to + l.rotulo}>
                  <Link to={l.to} className="block break-words text-sm leading-5 text-neutral-600 hover:text-primary">{l.rotulo}</Link>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </div>
      <div className="border-t border-neutral-200">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-2 px-4 py-4 text-center sm:flex-row sm:px-6 sm:text-left lg:px-12">
          <span className="text-xs text-neutral-500">© 2026 Saboriza. Todos os direitos reservados.</span>
          <span className="text-xs text-neutral-500">Pagamentos simulados para demonstração.</span>
        </div>
      </div>
    </footer>
  )
}
