<div align="center">

<img src="src/LiveCaptions-Translator.ico" width="128" height="128" alt="OneBoard Caption Translate 图标"/>

# OneBoard Caption Translate

### *适用于 Windows 的轻量级实时翻译工具。*

[English](README.md) | **中文**

</div>

## 概述

OneBoard Caption Translate 将 Windows 实时字幕连接到您选择的翻译服务，并在主窗口或可配置的悬浮窗中显示译文。

```text
Windows 实时字幕 -> OneBoard Caption Translate -> 翻译悬浮窗
```

- 产品页面：https://oneboard.io.vn/oneboard-caption-translate/
- OneBoard 官方源代码：https://github.com/phat7000/OneBoardCaptionTranslate
- 上游项目：https://github.com/SakiRinn/LiveCaptions-Translator

## 功能

- 捕获 Windows 实时字幕。
- 保留多种翻译服务；Google 可用于快速测试，也支持可配置的 LLM 和传统翻译服务。
- 双区域悬浮窗，默认原文在上、译文在下。
- 支持切换顺序、仅原文和仅译文模式。
- 支持字体大小、粗体、文字颜色、描边、背景颜色和透明度设置。
- 悬浮窗可移动、调整大小，并支持鼠标穿透。
- 支持翻译上下文选项。
- 支持翻译历史和 CSV 导出。
- 支持浅色和深色主题。
- 设置和历史记录保存在应用文件夹之外的用户数据目录中。

最终 v1.0 截图将在产品负责人完成质量验证后添加。早于最终品牌和双区域布局的旧截图不会在此显示。

## 系统要求

- Windows 11 22H2 或更高版本。
- 支持 Windows 实时字幕，并已安装所需的源语言语音包。
- v1.0 发布包适用于 Windows x64。

便携版为自包含版本，无需另行安装 .NET。

## 快速开始

1. 下载便携版 ZIP 或安装程序。
2. 如果使用 ZIP，请先完整解压。
3. 按 **Win + Ctrl + L**；如有提示，请完成 Windows 实时字幕初始化。
4. 配置 Windows 实时字幕的源语言。
5. 在 Windows 实时字幕中选择 **位置** > **覆盖在屏幕上**。
6. 启动 `OneBoardCaptionTranslate.exe`。
7. 选择翻译服务。可使用 Google 进行快速测试。
8. 选择目标语言，例如 `vi-VN`。
9. 如有需要，打开 **Overlay** 悬浮窗。

可在 Windows 实时字幕自身的设置中启用麦克风音频。不同翻译服务可能有各自的网络、账户、API 密钥或用量要求。

## 用户数据

设置和翻译历史保存在：

```text
%LOCALAPPDATA%\OneBoard\OneBoard Caption Translate\
```

删除便携版文件夹或卸载应用不会自动删除 LocalAppData 中的设置和历史记录。

## 更新和发布真实性

v1.0 已禁用自动更新。请只从 OneBoard 官方源代码仓库或产品页面获取发布包，并核对发布的 SHA256。

v1.0 Windows 二进制文件未进行代码签名，Windows SmartScreen 可能显示警告。

## 源代码、许可和上游署名

OneBoard 官方源代码仓库：

https://github.com/phat7000/OneBoardCaptionTranslate

OneBoard Caption Translate 基于采用 Apache License 2.0 的 [SakiRinn/LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator)。本产品包含修改，并非上游项目的官方发布；上游作者不对此修改产品表示认可或背书。

请参阅 [LICENSE](LICENSE)、[UPSTREAM_ATTRIBUTION.md](UPSTREAM_ATTRIBUTION.md) 和 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
