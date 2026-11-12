import { useEffect, useState } from "react"
import PropTypes from "prop-types"
import { useUi } from "../../context/UiContext"
import { usuariosAdminService } from "../../services/usuariosAdmin.service"
import { Button } from "../../components/ui/Button"
import { Input } from "../../components/ui/Input"
import { Badge } from "../../components/ui/Badge"
import { Modal } from "../../components/ui/Modal"
import { formatDataHora } from "../../lib/formatters"
import { mensagemPorStatus } from "../../lib/apiErrors"

const PAPEL_OPCOES = [
  { valor: "", rotulo: "Todos" },
  { valor: "1", rotulo: "Cliente" },
  { valor: "2", rotulo: "Vendedor" },
  { valor: "3", rotulo: "Admin" },
]

function badgeParaPapel(papel) {
  const map = {
    1: { tone: "neutral", rotulo: "Cliente" },
    2: { tone: "info", rotulo: "Vendedor" },
    3: { tone: "danger", rotulo: "Admin" },
  }
  return map[papel] || { tone: "neutral", rotulo: "—" }
}

export default function AdminUsuariosPage() {
  const { showToast } = useUi()
  const [usuarios, setUsuarios] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [filtros, setFiltros] = useState({ busca: "", papel: "", bloqueado: null, pagina: 1, tamanho: 20 })

  const [modalAberta, setModalAberta] = useState(false)
  const [selecionado, setSelecionado] = useState(null)
  const [acaoModal, setAcaoModal] = useState(null)
  const [motivoBloqueio, setMotivoBloqueio] = useState("")

  async function carregar() {
    setLoading(true)
    try {
      const r = await usuariosAdminService.listar({
        busca: filtros.busca || undefined,
        papel: filtros.papel || undefined,
        bloqueado: filtros.bloqueado,
        pagina: filtros.pagina,
        tamanho: filtros.tamanho,
      })
      setUsuarios(r.items ?? r ?? [])
      setTotal(r.totalCount ?? r.total ?? 0)
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível listar usuários."), "danger")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { carregar() }, [filtros.busca, filtros.papel, filtros.bloqueado, filtros.pagina])

  function handleBloquear(usuario) {
    setSelecionado(usuario)
    setAcaoModal("bloquear")
    setMotivoBloqueio("")
    setModalAberta(true)
  }

  function handleDesbloquear(usuario) {
    setSelecionado(usuario)
    setAcaoModal("desbloquear")
    setModalAberta(true)
  }

  async function executarAcao() {
    if (!selecionado) return
    try {
      if (acaoModal === "bloquear") {
        if (!motivoBloqueio.trim()) {
          showToast("Informe o motivo do bloqueio.", "danger")
          return
        }
        await usuariosAdminService.bloquear(selecionado.id, motivoBloqueio.trim())
        showToast("Usuário bloqueado com sucesso.", "success")
      } else if (acaoModal === "desbloquear") {
        await usuariosAdminService.desbloquear(selecionado.id)
        showToast("Usuário desbloqueado com sucesso.", "success")
      }
      setModalAberta(false)
      carregar()
    } catch (e) {
      showToast(mensagemPorStatus(e, "Não foi possível concluir a ação."), "danger")
    }
  }

  if (loading) return (
    <div className="flex items-center justify-center h-[500px]">
      <div className="h-8 w-48 rounded bg-neutral-200 animate-pulse" />
    </div>
  )

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h1 className="font-display text-2xl font-bold text-neutral-900">Gerenciamento de Usuários</h1>
          <p className="text-neutral-600">Bloqueie, desbloqueie e acompanhe todos os usuários da plataforma.</p>
        </div>
        <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
          <div className="flex-1 sm:w-64">
            <Input
              placeholder="Buscar por nome ou e-mail"
              value={filtros.busca}
              onChange={(e) => setFiltros({ ...filtros, busca: e.target.value, pagina: 1 })}
            />
          </div>
          <div className="flex-1 sm:w-56">
            <select
              value={filtros.papel}
              onChange={(e) => setFiltros({ ...filtros, papel: e.target.value, pagina: 1 })}
              className="h-11 px-3 rounded-xl border border-neutral-200 bg-white text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            >
              {PAPEL_OPCOES.map((o) => (
                <option key={o.valor} value={o.valor}>{o.rotulo}</option>
              ))}
            </select>
          </div>
          <div className="flex-1 sm:w-56">
            <select
              value={filtros.bloqueado === null ? "" : filtros.bloqueado ? "1" : "0"}
              onChange={(e) => setFiltros({ ...filtros, bloqueado: e.target.value === "" ? null : e.target.value === "1", pagina: 1 })}
              className="h-11 px-3 rounded-xl border border-neutral-200 bg-white text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="">Todos</option>
              <option value="1">Bloqueados</option>
              <option value="0">Ativos</option>
            </select>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-neutral-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-neutral-200 text-left text-neutral-600">
              <th className="p-3">Usuário</th>
              <th className="p-3">E-mail</th>
              <th className="p-3">Papel</th>
              <th className="p-3">Status</th>
              <th className="p-3">Cadastro</th>
              <th className="p-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody>
            {usuarios.length === 0 ? (
              <tr>
                <td colSpan={6} className="p-6 text-center text-neutral-500">
                  Nenhum usuário encontrado.
                </td>
              </tr>
            ) : (
              usuarios.map((u) => (
                <tr key={u.id} className="border-b border-neutral-100 hover:bg-neutral-50 transition-colors">
                  <td className="p-3">
                    <div className="font-semibold">{u.nomeCompleto}</div>
                    {u.cpf && <div className="text-xs text-neutral-500">CPF: {u.cpf}</div>}
                  </td>
                  <td className="p-3">{u.email}</td>
                  <td className="p-3"><Badge tone={badgeParaPapel(parseInt(u.papel) || 1).tone}>{badgeParaPapel(parseInt(u.papel) || 1).rotulo}</Badge></td>
                  <td className="p-3">
                    {u.isBloqueado ? (
                      <Badge tone="danger">Bloqueado</Badge>
                    ) : (
                      <Badge tone="success">Ativo</Badge>
                    )}
                  </td>
                  <td className="p-3 text-neutral-600">{formatDataHora(u.criadoEmUtc)}</td>
                  <td className="p-3 text-right">
                    {u.isBloqueado ? (
                      <Button size="sm" variant="success" onClick={() => handleDesbloquear(u)}>Desbloquear</Button>
                    ) : (
                      <Button size="sm" variant="danger" onClick={() => handleBloquear(u)}>Bloquear</Button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {usuarios.length > 0 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-neutral-600">Exibindo {usuarios.length} de {total} usuários</p>
          <div className="flex gap-2">
            <Button size="sm" disabled={filtros.pagina === 1} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina - 1 })}>
              Anterior
            </Button>
            <Button size="sm" disabled={usuarios.length < filtros.tamanho} onClick={() => setFiltros({ ...filtros, pagina: filtros.pagina + 1 })}>
              Próxima
            </Button>
          </div>
        </div>
      )}

      <Modal open={modalAberta} onClose={() => setModalAberta(false)} title={acaoModal === "bloquear" ? "Bloquear Usuário" : "Desbloquear Usuário"}>
        {acaoModal === "bloquear" && (
          <>
            <p className="mb-4">Tem certeza que deseja bloquear o usuário <strong>{selecionado?.nomeCompleto}</strong>?</p>
            <Input label="Motivo do bloqueio" value={motivoBloqueio} onChange={(e) => setMotivoBloqueio(e.target.value)} required />
            <div className="flex gap-2 mt-4">
              <Button variant="ghost" onClick={() => setModalAberta(false)}>Cancelar</Button>
              <Button variant="danger" disabled={!motivoBloqueio.trim()} onClick={executarAcao}>Confirmar Bloqueio</Button>
            </div>
          </>
        )}
        {acaoModal === "desbloquear" && (
          <>
            <p className="mb-4">Tem certeza que deseja desbloquear o usuário <strong>{selecionado?.nomeCompleto}</strong>?</p>
            <div className="flex gap-2 mt-4">
              <Button variant="ghost" onClick={() => setModalAberta(false)}>Cancelar</Button>
              <Button variant="success" onClick={executarAcao}>Confirmar Desbloqueio</Button>
            </div>
          </>
        )}
      </Modal>
    </div>
  )
}

AdminUsuariosPage.propTypes = {}
