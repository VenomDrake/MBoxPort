# MboxPort

[English](README.md) · [Italiano](README.it.md) · [简体中文](README.zh-Hans.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Bahasa Indonesia](README.id.md)

**Conversão gratuita e local de MBOX para PST no Windows. Não requer Outlook.**

[Alterações](CHANGELOG.md) · [Licença](LICENSE) · [Avisos de terceiros](THIRD_PARTY.md)

[Baixar a versão mais recente](https://github.com/VenomDrake/MBoxPort/releases/latest)

O MboxPort converte um arquivo MBOX padrão de cada vez em arquivos PST Unicode. As mensagens e os anexos permanecem no seu computador. O aplicativo para Windows inclui seu próprio ambiente de execução .NET.

## Uso

1. Extraia todo o conteúdo do arquivo `MboxPort-1.0.1-Portatile-x64.zip` e execute `MboxPort.exe`, ou use o instalador quando houver um disponível.
2. Escolha um arquivo ou arraste um MBOX para a janela. São aceitos arquivos sem extensão no nome.
3. Selecione uma pasta de destino existente e o tamanho aproximado das partes PST: 10, 20 ou 40 GiB.
4. Inicie a conversão. Cada execução cria sua própria pasta de resultados.
5. Abra os resultados e consulte o relatório antes de considerar válido um arquivo com avisos ou mensagens ignoradas.

Requer Windows 10/11 x64. O executável não tem assinatura digital.

## Idiomas

O seletor de idioma oferece **10 idiomas**, incluindo italiano. A seleção inicial segue o idioma do sistema quando ele é compatível; caso contrário, usa o inglês. O idioma selecionado é salvo localmente.

| Idioma | Código |
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

A interface, as mensagens do aplicativo e o relatório de conversão legível por pessoas usam o idioma selecionado. O árabe usa uma disposição da direita para a esquerda. Os detalhes de diagnóstico de baixo nível do mecanismo e do sistema operacional podem permanecer no idioma original. Os nomes das propriedades JSON e dos arquivos de relatório são mantidos em todos os idiomas.

## Resultados e cancelamento

- Os arquivos PST são divididos automaticamente. O tamanho selecionado é aproximado: os dados de estrutura e uma mensagem individual grande podem fazer com que ele seja ultrapassado.
- `rapporto.txt` é o relatório legível por pessoas; `rapporto-dettagliato.json` contém os detalhes do mecanismo; `sessione.json` registra o estado da execução e os contadores finais.
- Uma falha na leitura do arquivo de origem interrompe a conversão. As partes PST concluídas podem ser mantidas, mas são resultados parciais.
- O cancelamento descarta o PST incompleto em andamento e mantém as partes concluídas anteriormente. A contagem de mensagens salvas considera apenas os arquivos mantidos; as mensagens descartadas com a parte incompleta são informadas separadamente.
- O aplicativo confere a contagem final das mensagens e reabre os PST concluídos para verificar quantos itens estão armazenados. Essas verificações não garantem a exibição visual de todas as mensagens no Outlook.
- Cada conversão lê o arquivo original sem modificá-lo. Não há retomada automática.

## Compatibilidade e limites

Esta interface importa apenas e-mails no formato MBOX/MIME padrão. Extraia as exportações compactadas antes de selecionar o MBOX. Variantes específicas de fornecedores exigem testes com uma amostra. Um MBOX isolado não contém necessariamente a estrutura das pastas de origem, as etiquetas, os estados de leitura, os contatos ou os calendários.

Os arquivos de entrada podem ultrapassar 20 GiB, mas uma conversão completa com dados reais acima de 20 GiB não foi validada. Mensagens individuais com mais de aproximadamente 1900 MiB são ignoradas e informadas no relatório. A análise de mensagens grandes pode exigir bastante memória, e o cancelamento pode ter de aguardar o término da operação de análise em andamento. Reserve espaço em disco tanto para os PST de saída quanto para os anexos temporários. Use um sistema de arquivos que aceite arquivos grandes.

## Compilação a partir do código-fonte

Requer Windows x64, o SDK do .NET **8.0.427** ou uma atualização de correção posterior permitida por `global.json`, e acesso ao NuGet para a primeira restauração.

```powershell
./build.ps1
```

O script restaura as dependências de `win-x64` respeitando os arquivos de bloqueio, publica o aplicativo autônomo em um diretório de preparação limpo, executa os testes do aplicativo e uma seleção de testes de regressão do mecanismo e converte 240 mensagens sintéticas. Ele cria pacotes ZIP do código-fonte e da versão portátil, além de `SHA256SUMS.txt`, em `artifacts/`. O Inno Setup é usado para criar um instalador quando está disponível:

```powershell
./build.ps1 -InnoCompiler 'C:\path\to\Inno Setup 6\ISCC.exe'
```

O nome e a versão do produto são definidos em `src/Directory.Build.props`. O código-fonte completo do ContinuMail e de seu mecanismo PST está incluído. As versões das dependências NuGet são registradas em arquivos de bloqueio. As ferramentas de compilação e os pacotes NuGet não são incluídos como um SDK para uso sem conexão.

Depois que `build.ps1` restaurar as dependências, as verificações individuais podem ser executadas da seguinte forma:

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
```

A compilação separada da interface de linha de comando fornece o caminho esperado pelos testes de ponta a ponta do projeto original. Ela não altera a configuração de restauração do aplicativo. Consulte `VALIDAZIONE.txt` para ver as verificações realmente executadas e seus limites.

Um script opcional de leitura independente está incluído em `tools/verify_independent.py`. Ele requer Python 3.12+ e `pypstreader==1.0.0`; utiliza os resultados sintéticos do SmokeTest.

## Origem e licença

O MboxPort se chamava **PostaBridge** anteriormente. É uma integração independente baseada no [ContinuMail](https://github.com/ContinuMail/continumail-converter), com [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat). É distribuído sob a **GNU GPL v3 ou posterior**, sem garantia. A biblioteca PST mantém seus termos LGPL. Os avisos de direitos autorais, as licenças e as atribuições do código-fonte existentes são preservados em [THIRD_PARTY.md](THIRD_PARTY.md) e no código-fonte incluído do projeto original.

Os arquivos das versões publicadas incluem o código-fonte correspondente e os avisos de licença.
