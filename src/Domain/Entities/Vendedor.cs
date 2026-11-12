namespace GastronomiaMarketplace.Domain.Entities;

using System.Text.Json;
using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class Vendedor : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public string NomeLoja { get; private set; } = null!;
    public Guid CategoriaLojaId { get; private set; }
    public CategoriaLoja CategoriaLoja { get; private set; } = null!;
    public string EnderecoComercial { get; private set; } = null!;
    public string Cep { get; private set; } = null!;
    public string Telefone { get; private set; } = null!;
    public string? Cnpj { get; private set; }
    public string? FotoUrl { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public StatusVendedor Status { get; private set; } = StatusVendedor.PendenteAprovacao;
    public string? MotivoRejeicao { get; private set; }
    public string? HorarioFuncionamentoJson { get; private set; }
    public string? DiasFuncionamentoJson { get; private set; }
    public bool Aberta { get; private set; }
    public Loja? Loja { get; private set; }

    private Vendedor() { }

    public Vendedor(Guid usuarioId, string nomeLoja, Guid categoriaLojaId, string enderecoComercial, string cep, string telefone, string? cnpj, double latitude, double longitude, string? fotoUrl = null, string? horarioFuncionamentoJson = null, string? diasFuncionamentoJson = null, bool aberta = true)
    {
        if (usuarioId == Guid.Empty) throw new RegraDeNegocioException("UsuarioId obrigatório.");
        if (string.IsNullOrWhiteSpace(nomeLoja)) throw new RegraDeNegocioException("NomeLoja obrigatório.");
        UsuarioId = usuarioId;
        NomeLoja = nomeLoja.Trim();
        CategoriaLojaId = categoriaLojaId;
        EnderecoComercial = enderecoComercial.Trim();
        Cep = cep.Trim();
        Telefone = telefone.Trim();
        Cnpj = cnpj?.Trim();
        FotoUrl = fotoUrl?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Status = StatusVendedor.PendenteAprovacao;
        HorarioFuncionamentoJson = horarioFuncionamentoJson;
        DiasFuncionamentoJson = diasFuncionamentoJson;
        Aberta = aberta;
        if (!string.IsNullOrWhiteSpace(horarioFuncionamentoJson))
            DefinirHorarioFuncionamentoJson(horarioFuncionamentoJson);
    }

    public void Aprovar()
    {
        if (Status == StatusVendedor.Aprovado) throw new RegraDeNegocioException("Vendedor já aprovado.");
        Status = StatusVendedor.Aprovado;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Rejeitar(string motivo)
    {
        Status = StatusVendedor.Rejeitado;
        MotivoRejeicao = motivo;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void DefinirHorarioFuncionamento(string abertura, string fechamento)
    {
        var horario = ValueObjects.HorarioFuncionamento.Parse(abertura, fechamento);
        HorarioFuncionamentoJson = horario.ToJson();
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    private void DefinirHorarioFuncionamentoJson(string json)
    {
        var horario = ValueObjects.HorarioFuncionamento.FromJson(json);
        if (horario is null) throw new RegraDeNegocioException("Horário de funcionamento inválido.");
        HorarioFuncionamentoJson = horario.ToJson();
    }

    public void DefinirDiasFuncionamento(string? diasFuncionamentoJson)
    {
        if (!string.IsNullOrWhiteSpace(diasFuncionamentoJson))
        {
            using var doc = JsonDocument.Parse(diasFuncionamentoJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new RegraDeNegocioException("Dias de funcionamento deve ser uma lista.");
        }
        DiasFuncionamentoJson = diasFuncionamentoJson;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void AlternarStatusAberta()
    {
        Aberta = !Aberta;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void DefinirStatusAberta(bool aberta)
    {
        Aberta = aberta;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
