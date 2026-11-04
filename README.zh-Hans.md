# MboxPort

[English](README.md) · [Italiano](README.it.md) · [简体中文](README.zh-Hans.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Bahasa Indonesia](README.id.md)

**适用于 Windows 的免费本地 MBOX 转 PST 工具。无需安装 Outlook。**

[更新记录](CHANGELOG.md) · [许可证](LICENSE) · [第三方声明](THIRD_PARTY.md)

[下载最新版本](https://github.com/VenomDrake/MBoxPort/releases/latest)

MboxPort 每次将一个标准 MBOX 邮件归档转换为 Unicode PST 文件。邮件及附件始终保留在您的计算机上。Windows 应用程序自带 .NET 运行时。

## 使用方法

1. 完整解压 `MboxPort-1.0.1-Portatile-x64.zip` 归档并运行 `MboxPort.exe`，也可以在提供安装程序时使用安装程序。
2. 选择文件，或将一个 MBOX 文件拖入窗口。支持没有文件扩展名的邮件归档。
3. 选择一个已存在的目标文件夹，并设置每个 PST 分卷的大致大小：10、20 或 40 GiB。
4. 开始转换。每次运行都会创建独立的结果文件夹。
5. 打开结果。如果归档存在警告或跳过的邮件，请先查看报告，再决定是否使用该归档。

需要 Windows 10/11 x64。可执行文件未经数字签名。

## 语言

语言选择器提供包括意大利语在内的 **10 种语言**。如果支持系统语言，首次启动时会使用系统语言；否则使用英语。您选择的语言会保存在本机。

| 语言 | 代码 |
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

界面、应用程序消息和供人阅读的转换报告均使用所选语言。阿拉伯语采用从右到左的布局。底层引擎和操作系统的诊断详情可能仍保留原始语言。JSON 属性名和报告文件名不会随语言变化。

## 结果与取消

- PST 文件会自动分卷。所选大小仅为近似值：文件结构开销和单封大邮件可能使实际大小超过该值。
- `rapporto.txt` 是供人阅读的报告；`rapporto-dettagliato.json` 包含引擎详情；`sessione.json` 记录本次运行的状态和最终计数。
- 如果读取源文件失败，转换会停止。已完成的 PST 分卷可能会保留，但它们只是部分结果。
- 取消转换会丢弃当前尚未完成的 PST，并保留此前已完成的分卷。已保存邮件的计数仅包含保留文件中的邮件；随未完成分卷一起丢弃的邮件会单独列出。
- 应用程序会核对最终邮件计数，并重新打开已完成的 PST，以验证其中存储的项目数量。这些检查不能保证每封邮件在 Outlook 中的显示效果。
- 每次转换均只读取原始文件，不会修改原始文件。不支持自动续传或继续上次转换。

## 兼容性与限制

此界面仅导入标准 MBOX/MIME 邮件。请先解压压缩导出文件，再选择 MBOX 文件。邮件服务商特有的格式变体需要使用样本进行测试。单独的 MBOX 文件不一定包含原始文件夹结构、标签、已读状态、联系人或日历。

输入文件可以超过 20 GiB，但尚未验证过针对超过 20 GiB 的真实邮件归档进行完整转换的情况。大于约 1900 MiB 的单封邮件会被跳过，并在报告中注明。解析大邮件可能需要大量内存，取消操作也可能需要等待当前解析过程结束。请为 PST 输出文件和临时附件预留足够的磁盘空间，并使用支持大文件的文件系统。

## 从源代码构建

需要 Windows x64、.NET SDK **8.0.427** 或 `global.json` 允许的更高补丁版本，以及首次还原依赖项时访问 NuGet 的网络连接。

```powershell
./build.ps1
```

该脚本会以锁定模式执行 `win-x64` 依赖项还原，将自包含应用程序发布到干净的暂存目录，运行应用程序测试和选定的引擎回归测试，并转换 240 封合成邮件。它会在 `artifacts/` 下生成源代码 ZIP、便携版 ZIP 和 `SHA256SUMS.txt`。如果可用，则使用 Inno Setup 创建安装程序：

```powershell
./build.ps1 -InnoCompiler 'C:\path\to\Inno Setup 6\ISCC.exe'
```

产品名称和版本在 `src/Directory.Build.props` 中定义。已包含 ContinuMail 及其 PST 引擎的完整源代码。NuGet 依赖项的版本记录在锁定文件中。项目未将构建工具和 NuGet 包打包为离线 SDK。

`build.ps1` 还原依赖项后，可以按以下方式单独运行检查：

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
```

单独构建 CLI 是为了在上游端到端测试所预期的路径下提供可执行文件。这不会更改应用程序的依赖项还原配置。实际执行过的检查及其限制请参阅 `VALIDAZIONE.txt`。

`tools/verify_independent.py` 中包含可选的独立回读验证脚本。该脚本需要 Python 3.12+ 和 `pypstreader==1.0.0`；它用于检查合成 SmokeTest 输出。

## 来源与许可证

MboxPort 的原名为 **PostaBridge**。它是基于 [ContinuMail](https://github.com/ContinuMail/continumail-converter) 并采用 [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat) 的独立集成项目。项目依据 **GNU GPL 第 3 版或更高版本** 分发，不提供任何担保。PST 库仍遵循其 LGPL 许可条款。现有版权声明、许可证和源代码归属信息保留在 [THIRD_PARTY.md](THIRD_PARTY.md) 及随附的上游源代码中。

发布归档包含对应的源代码和许可证声明。
