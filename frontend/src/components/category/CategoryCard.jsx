import PropTypes from "prop-types"
import { tratarErroImagemComFallback } from "../../lib/imagens"

export function CategoryCard({ nome, slug, iconeUrl, reservaUrl, ativa, onSelect }) {
  return (
    <button
      onClick={() => onSelect(slug)}
      className={[
        "flex flex-col items-center gap-2 p-2 rounded-2xl transition-all",
        ativa ? "bg-primary text-white shadow-md scale-105" : "bg-white hover:shadow-md hover:-translate-y-0.5",
      ].join(" ")}
      aria-label={`Ver lojas de ${nome}`}
    >
      <img
        src={iconeUrl}
        data-fallback={reservaUrl}
        onError={tratarErroImagemComFallback}
        alt={nome}
        loading="lazy"
        className="h-20 w-20 rounded-full object-cover border-4 border-secondary"
      />
      <span className="text-sm font-medium">{nome}</span>
    </button>
  )
}

CategoryCard.propTypes = {
  nome: PropTypes.string.isRequired,
  slug: PropTypes.string.isRequired,
  iconeUrl: PropTypes.string.isRequired,
  reservaUrl: PropTypes.string,
  ativa: PropTypes.bool,
  onSelect: PropTypes.func.isRequired,
}

export function CategoryGrid({ categorias, categoriaAtiva, onSelectCategoria }) {
  return (
    <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-4">
      {categorias.map((c) => (
        <CategoryCard key={c.slug} nome={c.nome} slug={c.slug} iconeUrl={c.iconeUrl} reservaUrl={c.reservaUrl} ativa={c.slug === categoriaAtiva} onSelect={onSelectCategoria} />
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
      reservaUrl: PropTypes.string,
    })
  ).isRequired,
  categoriaAtiva: PropTypes.string,
  onSelectCategoria: PropTypes.func.isRequired,
}
