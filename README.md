# Sistema de Conciliação Financeira - Clínica Odontológica RIS.O.S

Sistema web para gestão de pagamentos e conciliação financeira de uma clínica odontológica com duas unidades.

## Tecnologias

- ASP.NET Core 8 MVC
- Entity Framework Core
- PostgreSQL (Npgsql)
- ASP.NET Core Identity
- Bootstrap 5

## Funcionalidades

- **Login**: Autenticação com roles (Admin/Recepção)
- **Gestão de Unidades**: CRUD para Admin
- **Gestão de Usuários**: Criação e edição de usuários com roles
- **Configuração de Taxas**: Formas de pagamento, bandeiras de cartão e regras de taxa
- **Lançamento de Pagamentos**: Cálculo automático de taxas baseado em regras configuradas
- **Conferência**: Marcação de pagamentos como OK/Pendente pelo Admin
- **Logs**: Rastreamento de alterações em pagamentos
- **Dashboard**: Resumo diário de pagamentos por status e unidade

## Estrutura do Banco de Dados

- `units`: Unidades da clínica
- `AspNetUsers`: Usuários do sistema (extendido com UnitId)
- `AspNetRoles`: Roles (Admin, Recepcao)
- `payment_methods`: Formas de pagamento (Pix, Dinheiro, Cartão, etc.)
- `card_brands`: Bandeiras de cartão (Visa, Mastercard, Elo, etc.)
- `fee_rules`: Regras de taxa por forma de pagamento, bandeira e parcelas
- `payments`: Pagamentos lançados
- `payment_logs`: Histórico de alterações em pagamentos

## Configuração Local

1. Clone o repositório
2. Configure a connection string PostgreSQL em User Secrets:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=clinica;Username=postgres;Password=your_password"
   ```
3. Execute as migrations:
   ```bash
   dotnet ef database update
   ```
4. Rode a aplicação:
   ```bash
   dotnet run
   ```

## Deploy no Railway

### Pré-requisitos

- Conta no Railway
- Repositório Git (GitHub, GitLab, etc.)

### Passos para Deploy

1. **Push do código para o repositório Git**

2. **Criar projeto no Railway**
   - Acesse railway.app
   - Clique em "New Project"
   - Selecione "Deploy from GitHub repo"
   - Escolha seu repositório

3. **Configurar variáveis de ambiente**
   - No projeto Railway, vá em "Settings" → "Variables"
   - Adicione a variável:
     ```
     ConnectionStrings__DefaultConnection=Host=seu-host.railway.app;Port=5432;Database=railway;Username=postgres;Password=sua-senha;SSL Mode=Require;Trust Server Certificate=true
     ```

4. **Criar banco PostgreSQL no Railway**
   - No projeto Railway, clique em "New Service"
   - Selecione "PostgreSQL"
   - Railway vai criar o banco e fornecer a connection string

5. **Atualizar a connection string**
   - Copie a connection string do PostgreSQL Railway
   - Atualize a variável de ambiente `ConnectionStrings__DefaultConnection` com a connection string do seu banco Railway

6. **Deploy automático**
   - Railway fará o deploy automaticamente após o push
   - O build vai instalar dependências e rodar as migrations automaticamente

### Credenciais Padrão

Após o primeiro deploy, o sistema criará automaticamente:
- **Email**: admin@clinica.com
- **Senha**: Admin@123
- **Role**: Admin

**Importante**: Altere a senha do Admin após o primeiro login!

## Acesso ao Sistema

- URL do Railway: `https://seu-projeto.railway.app`
- Login com as credenciais padrão acima

## Desenvolvimento

### Criar nova migration
```bash
dotnet ef migrations add NomeDaMigration
```

### Aplicar migration
```bash
dotnet ef database update
```

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

## Segurança

- Senhas armazenadas com hash (ASP.NET Core Identity)
- Connection string em User Secrets (desenvolvimento) ou variáveis de ambiente (produção)
- Roles para controle de acesso (Admin/Recepção)
- Isolamento por unidade (Global Query Filters)
- Logs de auditoria para pagamentos
