# Upkeep API

Backend do **Upkeep** — aplicativo de produtividade focado em construção de hábitos e organização da rotina. O Upkeep ajuda o usuário a criar e acompanhar hábitos diários, registrar execuções com XP, visualizar progresso ao longo do tempo e organizar eventos recorrentes de rotina.

A API é construída em C# / .NET 8, com autenticação JWT, banco de dados PostgreSQL via Supabase e suporte a sincronização offline-first.

## Tecnologias

- .NET 8 / ASP.NET Core
- Entity Framework Core + Npgsql (PostgreSQL / Supabase)
- JWT Authentication
- BCrypt para hashing de senhas
- Swagger UI com Swashbuckle Annotations
- Rate Limiting nativo do .NET 8
- DotNetEnv para carregamento de variáveis de ambiente
- xUnit + Testcontainers (testes de integração com Postgres real)

## Estrutura

```
upkeep-api/
├── src/UpkeepAPI/          → Projeto principal da API
│   ├── Controllers/        → Rotas HTTP com annotations Swagger
│   ├── DTOs/               → Contratos de entrada e saída (Auth/, User/, RoutineEvent/, Habit/, HabitLog/, UserProgress/)
│   ├── Mappers/            → Extensões de mapeamento Model → DTO
│   ├── Models/             → Entidades do domínio (users, habits, routines, progress, achievements)
│   ├── Services/           → Regras de negócio (Interfaces/ + implementações)
│   ├── Data/               → AppDbContext com timestamps automáticos
│   └── Migrations/         → Migrations do EF Core
└── tests/UpkeepAPI.Tests/  → Testes de integração
    ├── Fixtures/           → ApiFactory (WebApplicationFactory + Testcontainers)
    └── Integration/        → Suítes por endpoint (Health, Auth, Users, RoutineEvents, RefreshToken, Habits, HabitLogs, UserProgress, Achievements)
```

## Configuração

### 1. Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (necessário para os testes de integração)
- Conta no [Supabase](https://supabase.com/) (ou qualquer PostgreSQL, mas as instruções abaixo assumem Supabase)

### 2. Clonar e restaurar dependências

```bash
git clone <repo-url>
cd upkeep-api
dotnet restore
```

### 3. Configurar variáveis de ambiente

Copie o arquivo de exemplo e preencha com suas credenciais:

```bash
cp .env.example .env
```

Edite o `.env` na raiz do repositório:

```env
# Supabase Dashboard > Settings > Database
ConnectionStrings__DefaultConnection=Host=db.xxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true

# Chave secreta JWT (mínimo 32 bytes UTF-8 — validado no startup)
Jwt__SecretKey=sua-chave-secreta-com-pelo-menos-32-caracteres
Jwt__Issuer=upkeep-api
Jwt__Audience=upkeep-app
Jwt__ExpirationInHours=2
Jwt__RefreshExpirationInDays=60
```

> Valores com `$` no `.env` devem ficar entre aspas simples para evitar interpolação.

### 4. Aplicar migrations

```bash
dotnet ef database update --project src/UpkeepAPI
```

### 5. Executar

```bash
dotnet run --project src/UpkeepAPI
```

Swagger disponível em `/swagger`.

### 6. Desenvolvimento com hot reload

```bash
dotnet watch --project src/UpkeepAPI run
```

## Testes

Requer Docker em execução.

- Testcontainers sobe um container `postgres:16-alpine` por execução de testes.
- O banco é limpo entre cenários pela fixture de integração.
- Em ambiente `Testing`, os limites de rate limiting ficam em `int.MaxValue` para evitar flakiness.

```bash
dotnet test
```

## Rotas

Formato padrão para erros de negócio:

```json
{ "message": "..." }
```

### Auth

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| POST | `/auth/register` | — | Cadastrar novo usuário (retorna access + refresh token) |
| POST | `/auth/login` | — | Autenticar e obter access + refresh token |
| POST | `/auth/refresh` | — | Trocar refresh token por um novo par (access + refresh, com rotação) |
| POST | `/auth/logout` | — | Revogar refresh token do dispositivo atual |

### Users

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/users/me` | JWT | Obter dados do usuário autenticado |
| GET | `/users/me/progress` | JWT | Obter dashboard de progresso (XP, nível, streaks, taxas de conclusão) |
| GET | `/users/me/achievements` | JWT | Listar todas as conquistas (desbloqueadas + bloqueadas) |
| PUT | `/users/me` | JWT | Atualizar nome e e-mail (exige `currentPassword`; troca de e-mail revoga todos os refresh tokens) |
| PATCH | `/users/me/password` | JWT | Alterar senha (exige `currentPassword`; revoga todos os refresh tokens) |
| DELETE | `/users/me` | JWT | Excluir conta (exige `currentPassword` no corpo) |

**Política de senha:** mínimo 8 caracteres, máximo 72 bytes (limite do BCrypt). Aplicada em registro e troca de senha.

### Routine Events

Eventos de calendário vinculados ao usuário autenticado. Suporta dois tipos:

- **Recorrente** (`eventType: "recurring"`): repete nos dias da semana informados em `daysOfWeek` (array de inteiros 0–6, onde 0 = domingo).
- **Único** (`eventType: "once"`): ocorre em uma data específica (`eventDate`, formato `YYYY-MM-DD`).

Exatamente um de `daysOfWeek` ou `eventDate` deve ser informado na criação/atualização.

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/routine-events` | JWT | Listar eventos por intervalo de datas (ver filtros abaixo) |
| GET | `/routine-events/{id}` | JWT | Obter um evento de rotina específico |
| POST | `/routine-events` | JWT | Criar novo evento de rotina |
| PUT | `/routine-events/{id}` | JWT | Atualizar evento de rotina |
| DELETE | `/routine-events/{id}` | JWT | Excluir evento de rotina |

**Filtros do `GET /routine-events`:**

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `from` | `YYYY-MM-DD` | Início do intervalo |
| `to` | `YYYY-MM-DD` | Fim do intervalo |
| `updatedSince` | ISO 8601 UTC | Retorna todos os eventos modificados após essa data (sync delta — ignora `from`/`to`) |

Sem `from`/`to`, retorna os eventos do **dia atual**. Para semana ou mês, o cliente calcula os limites e passa como `from`/`to`. Timestamps são sempre UTC — a conversão para o fuso do usuário é responsabilidade do cliente.

### Autenticação offline-first

O par **access token + refresh token** foi desenhado para uso offline prolongado:

- **Access token (JWT)**: curta duração (padrão 2h). `ClockSkew` de 5 min tolera dessincronização do relógio do dispositivo quando offline.
- **Refresh token**: longa duração (padrão 60 dias). Armazenado no banco como hash SHA-256, com `RevokedAt` para revogação. Use `POST /auth/refresh` para trocar por um novo par — o refresh token antigo é **revogado imediatamente (rotação)**, então tentar reutilizá-lo retorna 401.
- **Detecção de reuso (token family)**: cada sessão tem um `FamilyId`. A rotação preserva o mesmo `FamilyId` e encadeia tokens via `ReplacedByTokenId`. Se um token já revogado **e já rotacionado** for replayado, toda a família é revogada — sinal de que o token foi comprometido. O atacante perde acesso; vítima precisa logar novamente.
- **Troca de senha ou e-mail**: revoga **todos** os refresh tokens ativos do usuário. O dispositivo atual precisa logar novamente.
- **Logout**: `POST /auth/logout` revoga o refresh token do dispositivo. O access token permanece válido até expirar — mantenha-o curto.
- **Dispositivo offline volta online**: o cliente tenta a requisição; se receber 401, chama `/auth/refresh` com o refresh token salvo e reenvia.

#### Sincronização offline-first

O frontend é offline-first: lê/escreve em banco local e sincroniza com a API quando online. Para suportar isso, todos os recursos expõem `createdAt` e `updatedAt` em UTC, e `GET /routine-events?updatedSince=<iso8601>` retorna apenas eventos com `updatedAt > updatedSince` (delta sync). Ids são gerados pelo servidor — o cliente mapeia id local → id remoto ao receber a resposta do POST. Conflitos seguem last-write-wins via `updatedAt`. Deletes atuais são hard; tombstones podem ser adicionados no futuro se a propagação de exclusões entre dispositivos se tornar necessária.

### Habits

Hábitos vinculados ao usuário autenticado. Cada hábito pode ser opcionalmente vinculado a eventos de rotina via `routineEventIds`.

**Frequências (`frequencyType`):** `Daily`, `Weekly`, `Monthly`.

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/habits` | JWT | Listar hábitos do usuário (`?updatedSince=` para delta sync) |
| GET | `/habits/{id}` | JWT | Obter um hábito específico |
| GET | `/habits/heatmap` | JWT | Heatmap de todos os hábitos (`?from=&to=`, padrão últimos 365 dias) |
| POST | `/habits` | JWT | Criar hábito (com `routineEventIds[]` opcional) |
| PUT | `/habits/{id}` | JWT | Atualizar hábito (substitui vínculos de rotina atomicamente) |
| DELETE | `/habits/{id}` | JWT | Excluir hábito (cascata: logs + vínculos) |

**Resposta do `GET /habits/heatmap`:** `[{ date, completedCount, totalHabits }]` — apenas dias com ao menos um log são retornados.

### Habit Logs

Registros de execução de um hábito por data. No máximo um registro por `(habitId, targetDate)`.

**Status (`status`):** `Completed`, `Skipped`, `Missed`.

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/habits/{habitId}/logs` | JWT | Listar logs (`?from=&to=` ou `?updatedSince=`, padrão hoje) |
| GET | `/habits/{habitId}/logs/{logId}` | JWT | Obter um log específico |
| POST | `/habits/{habitId}/logs` | JWT | Registrar execução para uma data |
| PUT | `/habits/{habitId}/logs/{logId}` | JWT | Atualizar status/notas/XP de um log |
| DELETE | `/habits/{habitId}/logs/{logId}` | JWT | Excluir log |

`completedAt` é definido automaticamente ao criar/atualizar para `Completed`, e limpo ao mudar para outro status. Serve como fonte de dados para o heatmap individual do hábito via `?from=&to=`.

### User Progress

Dashboard de progresso do usuário autenticado. Calculado em tempo real a partir dos `HabitLogs` e persistido para consultas futuras.

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/users/me/progress` | JWT | Retornar estatísticas de progresso do usuário |

**Campos retornados:**

| Campo | Descrição |
|---|---|
| `currentLevel` | Nível atual, derivado de XP: `floor(sqrt(totalXP / 50)) + 1` |
| `totalXP` | Soma de `earnedXP` de todos os logs com `Completed` |
| `currentStreak` | Dias consecutivos com ao menos um log `Completed` (encerra hoje ou ontem) |
| `longestStreak` | Maior sequência consecutiva histórica |
| `lastActivity` | Data (meia-noite UTC) do último log `Completed` |
| `totalHabitsActive` | Quantidade de hábitos ativos no momento |
| `totalLogsCompleted` | Total de logs `Completed` de todos os tempos |
| `completionRateLast7Days` | Taxa de conclusão nos últimos 7 dias (0–1, 3 casas decimais) |
| `completionRateLast30Days` | Taxa de conclusão nos últimos 30 dias (0–1, 3 casas decimais) |
| `updatedAt` | Timestamp UTC da última atualização do registro de progresso |

### Achievements

Conquistas predefinidas desbloqueadas automaticamente a cada chamada a `GET /users/me/progress`, com base nas estatísticas do usuário.

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/users/me/achievements` | JWT | Listar todas as conquistas com status de desbloqueio |

**Conquistas disponíveis:**

| Chave | Título | Condição |
|---|---|---|
| `FirstHabit` | Primeiro Hábito | Criou ao menos 1 hábito |
| `FirstLog` | Primeiro Passo | Completou ao menos 1 hábito |
| `Logs10` | Ganhando Ritmo | 10 hábitos completados |
| `Logs50` | Consistente | 50 hábitos completados |
| `Logs100` | Centenário | 100 hábitos completados |
| `Logs500` | Dedicado | 500 hábitos completados |
| `Streak3` | Começando Bem | Sequência de 3 dias |
| `Streak7` | Uma Semana | Sequência de 7 dias |
| `Streak14` | Quinzena | Sequência de 14 dias |
| `Streak30` | Mês Completo | Sequência de 30 dias |
| `Streak100` | Centurião | Sequência de 100 dias |
| `Level5` | Experiente | Nível 5 |
| `Level10` | Veterano | Nível 10 |
| `Level25` | Mestre | Nível 25 |

Cada item retornado inclui `key`, `title`, `description`, `icon` (nome Lucide), `isUnlocked` e `unlockedAt` (UTC, `null` se ainda bloqueada).

### Health

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/health` | — | Status da API e conectividade com o banco |

## Rate Limiting

| Política | Limite | Aplicado em |
|---|---|---|
| Global | 100 req/min por IP | Todas as rotas |
| `auth` | 10 req/min por IP | `/auth/*` e `/health` |

Requisições acima do limite retornam `429 Too Many Requests`.