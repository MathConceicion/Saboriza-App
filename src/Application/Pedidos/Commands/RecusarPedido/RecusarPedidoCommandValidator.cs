namespace GastronomiaMarketplace.Application.Pedidos.Commands.RecusarPedido;

using FluentValidation;

public sealed class RecusarPedidoCommandValidator : AbstractValidator<RecusarPedidoCommand>
{
    public RecusarPedidoCommandValidator()
    {
        RuleFor(x => x.PedidoId)
            .NotEmpty().WithMessage("PedidoId é obrigatório.")
            .Must(id => id != Guid.Empty).WithMessage("PedidoId inválido.");
        RuleFor(x => x.AtorUsuarioId)
            .NotEmpty().WithMessage("Usuário autenticado é obrigatório.");
        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("Motivo deve ter no máximo 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Motivo));
        RuleFor(x => x)
            .Must(cmd => cmd.IsAdmin || cmd.AtorVendorId.HasValue)
            .WithMessage("Apenas vendedores ou administradores podem recusar pedidos.")
            .WithName("Autorização");
    }
}
