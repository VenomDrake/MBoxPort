# MboxPort

**Conversione gratuita e locale da MBOX a PST per Windows. Non richiede Outlook.**

[English](README.md) · [Italiano](README.it.md) · [简体中文](README.zh-Hans.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Bahasa Indonesia](README.id.md)

[Modifiche](CHANGELOG.md) · [Licenza](LICENSE) · [Attribuzioni di terze parti](THIRD_PARTY.md)

[Scarica l'ultima versione](https://github.com/VenomDrake/MBoxPort/releases/latest)

MboxPort converte un archivio MBOX standard alla volta in file PST Unicode. Messaggi e allegati rimangono sul computer. L'applicazione Windows include il proprio runtime .NET.

## Utilizzo

1. Estrai l'intero archivio `MboxPort-1.0.1-Portatile-x64.zip` e avvia `MboxPort.exe`, oppure usa l'installer quando disponibile.
2. Scegli un file oppure trascina un MBOX nella finestra. Sono supportati anche archivi senza estensione.
3. Seleziona una cartella di destinazione esistente e una dimensione indicativa per le parti PST: 10, 20 o 40 GiB.
4. Avvia la conversione. Ogni esecuzione crea una propria cartella dei risultati.
5. Apri i risultati e leggi il rapporto prima di utilizzare un archivio con avvisi o messaggi saltati.

Richiede Windows 10/11 x64. L'eseguibile non è firmato digitalmente.

## Lingue

Il selettore offre **10 lingue**, compreso l'italiano. La scelta iniziale segue la lingua del sistema quando supportata; altrimenti viene usato l'inglese. La lingua scelta viene ricordata localmente.

| Lingua | Codice |
| --- | --- |
| English | `en` |
| Italiano | `it` |
| 简体中文 | `zh-Hans` |
| हिन्दी | `hi` |
| Español | `es` |
| العربية | `ar` |
| Français | `fr` |
| বাংলা | `bn` |
| Português | `pt` |
| Bahasa Indonesia | `id` |

L'interfaccia, i messaggi applicativi e il rapporto di conversione leggibile usano la lingua selezionata. L'arabo dispone di un'interfaccia da destra a sinistra. I dettagli diagnostici del motore e del sistema operativo possono rimanere nella lingua originale. I nomi delle proprietà JSON e dei file di rapporto rimangono invariati tra le lingue.

## Risultati e annullamento

- I PST vengono divisi automaticamente. La dimensione scelta è indicativa: lo spazio aggiuntivo richiesto dal formato e un singolo messaggio grande possono farla superare.
- `rapporto.txt` è il rapporto leggibile; `rapporto-dettagliato.json` contiene i dettagli del motore; `sessione.json` registra lo stato della sessione e i contatori finali.
- Un errore di lettura della sorgente interrompe la conversione. Le parti PST già complete possono rimanere disponibili, ma costituiscono risultati parziali.
- L'annullamento elimina il PST incompleto in corso e conserva le parti precedenti già completate. Il conteggio dei messaggi salvati riguarda solo i file conservati; i messaggi eliminati con la parte incompleta sono indicati separatamente.
- L'applicazione controlla il conteggio finale dei messaggi e riapre i PST completati per verificare il numero di elementi memorizzati. Questi controlli non garantiscono la resa visiva di ogni messaggio in Outlook.
- Ogni conversione legge l'origine senza modificarla. Non è prevista la ripresa automatica.

## Compatibilità e limiti

Questa interfaccia importa soltanto email MBOX/MIME standard. Estrai le esportazioni compresse prima di selezionare il MBOX. Le varianti specifiche dei fornitori richiedono una prova su un campione. Un MBOX isolato non contiene necessariamente struttura delle cartelle, etichette, stati di lettura, contatti o calendari dell'origine.

I file di ingresso possono superare 20 GiB, ma non è stata convalidata una conversione reale completa oltre 20 GiB. I singoli messaggi superiori a circa 1900 MiB vengono saltati e riportati. L'analisi di messaggi grandi può richiedere molta memoria e l'annullamento può attendere la conclusione dell'operazione di analisi corrente. Prevedi spazio sia per i PST sia per gli allegati temporanei. Usa un filesystem che supporti file grandi.

## Compilazione dai sorgenti

Richiede Windows x64, .NET SDK **8.0.427** o una patch successiva ammessa da `global.json`, e accesso a NuGet per il primo ripristino.

```powershell
./build.ps1
```

Lo script esegue ripristini bloccati per `win-x64`, pubblica l'applicazione autonoma in una cartella temporanea pulita, esegue i test applicativi e i test di regressione selezionati del motore, quindi converte 240 messaggi sintetici. Crea gli ZIP dei sorgenti e della versione portatile e `SHA256SUMS.txt` in `artifacts/`. Quando disponibile, Inno Setup viene usato per creare un installer:

```powershell
./build.ps1 -InnoCompiler 'C:\path\to\Inno Setup 6\ISCC.exe'
```

Nome e versione del prodotto sono definiti in `src/Directory.Build.props`. Sono inclusi i sorgenti completi di ContinuMail e del relativo motore PST. Le versioni delle dipendenze NuGet sono registrate nei file di blocco. Gli strumenti di compilazione e i pacchetti NuGet non sono inclusi come SDK per la compilazione offline.

Dopo che `build.ps1` ha ripristinato le dipendenze, è possibile eseguire singolarmente i controlli seguenti:

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
```

La compilazione separata della CLI fornisce il percorso previsto dai test end-to-end upstream. Non modifica la configurazione di ripristino dell'applicazione. Consulta `VALIDAZIONE.txt` per le prove effettivamente eseguite e i relativi limiti.

È incluso uno script facoltativo di rilettura indipendente in `tools/verify_independent.py`. Richiede Python 3.12+ e `pypstreader==1.0.0` e opera sui risultati sintetici dello SmokeTest.

## Provenienza e licenza

MboxPort si chiamava in precedenza **PostaBridge**. È un'integrazione indipendente basata su [ContinuMail](https://github.com/ContinuMail/continumail-converter), con [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat). È distribuito secondo la **GNU GPL versione 3 o successiva**, senza garanzia. La libreria PST mantiene le proprie condizioni LGPL. Le informazioni sul copyright, le licenze e le attribuzioni esistenti sono conservate in [THIRD_PARTY.md](THIRD_PARTY.md) e nei sorgenti upstream inclusi.

Gli archivi delle release includono i sorgenti corrispondenti e gli avvisi di licenza.
