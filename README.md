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
│   ├── DTOs/               → Contratos de entrada e saída (Auth/, User/)
│   ├── Mappers/            → Extensões de mapeamento Model → DTO
│   ├── Models/             → Entidades do domínio (users, habits, routines)
│   ├── Services/           → Regras de negócio (Interfaces/ + implementações)
│   ├── Data/               → AppDbContext com timestamps automáticos
│   └── Migrations/         → Migrations do EF Core
└── tests/UpkeepAPI.Tests/  → Testes de integração
    ├── Fixtures/           → ApiFactory (WebApplicationFactory + Testcontainers)
    └── Integration/        → Suítes por endpoint (Health, Auth, Users)
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
Jwt__ExpirationInHours=24
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
| POST | `/auth/register` | — | Cadastrar novo usuário |
| POST | `/auth/login` | — | Autenticar e obter token JWT |

### Users

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/users/me` | JWT | Obter dados do usuário autenticado |
| PUT | `/users/me` | JWT | Atualizar nome e e-mail |
| PATCH | `/users/me/password` | JWT | Alterar senha |
| DELETE | `/users/me` | JWT | Excluir conta |

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