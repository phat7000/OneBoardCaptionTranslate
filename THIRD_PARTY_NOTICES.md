# Third-Party Notices

This file covers the NuGet dependencies used by the OneBoard Capture Translate 1.1.0 source tree. It must be regenerated and verified against the exact restored graph before a public release package is created.

The OneBoard application itself is distributed under the Apache License 2.0 in [LICENSE](LICENSE). Upstream origin and modification information is in [UPSTREAM_ATTRIBUTION.md](UPSTREAM_ATTRIBUTION.md).

## Resolved application dependencies

| Component | Version | License / notice evidence |
|---|---:|---|
| CsvHelper | 33.0.1 | `MS-PL OR Apache-2.0`; this distribution uses the Apache-2.0 option. Copyright © 2009-2024 Josh Close. |
| Interop.UIAutomationClient | 10.19041.0 | MIT license bundled as `LICENSE.txt`. Copyright © 2019 Roman. |
| Microsoft.Data.Sqlite | 9.0.1 | MIT. © Microsoft Corporation. |
| Microsoft.Data.Sqlite.Core | 9.0.1 | MIT. © Microsoft Corporation. |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.10 | Apache-2.0. Copyright 2014-2024 SourceGear, LLC. |
| SQLitePCLRaw.core | 2.1.10 | Apache-2.0. Copyright 2014-2024 SourceGear, LLC. |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.10 | Apache-2.0. Copyright 2014-2024 SourceGear, LLC. |
| SQLitePCLRaw.provider.e_sqlite3 | 2.1.10 | Apache-2.0. Copyright 2014-2024 SourceGear, LLC. |
| System.Text.Json | 8.0.5 | MIT plus bundled .NET third-party notices. © Microsoft Corporation; .NET Foundation and contributors. |
| System.Net.Http | 4.3.4 | Microsoft .NET Library terms plus bundled third-party notices. © Microsoft Corporation. |
| System.Security.Cryptography.ProtectedData | 8.0.0 | MIT. © Microsoft Corporation. |
| Microsoft.CognitiveServices.Speech | 1.51.2 | Microsoft license bundled as `LICENSE.txt`. © Microsoft Corporation. |
| Google.Cloud.Speech.V1 | 3.9.0 | Apache-2.0. Copyright 2025 Google LLC. |
| NAudio | 2.2.1 | MIT license bundled as `license.txt`. © Mark Heath 2023. |
| WPF-UI | 4.0.1 | MIT plus bundled `ThirdPartyNotices.txt`. Copyright © 2021-2025 Leszek Pomianowski and WPF UI Contributors. |
| WPF-UI.Abstractions | 4.0.1 | MIT plus bundled `ThirdPartyNotices.txt`. Copyright © 2021-2025 Leszek Pomianowski and WPF UI Contributors. |

The resolved graph also contains the legacy `System.*` and platform runtime packages pulled by System.Net.Http 4.3.4, including Microsoft.NETCore.Platforms/Targets and native cryptography/HTTP runtime packages. Those components are covered by the Microsoft .NET Library terms and the notices supplied with that package. The self-contained .NET 8 runtime is covered by the .NET MIT license and its third-party notices.

## Apache License 2.0 components

CsvHelper (under its Apache-2.0 option) and the SQLitePCLRaw packages listed above are distributed under Apache License 2.0. The complete Apache License 2.0 text is reproduced in the package root as [LICENSE](LICENSE).

## MIT-licensed components

The following permission notice applies to the MIT-licensed components identified above, with their respective copyright notices retained in this file:

> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Microsoft .NET Library terms

System.Net.Http 4.3.4 is distributed under the Microsoft .NET Library license supplied in the restored package. Those terms permit distribution of its object code as part of an application subject to their distribution requirements. The canonical package license reference is:

https://go.microsoft.com/fwlink/?LinkId=329770

The bundled System.Net.Http notice also identifies .NET Core, copyright © .NET Foundation and Contributors, under the MIT License.

## .NET runtime third-party notices

The System.Text.Json 8.0.5 and self-contained .NET runtime notice inventory includes third-party material under permissive terms, including Unicode data, zlib, Mono, Intel/Slicing-by-8 material, W3C material, Brotli, Json.NET, and other components recorded by the .NET runtime. Canonical .NET runtime license and notice sources are:

- https://github.com/dotnet/runtime/blob/main/LICENSE.TXT
- https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT

Copyright and permission notices supplied by those projects remain applicable.

## WPF-UI license and bundled notices

WPF-UI and WPF-UI.Abstractions are MIT licensed:

Copyright © 2021-2025 Leszek Pomianowski and WPF UI Contributors. https://lepo.co/

WPF-UI 4.0.1's bundled third-party notice identifies these incorporated components:

1. `sbaeumlisberger/VirtualizingWrapPanel` 2.0.6 — MIT; Copyright © 2019 S. Bäumlisberger.
2. `microsoft/fluentui-system-icons` 1.1.242 — MIT; Copyright © 2020 Microsoft Corporation.
3. `dotnet/wpf` 8.0 — MIT; Copyright © .NET Foundation and Contributors.
4. `microsoft/microsoft-ui-xaml` 3.0 — MIT; Copyright © Microsoft Corporation.
5. `microsoft/segoe-fluent-icons-font` 3.0 — Microsoft platform-use terms reproduced below.

The MIT permission and warranty text above applies to items 1 through 4, with the listed copyright notices.

### Segoe Fluent Icons terms supplied by WPF-UI

> You may use the Segoe and icon fonts, or glyphs included in this file ("Software") solely to design, develop and test your programs that run on a Microsoft Platform, a Microsoft Platform includes but is not limited to any hardware or software product or service branded by trademark, trade dress, copyright or some other recognized means, as a product or service of Microsoft. This license does not grant you the right to distribute or sublicense all or part of the Software to any third party. By using the Software, you agree to these terms. If you do not agree to these terms, do not use the Software.

OneBoard Capture Translate is a Windows-only application. The package does not separately redistribute a standalone Segoe Fluent Icons font file; WPF-UI resources are embedded in the application payload.

## Verification status

The listed direct-package versions and license identifiers were checked against locally restored package metadata. A complete transitive inventory and bundled-license review remains required before release. This notice is provided for compliance information and is not legal advice.
