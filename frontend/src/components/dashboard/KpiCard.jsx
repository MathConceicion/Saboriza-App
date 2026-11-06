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
