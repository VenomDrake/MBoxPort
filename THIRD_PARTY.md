MboxPort 1.0.1 (in precedenza PostaBridge) — software libero GPL-3.0-or-later

Interfaccia e integrazione PostaBridge © 2026.

Il motore deriva da ContinuMail Converter, © 2026 Aksel Visby (ContinuMail),
GPL-3.0-or-later: https://github.com/ContinuMail/continumail-converter
Snapshot: d94d35e2f43611f0cbc85e1a8686b75249821079.
Codice completo, licenza e attribuzioni in third_party/ContinuMail.
PostaBridge è un progetto derivato indipendente, senza affiliazione con ContinuMail.

Il nome dell'applicazione è diventato MboxPort il 4 novembre 2026.
I nomi dei progetti e namespace interni possono mantenere il nome PostaBridge.

Modifiche a ContinuMail, 20 ottobre 2026:
- aggiunto CountMessagesWithProgress a MboxParser.cs per il conteggio con
  avanzamento in byte e annullamento a ogni buffer di lettura;
- aggiunta una callback opzionale al lettore interno; il comportamento dei
  chiamanti originali è invariato.

Modifiche a ContinuMail, 4 novembre 2026:
- gli errori I/O della sorgente interrompono la conversione e conservano
  il rapporto parziale dopo la chiusura delle parti PST;
- i contatori del rapporto distinguono gli elementi conservati da quelli
  eliminati con la parte incompleta, per ogni tipo di elemento supportato;
- aggiunti test di regressione per lettura incompleta e rollback.

PSTFileFormat e Utilities, © 2012–2019 ROM Knowledgeware / Tal Aloni,
LGPL-3.0-or-later, sono inclusi nella copia di ContinuMail con le sue modifiche.
Vedi third_party/ContinuMail/vendor/LICENSE-PSTFileFormat.txt e
third_party/ContinuMail/vendor/PSTFileFormat-MODIFICATIONS.md.

Le dipendenze NuGet e le loro attribuzioni sono descritte anche in
third_party/ContinuMail/NOTICE. I pacchetti ripristinati e i relativi testi di
licenza sono elencati nella cartella licenses della distribuzione binaria.
Il runtime .NET e WPF di Microsoft è incluso nella versione autonoma.
Licenza MIT e attribuzioni: https://github.com/dotnet/runtime e
https://github.com/dotnet/wpf.

Questo software è fornito senza garanzia. Puoi modificarlo e ricompilarlo
seguendo README.md e build.ps1. Distribuisci sempre anche i sorgenti completi
e le licenze quando ridistribuisci il pacchetto.
