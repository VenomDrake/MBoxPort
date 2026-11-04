# Modifiche a MboxPort

## 1.0.1 — 4 novembre 2026

- Il nome pubblico dell'applicazione diventa MboxPort; i namespace interni rimangono compatibili.
- Interfaccia, messaggi applicativi e rapporti in dieci lingue, con selettore, preferenza locale e disposizione da destra a sinistra per l'arabo.
- README in inglese e italiano e sorgenti pronti per un repository GitHub pubblico.

- Gli errori di lettura della sorgente interrompono la conversione e vengono distinti dai singoli messaggi saltati.
- Il completamento richiede che tutti i messaggi rilevati abbiano un esito finale.
- Dopo un annullamento, i conteggi distinguono i messaggi salvati nelle parti conservate da quelli eliminati con la parte incompleta.
- L'interfaccia e i rapporti mostrano questi conteggi e lo stato dei risultati parziali.
- Un errore durante la scrittura diagnostica conserva l'errore originale e il percorso dei risultati.
- Ripristino bloccato delle dipendenze, SDK definito, percorsi PowerShell corretti e pubblicazione da una cartella pulita.
- Versione applicativa centralizzata, licenze del runtime pubblicato e hash SHA-256 dei pacchetti generati.

## 1.0.0 — 20 ottobre 2026

- Prima distribuzione ricevuta: applicazione WPF in italiano, conversione locale di un MBOX in PST e divisione in più parti.
