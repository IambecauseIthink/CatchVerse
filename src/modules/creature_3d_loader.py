#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
3D精灵加载模块 - Rokid智能眼镜版
负责将3D精灵模型加载到AR场景中，支持.glb格式文件
"""

import os
import logging
from typing import Dict, Optional, Tuple
from dataclasses import dataclass

# Rokid AR SDK 核心类
from rokid.ar import ARScene, ModelLoader, Vector3, Quaternion
from rokid.ar.model import Model3D
from rokid.ar.anchor import Anchor
from rokid.ar.common import ARResult

# 配置日志
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class CreatureConfig:
    """精灵配置数据类"""
    creature_id: str
    model_path: str
    default_scale: float = 0.5
    default_animation: str = "idle"
    is_rare: bool = False
    shadow_enabled: bool = True
    collider_size: Tuple[float, float, float] = (1.0, 1.0, 1.0)

class Creature3DLoader:
    """
    3D精灵加载器
    功能：
    1. 加载.glb格式的3D模型文件
    2. 初始化精灵在AR场景中的位置和姿态
    3. 配置动画、阴影、碰撞体积等属性
    4. 处理加载失败情况，提供备用方案
    """
    
    def __init__(self, ar_scene: ARScene, base_asset_path: str = "assets/models"):
        """
        初始化3D精灵加载器
        
        Args:
            ar_scene: Rokid AR场景实例
            base_asset_path: 3D模型文件的基础路径
        """
        self.ar_scene = ar_scene
        self.model_loader = ModelLoader()
        self.base_asset_path = base_asset_path
        
        # 精灵配置映射
        self.creature_configs: Dict[str, CreatureConfig] = {
            "dragon": CreatureConfig(
                creature_id="dragon",
                model_path="dragon.glb",
                default_scale=0.8,
                default_animation="idle",
                is_rare=True,
                collider_size=(1.2, 1.2, 1.2)
            ),
            "pikachu": CreatureConfig(
                creature_id="pikachu", 
                model_path="pikachu.glb",
                default_scale=0.4,
                default_animation="idle",
                is_rare=False,
                collider_size=(0.8, 0.8, 0.8)
            ),
            "cat": CreatureConfig(
                creature_id="cat",
                model_path="cat.glb", 
                default_scale=0.3,
                default_animation="sit",
                is_rare=False,
                collider_size=(0.6, 0.6, 0.6)
            ),
            "wolf": CreatureConfig(
                creature_id="wolf",
                model_path="wolf.glb",
                default_scale=0.6,
                default_animation="howl",
                is_rare=True,
                collider_size=(1.0, 1.0, 1.0)
            )
        }
        
        # 加载失败的备用模型
        self.fallback_model = "fallback_cube.glb"
        
    def get_model_full_path(self, model_path: str) -> str:
        """获取模型文件的完整路径"""
        return os.path.join(self.base_asset_path, model_path)
    
    def verify_model_file(self, file_path: str) -> bool:
        """验证模型文件是否存在且有效"""
        if not os.path.exists(file_path):
            logger.error(f"模型文件不存在: {file_path}")
            return False
            
        if not file_path.endswith('.glb'):
            logger.error(f"不支持的文件格式: {file_path}")
            return False
            
        return True
    
    def load_creature(self, 
                     creature_id: str, 
                     spawn_position: Vector3,
                     custom_scale: Optional[float] = None,
                     custom_animation: Optional[str] = None) -> Optional[Model3D]:
        """
        加载3D精灵到AR场景
        
        Args:
            creature_id: 精灵类型ID
            spawn_position: 初始位置（基于Rokid空间坐标）
            custom_scale: 自定义缩放比例（可选）
            custom_animation: 自定义动画名称（可选）
            
        Returns:
            Model3D: 加载完成的3D模型对象，失败返回None
        """
        logger.info(f"开始加载精灵: {creature_id}")
        
        # 1. 获取精灵配置
        config = self.creature_configs.get(creature_id)
        if not config:
            logger.error(f"未找到精灵配置: {creature_id}")
            return self._load_fallback_model(spawn_position)
        
        # 2. 获取模型文件路径
        model_path = self.get_model_full_path(config.model_path)
        
        # 3. 验证文件
        if not self.verify_model_file(model_path):
            return self._load_fallback_model(spawn_position)
        
        # 4. 通过Rokid SDK加载3D模型
        try:
            model = self.model_loader.load_glb(model_path)
            logger.info(f"✅ 成功加载3D模型: {model_path}")
            
        except Exception as e:
            logger.error(f"❌ 加载3D模型失败: {e}")
            return self._load_fallback_model(spawn_position)
        
        # 5. 配置模型属性
        self._configure_model(model, config, spawn_position, 
                            custom_scale, custom_animation)
        
        # 6. 添加到AR场景
        try:
            result = self.ar_scene.add_object(model)
            if result == ARResult.SUCCESS:
                logger.info(f"✅ 精灵已添加到AR场景: {creature_id}")
                self._post_load_setup(model, config)
                return model
            else:
                logger.error(f"添加场景失败: {result}")
                return None
                
        except Exception as e:
            logger.error(f"场景添加异常: {e}")
            return None
    
    def _configure_model(self, 
                        model: Model3D, 
                        config: CreatureConfig,
                        spawn_position: Vector3,
                        custom_scale: Optional[float],
                        custom_animation: Optional[str]):
        """配置3D模型属性"""
        
        # 设置位置（绑定到Rokid空间坐标）
        model.set_position(spawn_position)
        
        # 设置旋转（正面朝向用户）
        model.set_rotation(Quaternion.identity())
        
        # 设置缩放比例
        scale = custom_scale if custom_scale else config.default_scale
        model.set_scale(Vector3(scale, scale, scale))
        
        # 绑定到真实地面
        try:
            anchor = Anchor.create_ground_anchor(spawn_position)
            model.set_anchor(anchor)
            logger.info("✅ 已绑定到地面锚点")
        except Exception as e:
            logger.warning(f"地面锚点绑定失败: {e}")
        
        # 设置初始动画
        animation = custom_animation if custom_animation else config.default_animation
        if hasattr(model, 'set_animation') and animation:
            try:
                model.set_animation(animation)
                logger.info(f"✅ 设置动画: {animation}")
            except Exception as e:
                logger.warning(f"动画设置失败: {e}")
    
    def _post_load_setup(self, model: Model3D, config: CreatureConfig):
        """加载完成后的额外配置"""
        
        # 启用阴影渲染
        if config.shadow_enabled and hasattr(model, 'enable_shadows'):
            model.enable_shadows()
            logger.info("✅ 已启用阴影渲染")
        
        # 设置碰撞体积
        try:
            collider_x, collider_y, collider_z = config.collider_size
            model.set_collider("box", 
                             size=Vector3(collider_x, collider_y, collider_z))
            logger.info(f"✅ 设置碰撞体积: {config.collider_size}")
        except Exception as e:
            logger.warning(f"碰撞体积设置失败: {e}")
    
    def _load_fallback_model(self, spawn_position: Vector3) -> Optional[Model3D]:
        """加载备用模型"""
        logger.warning("使用备用模型加载")
        
        fallback_path = self.get_model_full_path(self.fallback_model)
        if not os.path.exists(fallback_path):
            logger.error(f"备用模型也不存在: {fallback_path}")
            return None
        
        try:
            model = self.model_loader.load_glb(fallback_path)
            model.set_position(spawn_position)
            model.set_scale(Vector3(0.3, 0.3, 0.3))  # 缩小备用模型
            model.set_color(1.0, 0.0, 0.0)  # 红色提示
            self.ar_scene.add_object(model)
            return model
        except Exception as e:
            logger.error(f"备用模型加载失败: {e}")
            return None
    
    def unload_creature(self, model: Model3D):
        """卸载3D精灵"""
        try:
            self.ar_scene.remove_object(model)
            logger.info("✅ 精灵已从场景移除")
        except Exception as e:
            logger.error(f"精灵卸载失败: {e}")
    
    def get_creature_config(self, creature_id: str) -> Optional[CreatureConfig]:
        """获取精灵配置"""
        return self.creature_configs.get(creature_id)
    
    def list_available_creatures(self) -> list:
        """获取可用精灵列表"""
        return list(self.creature_configs.keys())

# 使用示例和测试代码
if __name__ == "__main__":
    """
    测试代码：演示如何使用3D精灵加载器
    """
    import time
    
    # 初始化Rokid AR场景
    try:
        ar_scene = ARScene()
        ar_scene.initialize()
        logger.info("✅ AR场景初始化完成")
    except Exception as e:
        logger.error(f"AR场景初始化失败: {e}")
        exit(1)
    
    # 创建加载器
    loader = Creature3DLoader(ar_scene)
    
    # 测试加载所有可用精灵
    available_creatures = loader.list_available_creatures()
    logger.info(f"可用精灵: {available_creatures}")
    
    # 加载示例精灵
    spawn_pos = Vector3(0, 0, -2)  # 前方2米处
    
    for creature_id in available_creatures[:2]:  # 只测试前2个
        logger.info(f"\n--- 测试加载 {creature_id} ---")
        creature = loader.load_creature(creature_id, spawn_pos)
        
        if creature:
            logger.info(f"✅ {creature_id} 加载成功")
            time.sleep(2)  # 等待2秒观察
            loader.unload_creature(creature)
        else:
            logger.error(f"❌ {creature_id} 加载失败")
    
    # 测试场景清理
    logger.info("测试完成，清理场景...")
    ar_scene.cleanup()