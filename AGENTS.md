# Project documentation

The README must always be available in the same languages as the application.

- Keep `README.md` as the complete English version.
- Maintain complete `README.<language-code>.md` versions for every other language catalog in `src/PostaBridge.Engine/Localization/`.
- Every README must link to all language versions and contain the same sections, features, limits, licensing information, download links, and command examples.
- When a software language or user-visible feature changes, update the corresponding READMEs in the same change. Do not replace a translated README with a short summary.
- Preserve filenames, language codes, command blocks, and link targets when translating.
- Run `tools/Verify-Readmes.ps1` after documentation changes. `build.ps1` runs this check and includes all README language versions in the distributed packages.
- Leave third-party upstream README files and attribution intact unless the change specifically concerns them.
