namespace GastronomiaMarketplace.Application.Pedidos.Queries.ListarMeusPedidos;

using FluentValidation;

public sealed class ListarMeusPedidosQueryValidator : AbstractValidator<ListarMeusPedidosQuery>
{
    public ListarMeusPedidosQueryValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty().WithMessage("ClienteId é obrigatório.");
    }
}
