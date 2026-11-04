# MboxPort 1.0.1

Applicazione desktop libera per Windows 10/11 x64 che converte **un file MBOX alla volta in PST Unicode**, senza Microsoft Outlook. Interfaccia WPF in italiano, conversione locale, nessun account o upload.

## Utilizzo

1. Installa `MboxPort-1.0.1-Setup-x64.exe`, se disponibile, oppure estrai la versione portatile e avvia `MboxPort.exe`.
2. Premi **Scegli file** o trascina un MBOX nella finestra. Sono selezionabili anche archivi senza estensione.
3. Scegli una cartella esistente di destinazione.
4. Scegli parti PST da circa **10, 20 o 40 GiB** (nell'interfaccia indicate come GB). Il valore predefinito è 20.
5. Premi **Converti in PST**. Il programma mostra prima l'analisi in byte, poi l'avanzamento per messaggi.
6. Apri i risultati e consulta `rapporto.txt` e `rapporto-dettagliato.json`.

Ogni esecuzione crea una sottocartella distinta. Il file di origine viene aperto in sola lettura e rimane bloccato contro modifiche durante la conversione. Messaggi e allegati non vengono caricati tutti insieme in memoria. Le parti PST vengono rilette dal motore per verificare il conteggio prima di dichiarare il completamento.

La versione 1.0.1 interrompe la conversione se non riesce a completare la lettura della sorgente. Confronta inoltre i messaggi rilevati con quelli salvati e saltati prima di annunciare il completamento. Il rapporto di una conversione annullata distingue i messaggi effettivamente salvati nei PST conservati da quelli eliminati con la parte incompleta. Vedi `CHANGELOG.md`.

## Compatibilità e limiti

- MBOX standard con separatori `From ` e messaggi MIME: adatto a esportazioni standard da Thunderbird, Gmail Takeout, Apple Mail, Aruba e altri fornitori. Il fornitore non determina il formato: le varianti non standard vanno verificate con un campione.
- Un MBOX può superare 20 GiB: non esiste un limite di 20 GiB sul file di ingresso; vengono usati flussi e offset a 64 bit. I PST si dividono automaticamente. La dimensione selezionata è indicativa, non un limite fisico rigoroso: overhead e singoli messaggi possono farla superare.
- Conservazione di testo, HTML, destinatari A/Cc/Ccn, date, intestazioni e allegati tramite il motore ContinuMail. Gli errori sui singoli messaggi vengono riportati; non vengono nascosti dietro un esito senza avvisi.
- Solo email: questa interfaccia non importa contatti, calendari o profili Thunderbird completi. Le cartelle del profilo, le etichette Gmail e gli stati contenuti soltanto negli indici `.msf` non vengono ricostruiti da un MBOX isolato. La cartella PST si chiama `Posta importata`.
- L'involucro MBOX deve essere ASCII/UTF-8 compatibile e iniziare con `From `; i charset dei corpi MIME sono decodificati dal motore. MBOX UTF-16, file corrotti, archivi compressi e formati proprietari richiedono un'esportazione standard prima della conversione.
- Il motore salta messaggi individuali oltre circa 1900 MiB e segnala il salto. Tale limite riguarda un messaggio, non l'intero archivio.
- Annullamento durante l'analisi: nessun PST viene creato. Durante la scrittura, il motore elimina la parte in corso e conserva le eventuali parti già completate, elencandole nel rapporto. Il conteggio finale dei messaggi salvati riguarda solo le parti conservate; i messaggi della parte eliminata hanno un contatore separato. L'annullamento può attendere la conclusione dell'operazione corrente su un messaggio molto grande. Non è prevista la ripresa automatica.
- Se la lettura della sorgente si interrompe, l'esito è un errore e gli eventuali PST conservati sono parziali. Quando il disco consente di scriverli, i rapporti spiegano l'errore e le parti disponibili. Il percorso dei risultati rimane accessibile anche se la scrittura del file diagnostico fallisce.
- Occorre spazio libero per i PST e per i temporanei degli allegati. Per archivi grandi usare NTFS o exFAT; FAT32 limita i singoli file a circa 4 GiB.
- L'installer è privo di firma digitale; può essere segnalato come autore non verificato da Windows. Nessun certificato di firma è stato acquistato o incluso.

## Compilazione

Richiede Windows x64, .NET SDK 8.0.427 (o una patch successiva ammessa da `global.json`) e accesso a NuGet per il primo ripristino. L'applicazione distribuita è autonoma e include il runtime .NET: chi la usa non deve installare Python, Node, Outlook o .NET separatamente.

```powershell
./build.ps1
```

Lo script cerca automaticamente Inno Setup nelle posizioni comuni. Per indicare un compilatore esplicito e produrre anche l'installer:

```powershell
./build.ps1 -InnoCompiler 'C:\percorso\Inno Setup 6\ISCC.exe'
```

L'output è in `artifacts/`. Lo script ripristina le dipendenze in modalità bloccata, compila e verifica il programma, quindi genera i sorgenti completi, la versione portatile e, se Inno Setup è disponibile, l'installer. `SHA256SUMS.txt` contiene gli hash dei pacchetti prodotti. La cartella di pubblicazione viene ricreata a ogni esecuzione per escludere file residui. I sorgenti di ContinuMail e del suo motore PST sono inclusi nel repository.

La versione dell'applicazione è definita in `src/Directory.Build.props`; il programma, i rapporti e i nomi dei pacchetti derivano da quel valore. I restore di app, test e SmokeTest usano tutti `win-x64`, così i lock dei progetti condivisi restano coerenti.

## Test

`build.ps1` esegue i test applicativi, i test selezionati del motore e una conversione sintetica. I comandi seguenti consentono di ripetere i controlli dopo il ripristino delle dipendenze eseguito dallo script.

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
dotnet run --project tools/SmokeTest/SmokeTest.csproj -c Release -r win-x64 --no-restore -- artifacts/smoke
```

Verifica indipendente opzionale, con Python 3.12+ e `pip install pypstreader==1.0.0`:

```powershell
python tools/verify_independent.py artifacts/smoke
```

Lo SmokeTest genera soltanto posta sintetica: 240 messaggi in più parti PST. Lo script Python opzionale rilegge quei file con un'implementazione indipendente e verifica allegati byte per byte, HTML, Unicode, destinatari e Message-ID. I test di regressione coprono inoltre errori di lettura e contatori dopo l'annullamento. Vedi `VALIDAZIONE.txt` per le prove effettivamente eseguite e i loro limiti.

## Provenienza e licenza

MboxPort (in precedenza PostaBridge) è un'integrazione derivata indipendente basata sul motore [ContinuMail](https://github.com/ContinuMail/continumail-converter), non un nuovo motore PST implementato da zero. Il codice del motore include [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat) sotto LGPL. La nuova interfaccia e l'integrazione sono distribuite con **GNU GPL v3 o successiva**, senza garanzia.

Vedi `LICENSE`, `THIRD_PARTY.md` e `third_party/ContinuMail/NOTICE`. La copia sorgente include il motore, le modifiche, gli script di build e i test. È possibile modificare anche il componente LGPL e ricompilare il programma.

Fonti tecniche consultate il 20 ottobre 2026:

- [ContinuMail: supporto, limiti e build](https://github.com/ContinuMail/continumail-converter).
- [libpff: accesso ai file PFF](https://github.com/libyal/libpff).
- [Microsoft: AddStoreEx](https://learn.microsoft.com/en-us/office/vba/api/outlook.namespace.addstoreex), valutato e scartato perché richiederebbe Outlook.
- [pypstreader: lettore indipendente](https://pypi.org/project/pypstreader/).
- [Formato PST Microsoft](https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-pst/).
