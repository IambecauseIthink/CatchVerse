# Rokid AR Unity Project

## 项目概述
这是一个基于Unity和Rokid XR SDK的AR项目，实现了3D精灵加载和Apple风格的图片画廊功能。

## 技术栈
- **Unity 2022.3.43f1 LTS**
- **Rokid XR SDK**
- **AR Foundation**
- **XR Interaction Toolkit**
- **DOTween** (动画库)

## 主要功能

### 1. 3D精灵加载系统
- **Creature3DLoader**: 核心加载器，支持异步加载
- **CreatureConfig**: 可配置化精灵设置
- **AR锚点支持**: 自动绑定到地面
- **回退机制**: 模型加载失败时使用备用模型

### 2. Apple风格图片画廊
- **ImageGalleryController**: 仿Apple相册的交互体验
- **触摸/鼠标支持**: 支持滑动翻页和点击
- **动画效果**: 平滑的过渡动画
- **预览功能**: 左右预览图显示

### 3. AR场景管理
- **ARSceneManager**: 统一的AR场景管理
- **AR平面检测**: 支持地面识别和放置
- **相机管理**: 集成AR相机系统

## 使用说明

### 初始设置
1. 导入项目到Unity 2022.3.43f1或更高版本
2. 安装AR Foundation和Rokid XR SDK包
3. 配置Android Build Settings

### 3D精灵加载
```csharp
// 加载精灵
await Creature3DLoader.Instance.LoadCreatureAsync("dragon", Vector3.zero);

// 获取可用精灵列表
List<string> creatures = Creature3DLoader.Instance.GetAvailableCreatures();
```

### 图片画廊
```csharp
// 显示图片画廊
List<Sprite> images = new List<Sprite> { sprite1, sprite2, sprite3 };
ImageGalleryController.Instance.ShowGallery(images);
```

## 文件结构
```
Assets/
├── Scripts/
│   ├── RokidAR/
│   │   ├── Creature3DLoader.cs
│   │   ├── CreatureConfig.cs
│   │   ├── ImageGalleryController.cs
│   │   ├── ARSceneManager.cs
│   │   └── CreatureInstanceData.cs
│   └── Utils/
│       └── TouchInputHandler.cs
├── Prefabs/
├── Models/
├── Scenes/
└── Resources/
    └── Configurations/
```

## 配置说明
在Unity编辑器中：
1. 创建`CreatureConfig` ScriptableObject来配置每个精灵
2. 设置AR Session Origin和必要的AR组件
3. 配置图片画廊的UI元素

## 部署
- 目标平台：Android (Rokid AR眼镜)
- 最低API等级：26 (Android 8.0)
- 推荐API等级：33 (Android 13)