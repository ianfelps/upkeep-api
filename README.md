# upkeep-api

Backend do aplicativo Upkeep. API REST em C# / .NET 8 com autenticação JWT e banco de dados PostgreSQL via Supabase.

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
│   ├── DTOs/               → Contratos de entrada e saída (Auth/, User/, RoutineEvent/)
│   ├── Mappers/            → Extensões de mapeamento Model → DTO
│   ├── Models/             → Entidades do domínio (users, habits, routines)
│   ├── Services/           → Regras de negócio (Interfaces/ + implementações)
│   ├── Data/               → AppDbContext com timestamps automáticos
│   └── Migrations/         → Migrations do EF Core
└── tests/UpkeepAPI.Tests/  → Testes de integração
    ├── Fixtures/           → ApiFactory (WebApplicationFactory + Testcontainers)
    └── Integration/        → Suítes por endpoint (Health, Auth, Users, RoutineEvents, RefreshToken)
```

## Configuração

### 1. Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (necessário para os testes de integração)
- Conta no [Supabase](https://supabase.com/)

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

# Chave secreta JWT (mínimo 32 caracteres)
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
| PUT | `/users/me` | JWT | Atualizar nome e e-mail |
| PATCH | `/users/me/password` | JWT | Alterar senha |
| DELETE | `/users/me` | JWT | Excluir conta |

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
- **Logout**: `POST /auth/logout` revoga o refresh token do dispositivo. O access token permanece válido até expirar — mantenha-o curto.
- **Dispositivo offline volta online**: o cliente tenta a requisição; se receber 401, chama `/auth/refresh` com o refresh token salvo e reenvia.

#### Sincronização offline-first

O futuro frontend é offline-first: lê/escreve em banco local e sincroniza com a API quando online. Para suportar isso, todos os recursos expõem `createdAt` e `updatedAt` em UTC, e `GET /routine-events?updatedSince=<iso8601>` retorna apenas eventos com `updatedAt > updatedSince` (delta sync). Ids são gerados pelo servidor — o cliente mapeia id local → id remoto ao receber a resposta do POST. Conflitos seguem last-write-wins via `updatedAt`. Deletes atuais são hard; tombstones podem ser adicionados no futuro se a propagação de exclusões entre dispositivos se tornar necessária.

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