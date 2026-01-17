using FluentValidation;

namespace FluxoCaixa.Api;

public sealed class GrupoRequestValidator : AbstractValidator<GrupoRequest>
{
    public GrupoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
    }
}

public sealed class SubgrupoRequestValidator : AbstractValidator<SubgrupoRequest>
{
    public SubgrupoRequestValidator()
    {
        RuleFor(x => x.GrupoId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
    }
}

public sealed class RegraClassificacaoRequestValidator : AbstractValidator<RegraClassificacaoRequest>
{
    public RegraClassificacaoRequestValidator()
    {
        RuleFor(x => x.MatchContraparte).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Prioridade).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GrupoId).NotEmpty();
        RuleFor(x => x.SubgrupoId).NotEmpty();
    }
}

public sealed class LancamentoRequestValidator : AbstractValidator<LancamentoRequest>
{
    public LancamentoRequestValidator()
    {
        RuleFor(x => x.ContraparteNome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Valor).GreaterThan(0);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequestApi>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
