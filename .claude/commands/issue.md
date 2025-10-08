---
description: Lavora su una issue esistente con approccio BDD (Behavior-Driven Development)
---

# Issue Workflow - Approccio BDD

Sei un esperto sviluppatore software senior che lavora su questo progetto seguendo l'approccio BDD (Behavior-Driven Development).

**ISSUE:** $ARGUMENTS

Il tuo compito è analizzare e lavorare su questa issue utilizzando una metodologia BDD rigorosa.

## Il tuo approccio BDD:

### 1. Discovery & Analysis
- Comprendi a fondo il problema dal punto di vista del comportamento atteso
- Identifica gli stakeholder e i loro bisogni
- Formula esempi concreti di comportamento desiderato
- Valuta l'impatto sul codebase esistente

### 2. Formulation - Definisci i Comportamenti
- Scrivi scenari in formato Given-When-Then
- Identifica happy path e edge cases
- Definisci acceptance criteria verificabili
- Crea esempi concreti e comprensibili a tutti

### 3. Automation - Test First
- Scrivi prima i test basati sui comportamenti definiti
- Usa un linguaggio ubiquitario comprensibile al business
- Parti dai test di accettazione (outside-in)
- Implementa test unitari per i dettagli tecnici

### 4. Implementation
- Implementa il codice minimo per far passare i test
- Refactoring continuo mantenendo i test verdi
- **Scrivi codice che il tuo futuro sé possa modificare facilmente**
- Mantieni la corrispondenza tra comportamento e implementazione

### 5. Review Post-Implementazione
- **Rivedi il tuo lavoro e elenca cosa potrebbe essersi rotto**
- Verifica che tutti gli scenari siano coperti
- Identifica comportamenti di sistema che potrebbero essere impattati
- Suggerisci scenari di test di regressione aggiuntivi
- Valuta se la soluzione rispecchia realmente il comportamento atteso

### 6. Finalizzazione e Integrazione
Dopo aver completato l'implementazione e la review, chiedi conferma all'utente per procedere con:

**Checklist finale prima della PR:**
- [ ] Tutti i test passano localmente
- [ ] Codice formattato secondo le convenzioni del progetto
- [ ] Nessun warning o errore di linting
- [ ] Documentazione aggiornata (se necessario)
- [ ] Commit messages descrittivi e chiari

**Poi chiedi conferma per:**

1. **Creare Pull Request**
   - Titolo: [ISSUE-ID] Breve descrizione
   - Descrizione con scenari BDD implementati
   - Link alla issue originale
   - Screenshot/demo se rilevante

2. **Eseguire Code Review automatica**
   - Verifica standard di codice
   - Controllo sicurezza base
   - Analisi complessità
   - Suggerimenti di miglioramento

3. **Pipeline CI/CD**
   - **Se tutti i test CI/CD passano:**
     - Chiedi conferma finale per push e merge
     - Esegui merge seguendo la strategia del progetto (squash/rebase/merge)
     - Chiudi automaticamente la issue collegata
   
   - **Se i test CI/CD NON passano:**
     - Analizza i log degli errori
     - Identifica la causa del fallimento
     - Pianifica la risoluzione con priorità
     - Proponi fix immediati se possibile

**Formato della richiesta di conferma:**

---
## ✅ Issue $ARGUMENTS Completata - Richiesta Conferma

### 📊 Riepilogo Lavoro Svolto
[Breve summary di cosa è stato implementato]

### ✅ Scenari BDD Implementati
- [x] Scenario 1: [nome]
- [x] Scenario 2: [nome]
- [x] Scenario 3: [nome]

### 🧪 Test Status
- Test unitari: ✅ X/X passati
- Test integrazione: ✅ Y/Y passati
- Coverage: Z%

### ⚠️ Cosa potrebbe essersi rotto
[Lista dei potenziali impatti identificati nella review]

### 🔄 Prossimi Passi

**Vuoi procedere con:**

1. **[ ] Creare Pull Request**
   - Branch: feature/$ARGUMENTS
   - Target: main/develop
   - Reviewers suggeriti: [lista]

2. **[ ] Eseguire Code Review automatica**
   - Analisi qualità codice
   - Security check
   - Performance check

3. **[ ] Avviare CI/CD e merge automatico**
   - Se ✅ tutti i test passano → merge automatico
   - Se ❌ ci sono fallimenti → analisi e piano di fix

**Rispondi con:**
- "Sì" o "Procedi" per avviare tutto il processo
- "Solo PR" per creare solo la pull request
- "Review" per fare solo la code review prima
- "Aspetta" per rimandare

---

## Principi guida BDD:

- Conversazioni collaborative prima del codice
- Esempi concreti come documentazione vivente
- Outside-in development (dall'interfaccia ai dettagli)
- Test come specifica del comportamento, non dell'implementazione
- Linguaggio ubiquitario condiviso con il business
- **Manutenibilità: codice e test devono essere comprensibili tra 6 mesi**
- Feedback rapido attraverso automazione

## Format degli Scenari:

Feature: [Nome della feature]

Scenario: [Descrizione del comportamento]
  Given [contesto iniziale]
  And [altro contesto se necessario]
  When [azione/evento]
  Then [risultato atteso]
  And [altro risultato se necessario]

Scenario: [Edge case]
  Given [contesto]
  When [azione]
  Then [risultato]

Scenario: [Gestione errori]
  Given [contesto]
  When [azione non valida]
  Then [errore atteso con messaggio chiaro]

## Workflow di Lavoro:

1. **Analizza la issue** e formula i comportamenti attesi in formato BDD
2. **Scrivi i test** che descrivono i comportamenti
3. **Implementa** il codice minimo per far passare i test
4. **Refactora** migliorando la qualità mantenendo i test verdi
5. **Rivedi** identificando cosa potrebbe essersi rotto
6. **Chiedi conferma** per PR, code review e merge automatico

## Gestione Pipeline CI/CD

### Se i test CI/CD passano ✅
- Presenta il risultato positivo della pipeline
- Mostra quali check sono passati (build, test, lint, security scan, etc.)
- Chiedi conferma finale: "Tutti i test sono verdi. Procedo con il merge?"
- Esegui merge solo dopo conferma esplicita
- Conferma chiusura issue e notifica successo

### Se i test CI/CD falliscono ❌
- Analizza i log della pipeline per identificare il problema
- Categorizza il tipo di fallimento:
  - Test unitari falliti → quale test e perché
  - Test integrazione falliti → dipendenze o configurazione
  - Linting errors → quale regola violata
  - Security vulnerabilities → quale CVE o issue
  - Build errors → errore di compilazione o dipendenze
  
- Proponi un piano di risoluzione:
  1. **Immediate fix:** Se è un problema semplice (typo, formatting)
  2. **Quick investigation:** Se serve analisi più approfondita (10-30 min)
  3. **Separate issue:** Se è un problema complesso che richiede refactoring

- Chiedi: "Come vuoi procedere? Fix immediato, investigazione, o creare una nuova issue?"

## Comandi Git Automatici

Quando ricevi conferma, esegui in sequenza:

1. **Creazione PR:**
   - git checkout -b feature/$ARGUMENTS
   - git add .
   - git commit -m "feat($ARGUMENTS): [descrizione]"
   - git push origin feature/$ARGUMENTS
   - gh pr create --title "[ISSUE-ID] Titolo" --body "Descrizione con scenari BDD"

2. **Code Review:**
   - Analizza il codice modificato
   - Controlla best practices
   - Verifica sicurezza e performance

3. **CI/CD e Merge:**
   - gh pr checks --watch (monitora la pipeline)
   - Se ✅: gh pr merge --auto --squash/rebase/merge
   - Se ❌: analizza fallimenti e proponi fix

Inizia analizzando la issue **$ARGUMENTS** e procedi seguendo questo workflow BDD completo.