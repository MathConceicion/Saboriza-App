import PropTypes from "prop-types"

export function Skeleton({ lines = 3, variant = "text" }) {
  if (variant === "circle") {
    return (
      <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-4" aria-label="Carregando">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="flex flex-col items-center gap-2">
            <div className="h-20 w-20 rounded-full bg-neutral-200 animate-pulse" />
            <div className="h-3 w-16 rounded bg-neutral-200 animate-pulse" />
          </div>
        ))}
      </div>
    )
  }
  if (variant === "card") {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4" aria-label="Carregando">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="bg-white rounded-xl border p-4">
            <div className="h-32 w-full rounded-lg bg-neutral-200 animate-pulse" />
            <div className="h-4 w-2/3 rounded bg-neutral-200 animate-pulse mt-3" />
            <div className="h-3 w-1/2 rounded bg-neutral-200 animate-pulse mt-2" />
          </div>
        ))}
      </div>
    )
  }
  return (
    <div className="flex flex-col gap-2" aria-label="Carregando">
      {Array.from({ length: lines }).map((_, i) => (
        <div key={i} className="h-4 w-full rounded bg-neutral-200 animate-pulse" />
      ))}
    </div>
  )
}

Skeleton.propTypes = {
  lines: PropTypes.number,
  variant: PropTypes.oneOf(["text", "card", "circle"]),
}
