# Guia de Deploy no Railway

Este guia explica como hospedar o sistema Clinica Odontologica no Railway para que seu cliente possa acessar pela internet.

## O que é Railway?

Railway é uma plataforma de hospedagem na nuvem que oferece:
- Hospedagem gratuita para projetos pequenos
- Banco de dados PostgreSQL incluso
- Deploy automático via Git
- Fácil configuração
- Domínio HTTPS gratuito

## Passo a Passo para Deploy

### 1. Preparar o Repositório Git

Se você ainda não tem o projeto no GitHub:

```bash
# Inicializar o Git (se ainda não fez)
git init

# Adicionar todos os arquivos
git add .

# Commit inicial
git commit -m "Initial commit"

# Criar repositório no GitHub e conectar
git remote add origin https://github.com/SEU_USUARIO/clinica-odontologica.git
git branch -M main
git push -u origin main
```

### 2. Criar Conta no Railway

1. Acesse: https://railway.app/
2. Clique em "Start a new project"
3. Faça login com GitHub, GitLab ou email

### 3. Criar Novo Projeto no Railway

1. No dashboard do Railway, clique em "New Project"
2. Selecione "Deploy from GitHub repo"
3. Autorize o Railway a acessar seu GitHub
4. Selecione o repositório `clinica-odontologica`

### 4. Configurar o Projeto

O Railway vai detectar automaticamente que é um projeto .NET e configurar:
- **Build Command**: `dotnet publish -c Release -o out`
- **Start Command**: `dotnet out/ClinicaOdontologica.dll --urls http://0.0.0.0:$PORT`

### 5. Adicionar Banco de Dados PostgreSQL

1. No projeto do Railway, clique em "New Service"
2. Selecione "Database"
3. Escolha "PostgreSQL"
4. O Railway vai criar um banco de dados PostgreSQL

### 6. Configurar Variáveis de Ambiente

1. Clique no serviço da aplicação (não no banco de dados)
2. Vá para a aba "Variables"
3. O Railway já vai ter a variável `DATABASE_URL` configurada automaticamente
4. Se precisar adicionar outras variáveis, clique em "New Variable"

### 7. Executar Migrations no Banco de Dados

Após o deploy, você precisa executar as migrations para criar as tabelas:

1. No Railway, clique no serviço do banco de dados PostgreSQL
2. Clique em "Connect" e copie a connection string
3. No seu terminal local, execute:

```bash
# Substitua pela connection string do Railway
export DATABASE_URL="postgresql://usuario:senha@host:porta/banco"
dotnet ef database update
```

Ou você pode adicionar um script de inicialização no Program.cs para executar migrations automaticamente no primeiro deploy.

### 8. Acessar a Aplicação

1. No Railway, clique no serviço da aplicação
2. Clique no domínio gerado (ex: `https://clinica-odontologica-production.up.railway.app`)
3. A aplicação estará disponível para seu cliente!

### 9. Configurar Domínio Personalizado (Opcional)

Se você quiser um domínio próprio:

1. No Railway, clique no serviço da aplicação
2. Vá para "Settings" > "Networking"
3. Clique em "Custom Domain"
4. Adicione seu domínio (ex: `clinica.seudominio.com`)
5. Configure o DNS conforme as instruções do Railway

## Atualizações Futuras

Para atualizar a aplicação:

1. Faça as alterações no código
2. Commit e push para o GitHub:
   ```bash
   git add .
   git commit -m "Descrição da alteração"
   git push
   ```
3. O Railway vai fazer deploy automático das alterações

## Backup do Banco de Dados

O Railway faz backup automático do banco de dados. Para fazer backup manual:

1. No Railway, clique no serviço do banco de dados
2. Clique em "Backups"
3. Clique em "New Backup"

## Monitoramento

No Railway você pode monitorar:
- Logs da aplicação
- Métricas de uso
- Status do banco de dados
- Histórico de deploys

## Custos

- Plano gratuito: $5/mês (suficiente para uso inicial)
- Inclui: 512MB RAM, 1 vCPU, 1GB de armazenamento
- PostgreSQL incluso
- Domínio HTTPS gratuito

Para uso comercial, considere o plano pago a partir de $20/mês.

## Suporte

Se tiver problemas:
- Verifique os logs no Railway
- Confirme se as migrations foram executadas
- Verifique as variáveis de ambiente
- Consulte a documentação: https://docs.railway.app/
