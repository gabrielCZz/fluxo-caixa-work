using FluxoCaixa.Domain;

namespace FluxoCaixa.Api;

public sealed record GrupoRequest(string Nome, TipoLancamento Tipo, int Ordem, bool Ativo);
public sealed record SubgrupoRequest(Guid GrupoId, string Nome, int Ordem, bool Ativo);
public sealed record RegraClassificacaoRequest(string MatchContraparte, ModoMatch Modo, string? MatchDescricaoKeywords, TipoLancamento? MatchTipoLancamento, Guid GrupoId, Guid SubgrupoId, int Prioridade, bool Ativa);
public sealed record LancamentoRequest(TipoLancamento Tipo, DateOnly DataVencimentoOriginal, string ContraparteNome, string? ContraparteDocumento, string? Descricao, decimal Valor, Guid? GrupoId, Guid? SubgrupoId, StatusLancamento Status, OrigemLancamento Origem, Guid? ImportacaoId);
public sealed record LancamentoClassificacaoRequest(Guid GrupoId, Guid SubgrupoId);
public sealed record SaldoInicialRequest(decimal Valor, string DefinidoPor);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LoginRequestApi(string Email, string Password);
