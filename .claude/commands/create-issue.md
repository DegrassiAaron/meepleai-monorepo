---
description: Crea una nuova issue ben strutturata seguendo l'approccio BDD
---

# Create Issue - Template BDD

Sei un esperto Product Owner / Business Analyst che crea issue ben strutturate seguendo l'approccio BDD (Behavior-Driven Development).

**TITOLO ISSUE:** $ARGUMENTS

Il tuo compito è creare una issue completa, chiara e pronta per essere implementata dal team di sviluppo.

## Struttura della Issue:

### 1. Titolo
- Breve, descrittivo e orientato al comportamento
- Formato: [TIPO] Breve descrizione del comportamento
- Tipi disponibili: Feature, Bug, Refactor, Docs, Security, Performance, Chore

**Genera un titolo ottimizzato basato su:** $ARGUMENTS

### 2. Descrizione del Comportamento (User Story)

**Come** [ruolo utente]
**Voglio** [obiettivo/funzionalità]
**Così che** [beneficio/valore di business]

### 3. Contesto

**Perché questa issue è necessaria?**
- Problema da risolvere o valore da aggiungere
- Background e motivazione
- Link a discussioni, documenti o issue correlate (se esistenti)

### 4. Scenari BDD (Acceptance Criteria)

Scrivi scenari concreti e verificabili in formato Given-When-Then:

Feature: [Nome della feature]

Scenario: [Happy path - comportamento principale]
  Given [contesto iniziale]
  And [altro contesto se necessario]
  When [azione/evento dell'utente]
  Then [risultato atteso]
  And [altro risultato se necessario]

Scenario: [Edge case 1]
  Given [contesto specifico]
  When [azione in condizione limite]
  Then [comportamento atteso in edge case]

Scenario: [Edge case 2]
  Given [altro contesto specifico]
  When [altra azione limite]
  Then [comportamento atteso]

Scenario: [Gestione errori]
  Given [contesto]
  When [azione non valida o input errato]
  Then [errore chiaro e user-friendly]
  And [sistema rimane in stato consistente]

**Importante:** Usa dati ed esempi concreti, non placeholder generici!

### 5. Considerazioni Tecniche

**Componenti/Moduli coinvolti:**
- [Lista dei file o componenti da modificare/creare]

**Possibili approcci di implementazione:**
- [Approccio 1 con pro/contro]
- [Approccio 2 con pro/contro]
- [Raccomandazione con motivazione]

**Dipendenze:**
- [Issue prerequisite o correlate]
- [Librerie o tool necessari]

**Impatti sul sistema esistente:**
- [Funzionalità che potrebbero essere influenzate]
- [Necessità di migration o backward compatibility]

**Performance e Scalabilità:**
- [Considerazioni su carico, latenza, memoria se rilevanti]

### 6. Definition of Done

- [ ] Tutti gli scenari BDD sono implementati e i test passano
- [ ] Test unitari scritti e passano (coverage >= 80% per nuovo codice)
- [ ] Test di integrazione (se necessari) passano
- [ ] Codice reviewato e approvato
- [ ] Documentazione aggiornata (README, API docs, commenti)
- [ ] Nessuna regressione su funzionalità esistenti verificata
- [ ] Performance verificate (se rilevanti per questa issue)
- [ ] Accessibilità verificata (se impatta UI)

### 7. Stima e Priorità

**Complessità:** [S / M / L / XL]
- **S (Small):** < 4 ore, chiaro e semplice
- **M (Medium):** 1-2 giorni, complessità moderata
- **L (Large):** 3-5 giorni, richiede design o refactoring
- **XL (Extra Large):** > 5 giorni, considerare di dividere la issue

**Motivazione della stima:** [Perché questa complessità]

**Priorità:** [Critical / High / Medium / Low]
- **Critical (P0):** Blocca utenti o sistema, security issue grave
- **High (P1):** Impatta funzionalità chiave o blocca altre issue
- **Medium (P2):** Miglioramento importante ma non urgente
- **Low (P3):** Nice-to-have, può aspettare

**Motivazione della priorità:** [Perché questa priorità]

**Valore di Business:** [Alto / Medio / Basso]
- [Spiegazione del valore per utenti/business]

### 8. Out of Scope

**Cosa NON è incluso in questa issue:**
- [Elemento 1 esplicitamente escluso]
- [Elemento 2 esplicitamente escluso]
- [Possibili estensioni future da fare in issue separate]

**Scopo:** Prevenire scope creep e mantenere la issue focalizzata

### 9. Domande Aperte

**Decisioni da prendere prima dell'implementazione:**
- [ ] [Domanda 1 che richiede chiarimento]
- [ ] [Domanda 2 che richiede decisione tecnica/business]

**Assumzioni correnti:**
- [Assunzione 1 da validare]
- [Assunzione 2 da validare]

## Principi per una Buona Issue:

✅ **Chiarezza:** Chiunque nel team deve capirla senza ambiguità
✅ **Testabilità:** Criteri di accettazione verificabili oggettivamente
✅ **Indipendenza:** Può essere implementata autonomamente (quando possibile)
✅ **Valore:** Chiaro beneficio per utenti o sistema
✅ **Small:** Completabile in uno sprint (altrimenti dividere)
✅ **Esempi Concreti:** Usa dati reali, non "user123" o "example.com"

Ora genera la issue completa e strutturata basata su: **$ARGUMENTS**