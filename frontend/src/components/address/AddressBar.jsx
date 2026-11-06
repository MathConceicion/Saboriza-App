import PropTypes from "prop-types"
import { useState } from "react"
import { Input } from "../ui/Input"
import { Button } from "../ui/Button"

export function AddressBar({ valorInicial = "", erro, carregando, textoAjuda, onBuscar, onLimpar, mostrarLimpar }) {
  const [cep, setCep] = useState(valorInicial)
  function handleSubmit(event) {
    event.preventDefault()
    onBuscar(cep)
  }
  return (
    <div className="bg-white rounded-xl border shadow-sm p-4">
      <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3 sm:items-end">
        <div className="flex-1">
          <Input label="Buscando em" placeholder="Digite seu CEP (ex: 01310-100)" value={cep} onChange={(e) => setCep(e.target.value)} error={erro} inputMode="numeric" />
        </div>
        <Button type="submit" loading={carregando}>Buscar</Button>
        {mostrarLimpar ? <Button type="button" variant="ghost" onClick={() => { setCep(""); onLimpar() }}>Limpar</Button> : null}
      </form>
      {textoAjuda ? <p className="text-sm text-neutral-600 mt-2">{textoAjuda}</p> : null}
    </div>
  )
}

AddressBar.propTypes = {
  valorInicial: PropTypes.string,
  erro: PropTypes.string,
  carregando: PropTypes.bool,
  textoAjuda: PropTypes.string,
  onBuscar: PropTypes.func.isRequired,
  onLimpar: PropTypes.func.isRequired,
  mostrarLimpar: PropTypes.bool,
}
