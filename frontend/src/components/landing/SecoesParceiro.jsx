import PropTypes from "prop-types"
import { useNavigate } from "react-router-dom"
import { Button } from "../ui/Button"
import { Icon } from "../ui/Icon"

const vantagens = [
  {
    icone: "store",
    titulo: "Vitrine por proximidade",
    texto: "Sua loja aparece para clientes a até 50 km, ordenada por distância com busca por CEP.",
  },
  {
    icone: "chart",
    titulo: "Gestão simples e fácil",
    texto: "Painel do vendedor com KPIs, agenda de produção cronológica e controle de estoque.",
  },
  {
    icone: "utensils",
    titulo: "Cardápio artesanal",
    texto: "Cadastre produtos por categoria com fotos. A moderação garante segurança alimentar.",
  },
  {
    icone: "check",
    titulo: "Comissão única de 8,5%",
    texto: "Sem taxa de adesão na demo. Pagamento via PIX fictício com repasse líquido automático.",
  },
]

const itensPlano = [
  "Vitrine ordenada por distância",
  "Agenda de produção do dia",
  "Controle de estoque com concorrência",
  "PIX fictício com cálculo de comissão",
  "Relatórios de faturamento",
]

const passos = [
  {
    numero: "1",
    titulo: "Crie a conta e cadastre sua loja",
    texto: "Registre-se, informe nome da loja, categoria, endereço comercial e CEP. Leva minutos.",
  },
  {
    numero: "2",
    titulo: "Aguarde a aprovação do admin",
    texto: "Nossa moderação valida a loja e o cardápio. Você recebe o acesso ao painel após aprovar.",
  },
  {
    numero: "3",
    titulo: "Configure o cardápio e receba pedidos",
    texto: "Cadastre produtos, organize a agenda por horário de aprovação e avance até a retirada.",
  },
]

const perguntas = [
  {
    pergunta: "Quanto custa vender na Saboriza?",
    resposta: "Na demonstração não há mensalidade. A comissão padrão é de 8,5% sobre o valor bruto de cada pedido finalizado, calculada automaticamente no pagamento.",
  },
  {
    pergunta: "Preciso de CNPJ para cadastrar minha loja?",
    resposta: "Não. O CNPJ é opcional no cadastro, atendendo MEIs e produtores pessoa física. Informe apenas nome da loja, categoria, endereço e telefone.",
  },
  {
    pergunta: "Como recebo os pedidos?",
    resposta: "Ao aprovar um pedido ele entra na agenda de produção do dia, ordenada pelo horário de aprovação. Você avança cada item até pronto para retirada e finalizado.",
  },
  {
    pergunta: "Quais CEPs funcionam na demonstração?",
    resposta: "A demo usa 5 CEPs de São Paulo sem internet: 01310-100, 01311-000, 04567-000, 05001-000 e 05410-002.",
  },
  {
    pergunta: "O pagamento é real?",
    resposta: "Não. O PIX é fictício e gera um payload de demonstração. Nenhum valor real é movimentado.",
  },
]

export function VantagensParceiro() {
  const navigate = useNavigate()
  return (
    <section aria-label="Vantagens de ser uma loja parceira" className="mx-auto max-w-7xl px-4 py-12">
      <h2 className="text-2xl lg:text-3xl font-bold text-center">Vantagens de ser uma loja parceira</h2>
      <div className="mt-8 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {vantagens.map((v) => (
          <div key={v.titulo} className="bg-white rounded-2xl border border-neutral-200 shadow-card p-6 flex flex-col items-center text-center gap-3">
            <span className="bg-primary/10 text-primary rounded-full p-3">
              <Icon name={v.icone} className="h-6 w-6" />
            </span>
            <h3 className="font-bold">{v.titulo}</h3>
            <p className="text-sm text-neutral-600">{v.texto}</p>
          </div>
        ))}
      </div>
      <div className="mt-8 flex justify-center">
        <Button size="lg" onClick={() => navigate("/vender")}>Cadastrar minha loja</Button>
      </div>
    </section>
  )
}

VantagensParceiro.propTypes = {}

export function PlanoParceiro() {
  const navigate = useNavigate()
  return (
    <section aria-label="Plano Saboriza" className="bg-primary-dark text-white">
      <div className="mx-auto max-w-7xl px-4 py-12">
        <h2 className="text-2xl lg:text-3xl font-bold text-center">Conheça nosso plano</h2>
        <div className="mt-8 flex justify-center">
          <div className="bg-white text-neutral-900 rounded-2xl shadow-card p-6 lg:p-8 w-full max-w-md">
            <div className="flex items-center justify-between">
              <h3 className="font-display text-xl font-bold text-primary">Saboriza</h3>
              <span className="bg-accent text-[#3A2A00] text-xs font-bold rounded-full px-3 py-1">Plano único</span>
            </div>
            <p className="mt-3 font-display text-4xl font-bold">
              8,5% <span className="text-base font-sans font-normal text-neutral-600">por pedido finalizado</span>
            </p>
            <ul className="mt-5 flex flex-col gap-2 text-sm">
              {itensPlano.map((item) => (
                <li key={item} className="flex items-center gap-2">
                  <span className="text-success"><Icon name="check" className="h-4 w-4" /></span>
                  {item}
                </li>
              ))}
            </ul>
            <Button fullWidth size="lg" className="mt-6" onClick={() => navigate("/vender")}>Quero aproveitar</Button>
          </div>
        </div>
      </div>
    </section>
  )
}

PlanoParceiro.propTypes = {}

export function PassosAbertura() {
  return (
    <section aria-label="Como abrir uma loja na Saboriza" className="mx-auto max-w-7xl px-4 py-12">
      <h2 className="text-2xl lg:text-3xl font-bold text-center">Como abrir uma loja na Saboriza?</h2>
      <div className="mt-8 grid grid-cols-1 md:grid-cols-3 gap-4">
        {passos.map((p) => (
          <div key={p.numero} className="bg-white rounded-2xl border border-neutral-200 shadow-card p-6 text-center">
            <span className="mx-auto bg-secondary text-[#5A1A1F] font-display font-bold rounded-full h-12 w-12 flex items-center justify-center text-xl">
              {p.numero}
            </span>
            <h3 className="font-bold mt-4">{p.titulo}</h3>
            <p className="text-sm text-neutral-600 mt-2">{p.texto}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

PassosAbertura.propTypes = {}

export function PerguntasFrequentes() {
  return (
    <section aria-label="Perguntas frequentes" className="bg-neutral-100 border-t border-neutral-200">
      <div className="mx-auto max-w-3xl px-4 py-12">
        <h2 className="text-2xl lg:text-3xl font-bold text-center">Perguntas frequentes</h2>
        <div className="mt-8 flex flex-col gap-3">
          {perguntas.map((p) => (
            <details key={p.pergunta} className="bg-white rounded-xl border border-neutral-200 px-5 py-4 group">
              <summary className="font-semibold cursor-pointer list-none flex items-center justify-between gap-4">
                {p.pergunta}
                <span className="text-primary font-bold group-open:rotate-180 transition-transform">⌄</span>
              </summary>
              <p className="text-sm text-neutral-600 mt-3">{p.resposta}</p>
            </details>
          ))}
        </div>
      </div>
    </section>
  )
}

PerguntasFrequentes.propTypes = {}
