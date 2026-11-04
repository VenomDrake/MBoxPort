# MboxPort

[English](README.md) · [Italiano](README.it.md) · [简体中文](README.zh-Hans.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Bahasa Indonesia](README.id.md)

**Conversión gratuita y local de MBOX a PST para Windows. No requiere Outlook.**

[Cambios](CHANGELOG.md) · [Licencia](LICENSE) · [Avisos de terceros](THIRD_PARTY.md)

[Descargar la última versión](https://github.com/VenomDrake/MBoxPort/releases/latest)

MboxPort convierte un archivo MBOX estándar a la vez en archivos PST Unicode. Los mensajes y los adjuntos permanecen en tu equipo. La aplicación para Windows incluye su entorno de ejecución .NET.

## Uso

1. Extrae por completo el archivo `MboxPort-1.0.1-Portatile-x64.zip` y ejecuta `MboxPort.exe`, o utiliza el instalador cuando se proporcione uno.
2. Elige un archivo o arrastra un MBOX a la ventana. Se admiten archivos sin extensión en el nombre.
3. Selecciona una carpeta de destino existente y un tamaño aproximado para las partes PST: 10, 20 o 40 GiB.
4. Inicia la conversión. Cada ejecución crea su propia carpeta de resultados.
5. Abre los resultados y revisa el informe antes de dar por válido un archivo con advertencias o mensajes omitidos.

Requiere Windows 10/11 x64. El ejecutable no tiene firma digital.

## Idiomas

El selector de idioma ofrece **10 idiomas**, incluido el italiano. La selección inicial corresponde al idioma del sistema si es compatible; de lo contrario, se utiliza el inglés. El idioma seleccionado se guarda localmente.

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

La interfaz, los mensajes de la aplicación y el informe de conversión legible por personas utilizan el idioma seleccionado. El árabe utiliza una disposición de derecha a izquierda. Los detalles de diagnóstico de bajo nivel del motor y del sistema operativo pueden permanecer en su idioma original. Los nombres de las propiedades JSON y de los archivos de informe se mantienen iguales en todos los idiomas.

## Resultados y cancelación

- Los archivos PST se dividen automáticamente. El tamaño seleccionado es aproximado: los datos de estructura y un mensaje individual grande pueden hacer que se supere.
- `rapporto.txt` es el informe legible por personas; `rapporto-dettagliato.json` contiene los detalles del motor; `sessione.json` registra el estado de la ejecución y los contadores finales.
- Un error de lectura del archivo de origen detiene la conversión. Las partes PST completadas pueden conservarse, pero son resultados parciales.
- La cancelación descarta el PST incompleto en curso y conserva las partes completadas anteriormente. El recuento de mensajes guardados se refiere solo a los archivos conservados; los mensajes descartados con la parte incompleta se indican por separado.
- La aplicación comprueba el recuento final de mensajes y vuelve a abrir los PST completados para verificar cuántos elementos contienen. Estas comprobaciones no garantizan la representación visual de cada mensaje en Outlook.
- Cada conversión lee el archivo original sin modificarlo. No hay reanudación automática.

## Compatibilidad y límites

Esta interfaz importa únicamente correo MBOX/MIME estándar. Extrae las exportaciones comprimidas antes de seleccionar el MBOX. Las variantes específicas de cada proveedor requieren pruebas con una muestra. Un MBOX aislado no contiene necesariamente la estructura de carpetas de origen, las etiquetas, los estados de lectura, los contactos ni los calendarios.

Los archivos de entrada pueden superar los 20 GiB, pero no se ha validado una conversión completa con datos reales de más de 20 GiB. Los mensajes individuales de más de aproximadamente 1900 MiB se omiten y se indican en el informe. El análisis de mensajes grandes puede requerir mucha memoria, y la cancelación puede tener que esperar a que termine la operación de análisis en curso. Reserva espacio en disco tanto para los PST de salida como para los adjuntos temporales. Utiliza un sistema de archivos que admita archivos grandes.

## Compilar desde el código fuente

Requiere Windows x64, el SDK de .NET **8.0.427** o una revisión posterior permitida por `global.json`, y acceso a NuGet para la primera restauración.

```powershell
./build.ps1
```

El script restaura las dependencias de `win-x64` respetando los archivos de bloqueo, publica la aplicación autónoma en un directorio de preparación limpio, ejecuta las pruebas de la aplicación y una selección de pruebas de regresión del motor, y convierte 240 mensajes sintéticos. Crea paquetes ZIP del código fuente y de la versión portátil, además de `SHA256SUMS.txt`, en `artifacts/`. Si Inno Setup está disponible, se utiliza para crear un instalador:

```powershell
./build.ps1 -InnoCompiler 'C:\path\to\Inno Setup 6\ISCC.exe'
```

El nombre y la versión del producto se definen en `src/Directory.Build.props`. Se incluye el código fuente completo de ContinuMail y de su motor PST. Las versiones de las dependencias NuGet se registran en archivos de bloqueo. Las herramientas de compilación y los paquetes NuGet no se incluyen como un SDK sin conexión.

Después de que `build.ps1` haya restaurado las dependencias, se pueden ejecutar comprobaciones individuales de la siguiente manera:

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
```

La compilación separada de la interfaz de línea de comandos proporciona la ruta que esperan las pruebas de extremo a extremo del proyecto original. No modifica la configuración de restauración de la aplicación. Consulta `VALIDAZIONE.txt` para conocer las comprobaciones realmente realizadas y sus límites.

Se incluye un script opcional de lectura independiente en `tools/verify_independent.py`. Requiere Python 3.12+ y `pypstreader==1.0.0`; utiliza los resultados sintéticos de SmokeTest.

## Origen y licencia

MboxPort se llamaba anteriormente **PostaBridge**. Es una integración independiente basada en [ContinuMail](https://github.com/ContinuMail/continumail-converter), con [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat). Se distribuye bajo **GNU GPL v3 o posterior**, sin garantía. La biblioteca PST mantiene sus condiciones LGPL. Los avisos de derechos de autor, las licencias y las atribuciones del código fuente existentes se conservan en [THIRD_PARTY.md](THIRD_PARTY.md) y en el código fuente incluido del proyecto original.

Los archivos de las versiones publicadas incluyen el código fuente correspondiente y los avisos de licencia.
