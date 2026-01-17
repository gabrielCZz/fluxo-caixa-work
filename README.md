# Fluxo de Caixa

Sistema completo de controle de fluxo de caixa com backend em .NET 8, frontend em Blazor WebAssembly, JWT, SQL Server, MongoDB, Redis e logs no Elasticsearch.

## Stack
- Backend: ASP.NET Core Web API (.NET 8)
- Frontend: Blazor WebAssembly
- Auth: JWT + Refresh Token
- Logs: Serilog + Elasticsearch + Console
- Dados: SQL Server (relacional), MongoDB (linhas brutas da importação)
- Cache: Redis (cache de matriz do fluxo mensal)

## Como rodar
1. Suba dependências:
```bash
docker compose -f docker/docker-compose.yml up -d
```

2. Configure connection strings em `src/FluxoCaixa.Api/appsettings.json` se necessário.

3. Rodar API:
```bash
dotnet run --project src/FluxoCaixa.Api
```

4. Rodar Blazor WASM:
```bash
dotnet run --project src/FluxoCaixa.Blazor
```

## Migrations
```bash
dotnet ef migrations add Initial --project src/FluxoCaixa.Infrastructure --startup-project src/FluxoCaixa.Api
```

```bash
dotnet ef database update --project src/FluxoCaixa.Infrastructure --startup-project src/FluxoCaixa.Api
```

## Usuário admin seed
- Email: `admin@fluxocaixa.local`
- Senha: `Admin@123`

## Formato de importação
CSV ou XLSX com colunas:
- `DataVencimento`
- `Contraparte`
- `Valor`
- `Tipo` (Entrada/Saida)
- `Descricao` (opcional)
- `Documento` (opcional)
- `Status` (Previsto/Realizado, opcional)

## Decisões importantes
- MongoDB guarda as linhas brutas da importação e erros de parsing para rastreabilidade sem inflar o SQL.
- Redis é usado para cache do resultado da matriz mensal (`/api/fluxo`) por 10 minutos. A invalidação deve ser feita ao alterar lançamentos, regras ou saldos.
- Duplicidades: lançamentos duplicados são importados com flag `Duplicado = true` e não são bloqueados.

## Kibana
- Acesse `http://localhost:5601` e configure o index pattern `fluxocaixa-logs-*`.

## Exemplos de chamadas
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@fluxocaixa.local","password":"Admin@123"}'
```

```bash
curl -X GET "https://localhost:5001/api/fluxo?periodo=2024-05&modo=ambos" \
  -H "Authorization: Bearer <token>"
```
