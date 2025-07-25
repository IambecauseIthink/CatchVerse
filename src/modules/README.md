# 3D精灵加载模块使用说明

## 模块功能
该模块负责将3D精灵模型加载到Rokid AR场景中，支持.glb格式文件，提供完整的模型初始化功能。

## 文件结构
```
src/modules/
├── creature_3d_loader.py    # 主模块文件
└── README.md               # 使用说明
```

## 快速开始

### 1. 基础使用
```python
from src.modules.creature_3d_loader import Creature3DLoader
from rokid.ar import ARScene, Vector3

# 初始化AR场景
ar_scene = ARScene()

# 创建加载器
loader = Creature3DLoader(ar_scene)

# 加载精灵
creature = loader.load_creature("dragon", Vector3(0, 0, -2))
```

### 2. 支持的精灵类型
- dragon: 龙（稀有）
- pikachu: 皮卡丘
- cat: 猫
- wolf: 狼（稀有）

### 3. 自定义配置
```python
# 使用自定义缩放和动画
creature = loader.load_creature(
    "pikachu", 
    Vector3(1, 0, -3), 
    custom_scale=0.8,
    custom_animation="run"
)
```

## 依赖要求
- Python 3.7+
- Rokid AR SDK
- Unity3D环境（开发时）

## 测试
运行测试文件验证功能：
```bash
python test_creature_loader.py
```

## 注意事项
1. 所有3D模型文件必须放在 `assets/models/` 目录下
2. 文件格式必须为.glb
3. 确保模型有对应的动画名称