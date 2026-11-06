import PropTypes from "prop-types"
import { useRef, useState } from "react"
import { Icon } from "../ui/Icon"

const tiposAceitos = ["image/jpeg", "image/png", "image/webp"]
const tamanhoMaximo = 5 * 1024 * 1024

export function ImagemUpload({ imagemAtual, onArquivo, erro, rotulo }) {
  const inputRef = useRef(null)
  const [arrastando, setArrastando] = useState(false)
  const [vistoria, setVistoria] = useState(null)
  const [erroLocal, setErroLocal] = useState(null)

  function validar(arquivo) {
    if (!tiposAceitos.includes(arquivo.type)) return "Formato inválido. Use JPG, PNG ou WEBP."
    if (arquivo.size > tamanhoMaximo) return "Imagem deve ter até 5MB."
    return null
  }

  function receber(lista) {
    const arquivo = lista?.[0]
    if (!arquivo) return
    const problema = validar(arquivo)
    if (problema) {
      setErroLocal(problema)
      return
    }
    setErroLocal(null)
    if (vistoria) window.URL.revokeObjectURL(vistoria)
    setVistoria(window.URL.createObjectURL(arquivo))
    onArquivo(arquivo)
  }

  function limpar() {
    if (vistoria) window.URL.revokeObjectURL(vistoria)
    setVistoria(null)
    if (inputRef.current) inputRef.current.value = ""
    onArquivo(null)
  }

  const mostra = vistoria ?? imagemAtual

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm font-medium text-neutral-900">{rotulo ?? "Foto do produto"}</span>
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        onDragOver={(e) => { e.preventDefault(); setArrastando(true) }}
        onDragLeave={() => setArrastando(false)}
        onDrop={(e) => { e.preventDefault(); setArrastando(false); receber(e.dataTransfer.files) }}
        className={["flex items-center gap-3 rounded-xl border bg-white p-3 text-left focus:outline-none focus:ring-2 focus:ring-primary", arrastando ? "border-primary ring-2 ring-primary" : "border-neutral-200", erro || erroLocal ? "border-danger" : ""].join(" ")}
      >
        {mostra ? (
          <img src={mostra} alt="Prévia do produto" className="h-16 w-16 rounded-lg object-cover" />
        ) : (
          <span aria-hidden="true" className="flex h-16 w-16 items-center justify-center rounded-lg bg-neutral-100"><Icon name="camera" className="h-8 w-8 text-neutral-400" /></span>
        )}
        <span className="text-sm text-neutral-600">{mostra ? "Trocar foto (clique ou arraste)" : "Arraste a foto ou clique para escolher"}</span>
      </button>
      <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={(e) => receber(e.target.files)} />
      {mostra ? <button type="button" onClick={limpar} className="text-sm font-semibold text-neutral-600 hover:text-danger self-start">Remover foto</button> : null}
      {erroLocal || erro ? <p className="text-danger text-sm mt-1" aria-live="polite">{erroLocal ?? erro}</p> : null}
    </div>
  )
}

ImagemUpload.propTypes = {
  imagemAtual: PropTypes.string,
  onArquivo: PropTypes.func.isRequired,
  erro: PropTypes.string,
  rotulo: PropTypes.string,
}
