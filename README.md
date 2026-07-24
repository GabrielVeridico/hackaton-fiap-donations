# HackatonFiap.Donations (DonationAPI)

Microsserviço **central** da plataforma **Conexão Solidária** (Hackathon FIAP PosTech). Concentra três frentes: **Campanhas** (CRUD + ciclo de vida), **Doações/Arrecadação** (saga coreografada com a PaymentAPI + consumer idempotente que consolida o valor arrecadado) e **Transparência** (leitura pública servida **só** do read model em Cosmos DB).

> **Ecossistema (6 repos):** `donations` (este) · `users` · `payments` · `notifications` · `front` · `orchestration`. Mapa completo no [orchestration](https://github.com/GabrielVeridico/hackaton-fiap-orchestration#-ecossistema).

- **.NET 8** / ASP.NET Core · **Clean Architecture** (Domain/Application/Infrastructure/API)
- **CQRS** manual (handlers + `Result<T>`, sem MediatR)
- **Persistência CQRS:** **EF Core 8 + SQL Server** (escrita, `HackatonFiapDonationsDb`) + **Cosmos DB** (read model do painel; fallback in-memory em Development)
- **Azure Service Bus** — publisher + consumer (`PaymentResultConsumer`, BackgroundService) + `CampaignExpirationWorker`
- **Serilog** + **OpenTelemetry** (métricas → `/metrics` Prometheus)
- Testes: **xUnit + NSubstitute + FluentAssertions** (34 métodos de teste)

## Endpoints

### Campanhas (`GestorONG`/Owner)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/campaigns` | cria (status inicial `Active`) → 201 / 400 (meta≤0, data-fim no passado) / 403 |
| PUT | `/api/campaigns/{id}` | edita (não altera `amountRaised`) |
| PATCH | `/api/campaigns/{id}/status` | encerra manualmente / cancela (`action`: `0`=Close, `1`=Cancel) |
| GET | `/api/campaigns` · `/api/campaigns/{id}` | lista / detalha |

### Doações (`Doador`)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/donations` | intenção de doação → **202** (grava `Pending`, publica evento) · 400 (amount≤0) · 422 (campanha inexistente/encerrada/fora do período) |
| GET | `/api/donations/{id}` | status da própria doação (compara `DonorId` das claims) |

### Transparência (público)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/transparency/campaigns` | lista só campanhas `Active` (título, meta, arrecadado, %) — lê **só do Cosmos** |

Observabilidade: `/health`, `/ready` (checa SQL), `/metrics` (`donations_received_total`, `donations_approved_total`, `donations_declined_total`, `campaigns_completed_total`, `amount_raised_total`).

## Saga de doação (Service Bus, pub/sub)

```
POST /api/donations → grava Doacao=Pending (SQL) → publica DonationRequested (tópico donation-requested)
                                                  → 202 { donationId, "Pending" }
tópico payment-result / subscription "donations":
  PaymentResultConsumer roteia por Subject:
    PaymentApproved → consolida (uma transação SQL): Donation.Approve() + Campaign.AddRaised()
                       (se atingir a meta → Complete(GoalReached)) + grava ProcessedEvent
                       → após o commit, projeta a campanha no Cosmos (read model)
    PaymentDeclined → Donation.Decline() (não credita)
```

- **Idempotência (RN06.10):** inbox `ProcessedEvent` (único por `DonationId`) — reprocessar não recredita.
- **`CampaignExpirationWorker`** (BackgroundService): campanhas `Active` vencidas → `Complete(Expired)` + projeta no Cosmos.
- **Enums:** request em **inteiro** (`paymentMethod` 0=Pix/1=CreditCard/2=BankTransfer; `action` 0=Close/1=Cancel); response em **string** (status `Pending/Approved/Declined`, `Active/Completed/Cancelled`).

## Como rodar localmente

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

export ConnectionStrings__Default="Server=localhost,1433;Database=HackatonFiapDonationsDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;"
export ASPNETCORE_ENVIRONMENT=Development
# Sem ServiceBus:ConnectionString → consumer NoOp; sem Cosmos:ConnectionString → read store in-memory.

dotnet build HackatonFiap.Donations.sln -c Release
dotnet test  HackatonFiap.Donations.sln -c Release
dotnet run --project src/HackatonFiap.Donations.API
```

Requisições de exemplo em `src/HackatonFiap.Donations.API/*.http`. Ambiente completo (3 serviços + saga) via `hackaton-fiap-orchestration/local` (inclui um smoke test da saga).

### Docker
```bash
docker build -t hackatonfiap-donations:local .
```

## Configuração
| Chave | Env var | Observação |
|-------|---------|-----------|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | SQL Server (obrigatório fora de Development) |
| `ServiceBus:ConnectionString` | `ServiceBus__ConnectionString` | ausente em Development → NoOp |
| `Cosmos:ConnectionString` | `Cosmos__ConnectionString` | ausente em Development → read store in-memory |
| `Jwt:*` | `Jwt__*` | mesmo emissor/audience do ecossistema |

Segredos via **Key Vault** (CSI + Workload Identity) no AKS; nada commitado.

## CI/CD
`.github/workflows/ci-cd.yaml`: push/PR na `main` → `dotnet build` + `dotnet test` + **build da imagem Docker** (sempre). Deploy no AKS **opcional/gated** por `vars.DEPLOY_TO_AKS == 'true'` (push ACR + `kubectl set image` no namespace `conexao-solidaria`).

## Arquitetura
```
src/
├── HackatonFiap.Donations.Domain          # Campaign, Donation, ProcessedEvent, VOs, Result<T>
├── HackatonFiap.Donations.Application      # CQRS handlers, ICampaignReadStore, eventos, métricas
├── HackatonFiap.Donations.Infrastructure   # EF Core, Service Bus, Cosmos/InMemory read store, workers
└── HackatonFiap.Donations.API              # controllers, Program.cs
tests/
└── HackatonFiap.Donations.UnitTests
```
