namespace GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;

using FluentValidation;

public sealed class AprovarPedidoCommandValidator : AbstractValidator<AprovarPedidoCommand>
{
    public AprovarPedidoCommandValidator()
    {
        RuleFor(x => x.PedidoId)
            .NotEmpty().WithMessage("PedidoId é obrigatório.")
            .Must(id => id != Guid.Empty).WithMessage("PedidoId inválido.");
        RuleFor(x => x.AtorUsuarioId)
            .NotEmpty().WithMessage("Usuário autenticado é obrigatório.");
        RuleFor(x => x)
            .Must(cmd => cmd.IsAdmin || cmd.AtorVendorId.HasValue)
            .WithMessage("Apenas vendedores ou administradores podem aprovar pedidos.")
            .WithName("Autorização");
    }
}
