# 游戏光标整行翻译助手（Windows 11）

## 本次修改
- 改为识别鼠标所在的**整行英文句子**，不再只翻译单词。
- 按 OCR 识别结果从左到右组合整行文字，再翻译为简体中文。
- 译文窗口位于鼠标光标上方约 **5 毫米**，根据鼠标所在显示器 DPI 换算。
- 长句译文和原文支持自动换行。
- 同一行不重复请求翻译；移到另一行时自动翻译新行。

## 功能
- Windows 自带 OCR（英语）
- 英文 → 简体中文
- 半透明、置顶、鼠标穿透译文窗口
- F8 开始/停止，F9 暂停/恢复，Esc 退出主窗口
- 翻译缓存

## 环境要求
- Windows 11 x64
- .NET 8 SDK
- Windows 英语 OCR 语言功能
- 网络连接（当前版本通过 MyMemory 公共接口翻译）

## 编译
在项目目录打开 PowerShell：
```powershell
dotnet restore
dotnet build -c Release
```

## 发布为单文件 EXE
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

生成文件位于：
`bin\\Release\\net8.0-windows10.0.26100.0\\win-x64\\publish\\GameCursorTranslator.exe`

## 使用
启动程序，点击“开始取词”或按 F8；将鼠标停在某一行英文句子上约 0.3 秒，译文会显示在光标上方约 5 毫米处。F9 暂停/恢复。

## 注意
- 当前版本通过桌面 GDI 截图获取画面；部分独占全屏、HDR、受保护画面或反作弊环境可能阻止截图。
- OCR 识别依赖 Windows 英语 OCR 语言包。
- MyMemory 是公共在线翻译接口，可能限流；本版本不承诺离线可用或无限调用。
- 对复杂字体、动态文字、很小的文字、鼠标指针遮挡文字的情况，识别准确率会下降。
- 5 毫米位置依据鼠标所在显示器 DPI 计算；多显示器混合缩放环境建议实际测试。
