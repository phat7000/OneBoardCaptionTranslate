<div align="center">

<img src="src/LiveCaptions-Translator.ico" width="128" height="128" alt="OneBoard Capture Translate 图标"/>

# OneBoard Capture Translate

### *适用于 Windows 的轻量级实时翻译工具。*

[English](README.md) | **中文**

</div>

## 概述

OneBoard Capture Translate 将手动选择的语音识别服务连接到独立选择的翻译服务，并在主窗口或可配置的悬浮窗中显示结果。

```text
语音识别服务 -> OneBoard Capture Translate -> 翻译服务 -> 自适应显示
```

- 产品页面：https://oneboard.io.vn/
- OneBoard 官方源代码：https://github.com/phat7000/OneBoardCaptionTranslate
- 上游项目：https://github.com/SakiRinn/LiveCaptions-Translator

## 功能

- 支持 Windows 实时字幕、Azure Speech 和 Google Cloud Speech-to-Text，并由用户明确选择。
- 保留现有翻译服务，并新增 Microsoft Translator、Google Cloud Translation Basic v2、TranslatePlus 和 Langbly。
- 包含 87 种语言的规范目录，以及按服务映射的翻译代码和 BCP-47 语音区域代码。
- 原文和译文区域自适应高度、底部对齐，并稳定更新临时识别结果。
- 双区域悬浮窗，默认原文在上、译文在下。
- 支持切换顺序、仅原文和仅译文模式。
- 支持字体大小、粗体、文字颜色、描边、背景颜色和透明度设置。
- 悬浮窗可移动、调整大小，并支持鼠标穿透。
- 支持翻译上下文选项。
- 支持翻译历史和 CSV 导出。
- 支持浅色和深色主题。
- 设置和历史记录保存在应用文件夹之外的用户数据目录中。

截图将在产品负责人完成质量验证后添加。早于 Capture 品牌和自适应布局的旧截图不会在此显示。

## 系统要求

- Windows 11 22H2 或更高版本。
- 选择 Windows 实时字幕时，需要系统支持并安装相应语音包。
- 选择 Azure Speech 或 Google Speech 时，需要麦克风和相应服务凭据。
- CI 构建支持 Windows x64 和 ARM64。

便携版为自包含版本，无需另行安装 .NET。

## 快速开始

1. 下载便携版 ZIP 或安装程序。
2. 如果使用 ZIP，请先完整解压。
3. 启动 `OneBoardCaptureTranslate.exe`。
4. 选择语音识别服务和源语言；若使用 Windows 实时字幕，请按提示完成系统设置。
5. 选择翻译服务和目标语言。
6. 打开 **Provider Settings**，仅填写所选云服务需要的凭据。
7. 如有需要，打开 **Overlay** 悬浮窗。

可在 Windows 实时字幕自身的设置中启用麦克风音频。不同翻译服务可能有各自的网络、账户、API 密钥或用量要求。

## 用户数据

设置和翻译历史保存在：

```text
%LOCALAPPDATA%\OneBoard\OneBoard Capture Translate\
```

删除便携版文件夹或卸载应用不会自动删除 LocalAppData 中的设置和历史记录。

## 更新和发布真实性

v1.1 已禁用自动更新。请只从 OneBoard 官方源代码仓库或产品页面获取发布包，并核对发布的 SHA256。

Windows 二进制文件未进行代码签名，Windows SmartScreen 可能显示警告。

## 源代码、许可和上游署名

OneBoard 官方源代码仓库：

https://github.com/phat7000/OneBoardCaptionTranslate

OneBoard Capture Translate 基于采用 Apache License 2.0 的 [SakiRinn/LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator)。本产品包含修改，并非上游项目的官方发布；上游作者不对此修改产品表示认可或背书。

请参阅 [LICENSE](LICENSE)、[UPSTREAM_ATTRIBUTION.md](UPSTREAM_ATTRIBUTION.md) 和 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
