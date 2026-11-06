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
    <footer className="border-t border-neutral-200 bg-white mt-12">
      <div className="mx-auto max-w-7xl px-4 py-10 grid grid-cols-2 md:grid-cols-5 gap-8">
        <div className="col-span-2">
          <Link to="/" className="flex items-center gap-2">
            <img src="/favicon.ico" alt="Logo Saboriza" className="h-8 w-8 rounded-full object-cover" />
            <span className="font-display font-bold text-primary text-xl">Saboriza</span>
          </Link>
          <p className="text-sm text-neutral-600 mt-3 max-w-xs">Marketplace de gastronomia artesanal. Descubra sabores perto de você.</p>
        </div>
        {colunas.map((col) => (
          <nav key={col.titulo} aria-label={col.titulo}>
            <h3 className="text-sm font-bold text-neutral-900 mb-3">{col.titulo}</h3>
            <ul className="flex flex-col gap-2">
              {col.links.map((l) => (
                <li key={l.to + l.rotulo}>
                  <Link to={l.to} className="text-sm text-neutral-600 hover:text-primary">{l.rotulo}</Link>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </div>
      <div className="border-t border-neutral-200">
        <div className="mx-auto max-w-7xl px-4 py-4 flex flex-col sm:flex-row items-center justify-between gap-2">
          <span className="text-xs text-neutral-500">© 2026 Saboriza. Todos os direitos reservados.</span>
          <span className="text-xs text-neutral-500">Pagamentos simulados para demonstração.</span>
        </div>
      </div>
    </footer>
  )
}
