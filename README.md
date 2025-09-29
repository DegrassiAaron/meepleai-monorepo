# MeepleAI Monorepo

![CI](https://github.com/MeepleAI/meepleai-monorepo/actions/workflows/ci.yml/badge.svg)


Questo repository ospita gli stack principali di MeepleAI:

- **apps/web** – front-end Next.js per l'interfaccia degli agenti Meeple.
- **apps/api** – API .NET per ingesti PDF, Q&A e seed di demo.
- **infra/** – definizioni Docker Compose e file di ambiente per avviare rapidamente i servizi di base (Postgres, Redis, Qdrant, n8n, API e web).

## Avvio rapido con Docker Compose

1. Copia i template `infra/env/*.env.dev.example` in file `infra/env/*.env.dev` non tracciati (lo script `scripts/dev-up.ps1` lo fa automaticamente al primo avvio) e personalizzali con i tuoi segreti locali. Tutti i valori forniti di default sono sicuri per lo sviluppo locale.
2. Avvia lo stack completo:
   ```bash
   cd infra
   docker compose up -d --build
   ```
3. Apri il front-end su [http://localhost:3000](http://localhost:3000) e le API su [http://localhost:8080](http://localhost:8080).

Ogni servizio espone un healthcheck nel `docker-compose.yml`, per cui `docker compose ps` mostra lo stato "healthy" quando l'avvio è completo.

## Database

### Avvio di Postgres via Docker Compose

- Il servizio Postgres è definito in `infra/docker-compose.yml` con credenziali di default `meeple / meeplepass` e database `meepleai`.\
  Puoi avviare solo il database (più eventuali dipendenze) con:
  ```bash
  cd infra
  docker compose up -d postgres
  ```
- Il volume `pgdata` mantiene i dati tra i riavvii; lo script `infra/init/postgres-init.sql` è montato come bootstrap e rimane vuoto perché lo schema è gestito da EF Core.

### Migrazioni EF Core

- Assicurati di avere installato lo strumento CLI (`dotnet tool install --global dotnet-ef`) e di esportare le variabili d'ambiente presenti in `infra/env/api.env.dev.example` (copiandole in `infra/env/api.env.dev`) o in un tuo `.env` locale.
- Posizionati nel progetto API e applica le migrazioni con:
  ```bash
  cd apps/api/src/Api
  dotnet ef database update
  ```
  Se stai aggiungendo una nuova migrazione, usa `dotnet ef migrations add <NomeMigrazione>` specificando `--project` e `--startup-project` se lavori da una directory diversa.
- Le migrazioni generate sono versionate nella cartella `apps/api/src/Api/Migrations/`, così da avere commit tracciabili insieme al codice applicativo.

### Modello ER multi-tenant

```mermaid
erDiagram
    TENANTS {
        string tenant_id PK
        string name
        timestamptz created_at
    }
    USERS {
        string user_id PK
        string tenant_id FK
        string email
        string role
    }
    GAMES {
        string game_id PK
        string tenant_id FK
        string rule_spec_id FK
        string status
    }
    RULE_SPECS {
        string rule_spec_id PK
        string tenant_id FK
        string version
        timestamptz published_at
    }
    AGENTS {
        string agent_id PK
        string tenant_id FK
        string kind
        string display_name
    }
    CHATS {
        string chat_id PK
        string tenant_id FK
        string game_id FK
        string rule_spec_id FK
        string agent_id FK
        timestamptz started_at
    }
    CHAT_LOGS {
        string log_id PK
        string tenant_id FK
        string chat_id FK
        string speaker
        text body
    }

    TENANTS ||--o{ USERS : "PK tenant_id"
    TENANTS ||--o{ GAMES : "PK tenant_id"
    TENANTS ||--o{ RULE_SPECS : "PK tenant_id"
    TENANTS ||--o{ AGENTS : "PK tenant_id"
    TENANTS ||--o{ CHATS : "PK tenant_id"
    TENANTS ||--o{ CHAT_LOGS : "PK tenant_id"
    RULE_SPECS ||--o{ GAMES : "FK rule_spec_id"
    RULE_SPECS ||--o{ CHATS : "FK rule_spec_id"
    GAMES ||--o{ CHATS : "FK game_id"
    AGENTS ||--o{ CHATS : "FK agent_id"
    CHATS ||--o{ CHAT_LOGS : "FK chat_id"
```

> Multi-tenant indexing: oltre alle chiavi primarie, ogni tabella deve avere almeno un indice composto su `(tenant_id, <chiave_naturale>)` e indici aggiuntivi su `(tenant_id, game_id)` per `chats` e `(tenant_id, published_at)` per `rule_specs` per supportare filtri e retention richiesti da DB-01.

### Seed demo e vincoli di tenancy

- L'endpoint `POST /admin/seed` popola (o rigenera) una demo di regole di gioco tramite `RuleSpecService`, utile per validare rapidamente lo stack QA; lo script `scripts/seed-demo.ps1` lo invoca automaticamente contro le API locali.
- Il front-end si connette usando `NEXT_PUBLIC_TENANT_ID=dev`, quindi i dati demo devono sempre essere creati con `tenant_id = "dev"` e `game_id = "demo-chess"` per permettere alle chiamate `/agents/qa` di funzionare senza configurazione aggiuntiva.
- Le chat devono referenziare sia il gioco (`game_id`) sia il modello di regole (`rule_spec_id`) per il tenant corrente; la cancellazione di un tenant deve propagare tramite cascade verso utenti, giochi, agenti, chat e log per evitare orfani e mantenere l'isolamento. Quando si applicano nuove migrazioni verificare che tutte le FK abbiano `ON DELETE CASCADE` o strategie equivalenti compatibili con RLS.
- Per la verifica di DB-01 assicurarsi che: (1) tutte le tabelle includano `tenant_id NOT NULL`, (2) gli indici multi-tenant siano presenti, e (3) i dati seed rispettino l'isolamento (nessun record cross-tenant, chat/log sempre filtrati per `tenant_id` e `game_id`).

## Struttura

```
apps/
  web/          # Next.js app + Dockerfile + template .env.dev.example
  api/          # Progetto .NET (Api + test + Dockerfile)
infra/
  docker-compose.yml
  env/          # Template .env.dev.example e .env.ci.example per i servizi
  init/         # Script inizializzazione Postgres
meepleai_backlog/ # Backlog prodotto
scripts/, tools/, schemas/ ...
```

## Test locali

- Front-end: `cd apps/web && npm test`
- API: `cd apps/api && dotnet test` (richiede .NET 8 SDK installato in locale)

Per altre linee guida consulta `agents.md` e i README specifici nelle rispettive app.

## Hook di sicurezza locali

1. Installa le dipendenze di sviluppo Python (richiede Python 3.9+):
   ```bash
   python -m pip install --user -r requirements-dev.txt
   ```
   > Su Windows puoi usare `py -3 -m pip install --user -r requirements-dev.txt`. Se ottieni l'errore `No module named pre_commit`, ripeti il comando per assicurarti che `pip` abbia installato il pacchetto.
2. Installa gli hook di pre-commit nel repository:
   ```bash
   pre-commit install
   ```
3. Esegui un controllo completo (opzionale ma consigliato prima del primo commit):
   ```bash
   pre-commit run --all-files
   ```

## Gestione secrets e rotazione

- I file `infra/env/*.env.dev` restano fuori dal versionamento (`.gitignore`) e sono pensati solo per lo sviluppo locale. Usa i template `*.env.dev.example` come base e mantieni le credenziali nel tuo password manager.
- Per la CI utilizza variabili sicure (GitHub Secrets/Environments) che popolano file `infra/env/*.env.ci` o variabili d'ambiente equivalenti. I template `*.env.ci.example` elencano i nomi richiesti senza fornire valori sensibili.
- **Rotazione OpenRouter API key:** genera una chiave dedicata per MeepleAI, ruotala almeno ogni 90 giorni o immediatamente in caso di sospetta fuga, aggiorna il secret GitHub `OPENROUTER_API_KEY` e invalida la chiave precedente dal pannello OpenRouter.
- **Rotazione GitHub PAT (per n8n o automazioni):** usa PAT con scope minimi, memorizzalo come secret GitHub (`GITHUB_TOKEN`/`N8N_GITHUB_PAT`), ruotalo ogni 90 giorni e revoca immediatamente i token inutilizzati.
- Configura gli hook di sicurezza come descritto nella sezione precedente: il gancio `detect-secrets` blocca la maggior parte dei leak accidentali; aggiorna il baseline con `detect-secrets scan > .secrets.baseline` solo dopo aver verificato che non siano presenti segreti reali.
