# MboxPort

[English](README.md) · [Italiano](README.it.md) · [简体中文](README.zh-Hans.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Bahasa Indonesia](README.id.md)

**Konversi MBOX ke PST gratis dan lokal untuk Windows. Tidak memerlukan Outlook.**

[Perubahan](CHANGELOG.md) · [Lisensi](LICENSE) · [Pemberitahuan pihak ketiga](THIRD_PARTY.md)

[Unduh rilis terbaru](https://github.com/VenomDrake/MBoxPort/releases/latest)

MboxPort mengonversi satu arsip MBOX standar setiap kali menjadi berkas PST Unicode. Pesan dan lampiran tetap berada di komputer Anda. Aplikasi Windows ini menyertakan runtime .NET yang dibutuhkannya.

## Penggunaan

1. Ekstrak seluruh arsip `MboxPort-1.0.1-Portatile-x64.zip` dan jalankan `MboxPort.exe`, atau gunakan penginstal jika tersedia.
2. Pilih berkas, atau seret satu MBOX ke jendela aplikasi. Arsip tanpa ekstensi nama berkas juga didukung.
3. Pilih folder tujuan yang sudah ada dan perkiraan ukuran setiap bagian PST: 10, 20, atau 40 GiB.
4. Mulai konversi. Setiap proses membuat folder hasilnya sendiri.
5. Buka hasil dan periksa laporan sebelum mengandalkan arsip yang memiliki peringatan atau pesan yang dilewati.

Memerlukan Windows 10/11 x64. Berkas aplikasi tidak ditandatangani secara digital.

## Bahasa

Pemilih bahasa menyediakan **10 bahasa**, termasuk bahasa Italia. Pilihan awal mengikuti bahasa sistem jika didukung; jika tidak, aplikasi menggunakan bahasa Inggris. Bahasa yang dipilih disimpan secara lokal.

| Bahasa | Kode |
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

Antarmuka, pesan aplikasi, dan laporan konversi yang dapat dibaca menggunakan bahasa yang dipilih. Bahasa Arab menggunakan tata letak dari kanan ke kiri. Rincian diagnostik tingkat rendah dari mesin konversi dan sistem operasi dapat tetap menggunakan bahasa aslinya. Nama properti JSON dan nama berkas laporan tetap sama dalam semua bahasa.

## Hasil dan pembatalan

- Berkas PST dibagi secara otomatis. Ukuran yang dipilih adalah perkiraan: data tambahan untuk penyimpanan dan satu pesan berukuran besar dapat membuat ukurannya melebihi batas tersebut.
- `rapporto.txt` adalah laporan yang dapat dibaca; `rapporto-dettagliato.json` berisi rincian mesin konversi; `sessione.json` mencatat status proses dan jumlah akhir.
- Kegagalan membaca sumber menghentikan konversi. Bagian PST yang sudah selesai dapat tetap tersimpan, tetapi hasil tersebut bersifat parsial.
- Pembatalan membuang PST yang sedang dibuat dan belum selesai, serta mempertahankan bagian yang telah selesai sebelumnya. Jumlah pesan tersimpan hanya mengacu pada berkas yang dipertahankan; pesan yang dibuang bersama bagian yang belum selesai dilaporkan secara terpisah.
- Aplikasi memeriksa perhitungan akhir pesan dan membuka kembali PST yang telah selesai untuk memverifikasi jumlah item yang tersimpan. Pemeriksaan ini tidak menjamin tampilan visual setiap pesan di Outlook.
- Setiap konversi membaca berkas asli tanpa mengubahnya. Tidak tersedia fitur melanjutkan proses secara otomatis.

## Kompatibilitas dan batasan

Antarmuka ini hanya mengimpor email MBOX/MIME standar. Ekstrak hasil ekspor yang dikompresi sebelum memilih MBOX. Variasi format khusus penyedia layanan perlu diuji menggunakan sampel. Satu MBOX terpisah belum tentu berisi struktur folder sumber, label, status baca, kontak, atau kalender.

Berkas masukan dapat melebihi 20 GiB, tetapi konversi lengkap terhadap data nyata berukuran lebih dari 20 GiB belum divalidasi. Pesan individual yang lebih besar dari sekitar 1900 MiB akan dilewati dan dilaporkan. Penguraian pesan besar dapat memerlukan banyak memori, dan pembatalan dapat menunggu operasi penguraian yang sedang berjalan selesai. Sediakan ruang disk untuk keluaran PST maupun lampiran sementara. Gunakan sistem berkas yang mendukung berkas besar.

## Membangun dari kode sumber

Memerlukan Windows x64, .NET SDK **8.0.427** atau patch yang lebih baru yang diizinkan oleh `global.json`, serta akses ke NuGet untuk pemulihan dependensi pertama.

```powershell
./build.ps1
```

Skrip menjalankan pemulihan dependensi `win-x64` dengan versi yang dikunci, menerbitkan aplikasi beserta runtime-nya ke direktori penyiapan yang bersih, menjalankan pengujian aplikasi dan pengujian regresi mesin konversi yang dipilih, serta mengonversi 240 pesan sintetis. Skrip membuat paket ZIP kode sumber dan versi portabel, serta `SHA256SUMS.txt`, di dalam `artifacts/`. Inno Setup digunakan untuk membuat penginstal jika tersedia:

```powershell
./build.ps1 -InnoCompiler 'C:\path\to\Inno Setup 6\ISCC.exe'
```

Nama dan versi produk ditentukan di `src/Directory.Build.props`. Kode sumber lengkap ContinuMail dan mesin PST-nya disertakan. Versi dependensi NuGet dicatat dalam berkas pengunci. Alat untuk membangun aplikasi dan paket NuGet tidak disertakan sebagai SDK luring.

Setelah `build.ps1` memulihkan dependensi, pemeriksaan individual dapat dijalankan sebagai berikut:

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
```

Proses membangun CLI secara terpisah menyediakan jalur yang diharapkan oleh pengujian menyeluruh dari proyek asal. Proses ini tidak mengubah konfigurasi pemulihan dependensi aplikasi. Lihat `VALIDAZIONE.txt` untuk pemeriksaan yang benar-benar telah dilakukan beserta batasannya.

Skrip pembacaan ulang independen yang bersifat opsional disertakan di `tools/verify_independent.py`. Skrip ini memerlukan Python 3.12+ dan `pypstreader==1.0.0`, serta bekerja pada keluaran sintetis SmokeTest.

## Asal dan lisensi

MboxPort sebelumnya bernama **PostaBridge**. Aplikasi ini merupakan integrasi independen berdasarkan [ContinuMail](https://github.com/ContinuMail/continumail-converter), dengan [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat). Aplikasi didistribusikan berdasarkan **GNU GPL v3 atau yang lebih baru**, tanpa jaminan. Pustaka PST tetap menggunakan ketentuan LGPL-nya. Pemberitahuan hak cipta, lisensi, dan atribusi kode sumber yang sudah ada dipertahankan di [THIRD_PARTY.md](THIRD_PARTY.md) serta kode sumber proyek asal yang disertakan.

Arsip rilis menyertakan kode sumber yang sesuai dan pemberitahuan lisensi.
