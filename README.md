# upkeep-api

Backend do aplicativo Upkeep. API REST em C# / .NET 8 com autenticação JWT e banco de dados PostgreSQL via Supabase.

## Tecnologias

- .NET 8 / ASP.NET Core
- Entity Framework Core + Npgsql (PostgreSQL / Supabase)
- JWT Authentication
- BCrypt para hashing de senhas
- Swagger UI com Swashbuckle Annotations
- Rate Limiting nativo do .NET 8

## Estrutura

```
Controllers/    → Rotas HTTP com annotations Swagger
DTOs/           → Contratos de entrada e saída (Auth/, User/)
Mappers/        → Extensões de mapeamento Model → DTO
Models/         → Entidades do domínio (herdam de BaseEntity)
Services/       → Regras de negócio (Interfaces/ + implementações)
Data/           → AppDbContext com timestamps automáticos
```

## Configuração

### 1. Clonar e instalar dependências

```bash
git clone <repo-url>
cd upkeep-api
dotnet restore
```

### 2. Configurar variáveis de ambiente

Copie o arquivo de exemplo e preencha com suas credenciais do Supabase:

```bash
cp .env.example .env
```

Edite o `.env`:

```env
# Supabase Dashboard > Settings > Database
ConnectionStrings__DefaultConnection='Host=db.xxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true'

# Chave secreta JWT (mínimo 32 caracteres)
Jwt__SecretKey=sua-chave-secreta
Jwt__Issuer=upkeep-api
Jwt__Audience=upkeep-app
Jwt__ExpirationInHours=24
```

> Senhas com caracteres especiais (`$`, `!`) devem estar entre aspas simples.

### 3. Aplicar migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Executar

```bash
dotnet run
```

A API estará disponível em `https://localhost:PORT`. O Swagger UI em `/swagger`.

## Rotas

### Auth — `POST /auth/*`

| Método | Rota | Descrição |
|---|---|---|
| POST | `/auth/register` | Cadastrar novo usuário |
| POST | `/auth/login` | Autenticar e obter token JWT |

### Users — `[Authorize]`

| Método | Rota | Descrição |
|---|---|---|
| GET | `/users/me` | Obter dados do usuário autenticado |
| PUT | `/users/me` | Atualizar nome e e-mail |
| PATCH | `/users/me/password` | Alterar senha |
| DELETE | `/users/me` | Excluir conta |

### Health

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Status da API e conectividade com o banco |

## Rate Limiting

| Política | Limite | Aplicado em |
|---|---|---|
| Global | 100 req/min por IP | Todas as rotas |
| `auth` | 10 req/min por IP | `/auth/*` e `/health` |

Requisições que excedem o limite retornam `429 Too Many Requests`.
