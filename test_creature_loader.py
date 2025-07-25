#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
3D精灵加载模块测试文件
用于验证Creature3DLoader的功能是否正常
"""

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), 'src/modules'))

from creature_3d_loader import Creature3DLoader

# 模拟Rokid SDK（测试环境用）
class MockVector3:
    def __init__(self, x=0, y=0, z=0):
        self.x, self.y, self.z = x, y, z

class MockQuaternion:
    @staticmethod
    def identity():
        return MockQuaternion()

class MockModel3D:
    def __init__(self):
        self.position = MockVector3()
        self.rotation = MockQuaternion()
        self.scale = MockVector3(1, 1, 1)
    
    def set_position(self, pos): self.position = pos
    def set_rotation(self, rot): self.rotation = rot
    def set_scale(self, scale): self.scale = scale

class MockARScene:
    def __init__(self):
        self.objects = []
    
    def add_object(self, obj):
        self.objects.append(obj)
        return True
    
    def remove_object(self, obj):
        if obj in self.objects:
            self.objects.remove(obj)

class MockModelLoader:
    def load_glb(self, path):
        if "fallback" in path:
            return MockModel3D()
        elif not os.path.exists(path):
            raise FileNotFoundError(f"文件不存在: {path}")
        return MockModel3D()

# 替换导入的类
import creature_3d_loader
creature_3d_loader.Vector3 = MockVector3
creature_3d_loader.Quaternion = MockQuaternion
creature_3d_loader.Model3D = MockModel3D
creature_3d_loader.ARScene = MockARScene
creature_3d_loader.ModelLoader = MockModelLoader

def test_creature_loader():
    """测试3D精灵加载器"""
    print("🧪 开始测试3D精灵加载器...")
    
    # 创建测试场景
    mock_scene = MockARScene()
    loader = Creature3DLoader(mock_scene)
    
    # 测试1: 获取可用精灵
    creatures = loader.list_available_creatures()
    print(f"📋 可用精灵: {creatures}")
    assert len(creatures) > 0, "应该至少有一个可用精灵"
    
    # 测试2: 加载存在的精灵
    spawn_pos = MockVector3(0, 0, -2)
    creature = loader.load_creature("dragon", spawn_pos)
    assert creature is not None, "应该成功加载龙精灵"
    print("✅ 龙精灵加载成功")
    
    # 测试3: 加载不存在的精灵
    creature = loader.load_creature("unknown", spawn_pos)
    assert creature is not None, "应该加载备用模型"
    print("✅ 备用模型加载成功")
    
    # 测试4: 卸载精灵
    if creature:
        loader.unload_creature(creature)
        assert len(mock_scene.objects) == 0, "场景应该为空"
        print("✅ 精灵卸载成功")
    
    print("🎉 所有测试通过！")

if __name__ == "__main__":
    test_creature_loader()