using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RokidAR
{
    public class Creature3DLoader : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARRaycastManager arRaycastManager;
        [SerializeField] private ARPlaneManager arPlaneManager;
        [SerializeField] private ARSessionOrigin arSessionOrigin;
        
        [Header("Configuration")]
        [SerializeField] private List<CreatureConfig> creatureConfigs = new List<CreatureConfig>();
        [SerializeField] private GameObject fallbackPrefab;
        
        [Header("Settings")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool autoPlaceOnGround = true;
        [SerializeField] private LayerMask placementLayerMask = -1;
        
        private Dictionary<string, CreatureConfig> configDict = new Dictionary<string, CreatureConfig>();
        private List<GameObject> activeCreatures = new List<GameObject>();
        private Camera arCamera;
        
        public static Creature3DLoader Instance { get; private set; }
        
        public event Action<GameObject> OnCreatureLoaded;
        public event Action<GameObject> OnCreatureUnloaded;
        public event Action<string> OnLoadingError;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeConfigDict();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            arCamera = arSessionOrigin?.camera ?? Camera.main;
            Log("Creature3DLoader initialized");
        }
        
        private void InitializeConfigDict()
        {
            foreach (var config in creatureConfigs)
            {
                if (!string.IsNullOrEmpty(config.creatureId))
                {
                    configDict[config.creatureId] = config;
                }
            }
        }
        
        public async Task<GameObject> LoadCreatureAsync(
            string creatureId, 
            Vector3? targetPosition = null,
            Vector3? customScale = null,
            string customAnimation = null)
        {
            if (!configDict.TryGetValue(creatureId, out var config))
            {
                LogError($"Creature config not found: {creatureId}");
                OnLoadingError?.Invoke($"Creature config not found: {creatureId}");
                return await LoadFallbackModelAsync(targetPosition ?? Vector3.zero);
            }
            
            try
            {
                Vector3 spawnPosition = targetPosition ?? await FindPlacementPositionAsync(config);
                GameObject creature = await SpawnCreatureAsync(config, spawnPosition, customScale, customAnimation);
                
                if (creature != null)
                {
                    activeCreatures.Add(creature);
                    OnCreatureLoaded?.Invoke(creature);
                    Log($"Successfully loaded creature: {creatureId}");
                }
                
                return creature;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load creature {creatureId}: {ex.Message}");
                OnLoadingError?.Invoke($"Failed to load creature: {ex.Message}");
                return await LoadFallbackModelAsync(targetPosition ?? Vector3.zero);
            }
        }
        
        private async Task<GameObject> SpawnCreatureAsync(
            CreatureConfig config, 
            Vector3 position, 
            Vector3? scale, 
            string animation)
        {
            GameObject modelPrefab = config.modelPrefab ?? config.fallbackPrefab;
            if (modelPrefab == null)
            {
                modelPrefab = fallbackPrefab;
            }
            
            if (modelPrefab == null)
            {
                LogError("No valid prefab found for creature");
                return null;
            }
            
            GameObject creature = Instantiate(modelPrefab, position, config.defaultRotation);
            
            // Apply scale
            Vector3 finalScale = scale ?? config.defaultScale;
            creature.transform.localScale = finalScale;
            
            // Configure components
            ConfigureCreature(creature, config, animation);
            
            return creature;
        }
        
        private void ConfigureCreature(GameObject creature, CreatureConfig config, string animation)
        {
            // Add AR anchor if needed
            if (config.requireGroundPlane)
            {
                var anchor = creature.AddComponent<ARAnchor>();
            }
            
            // Configure physics
            if (config.useRigidbody)
            {
                var rigidbody = creature.GetComponent<Rigidbody>() ?? creature.AddComponent<Rigidbody>();
                rigidbody.mass = config.mass;
                rigidbody.isKinematic = true; // AR objects usually kinematic
            }
            
            // Configure collider
            var collider = creature.GetComponent<Collider>() ?? creature.AddComponent<BoxCollider>();
            if (collider is BoxCollider boxCollider)
            {
                boxCollider.size = config.colliderSize;
                boxCollider.center = config.colliderCenter;
            }
            
            // Configure renderer settings
            var renderers = creature.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = config.enableShadows ? 
                    UnityEngine.Rendering.ShadowCastingMode.On : 
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                
                renderer.receiveShadows = config.enableShadows;
            }
            
            // Handle animation
            if (!string.IsNullOrEmpty(animation) || !string.IsNullOrEmpty(config.defaultAnimation))
            {
                var animator = creature.GetComponent<Animator>();
                if (animator != null)
                {
                    string animToPlay = animation ?? config.defaultAnimation;
                    animator.Play(animToPlay);
                }
            }
            
            // Add creature metadata
            var creatureData = creature.AddComponent<CreatureInstanceData>();
            creatureData.Initialize(config);
        }
        
        private async Task<Vector3> FindPlacementPositionAsync(CreatureConfig config)
        {
            if (!autoPlaceOnGround || arRaycastManager == null)
            {
                return arCamera.transform.position + arCamera.transform.forward * 2f;
            }
            
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            
            if (arRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes))
            {
                Vector3 hitPoint = hits[0].pose.position;
                
                // Validate distance
                float distance = Vector3.Distance(arCamera.transform.position, hitPoint);
                if (distance < config.minPlacementDistance || distance > config.maxPlacementDistance)
                {
                    return arCamera.transform.position + arCamera.transform.forward * 
                           Mathf.Clamp(distance, config.minPlacementDistance, config.maxPlacementDistance);
                }
                
                return hitPoint;
            }
            
            // Fallback to camera forward
            return arCamera.transform.position + arCamera.transform.forward * 1.5f;
        }
        
        private async Task<GameObject> LoadFallbackModelAsync(Vector3 position)
        {
            Log("Loading fallback model");
            
            if (fallbackPrefab == null)
            {
                // Create basic cube as ultimate fallback
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.transform.position = position;
                fallback.transform.localScale = Vector3.one * 0.3f;
                
                var renderer = fallback.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }
                
                return fallback;
            }
            
            GameObject fallbackCreature = Instantiate(fallbackPrefab, position, Quaternion.identity);
            return fallbackCreature;
        }
        
        public void UnloadCreature(GameObject creature)
        {
            if (creature != null && activeCreatures.Contains(creature))
            {
                activeCreatures.Remove(creature);
                Destroy(creature);
                OnCreatureUnloaded?.Invoke(creature);
                Log("Creature unloaded");
            }
        }
        
        public void UnloadAllCreatures()
        {
            foreach (var creature in new List<GameObject>(activeCreatures))
            {
                UnloadCreature(creature);
            }
        }
        
        public List<string> GetAvailableCreatures()
        {
            return new List<string>(configDict.Keys);
        }
        
        public CreatureConfig GetCreatureConfig(string creatureId)
        {
            return configDict.TryGetValue(creatureId, out var config) ? config : null;
        }
        
        public List<GameObject> GetActiveCreatures()
        {
            return new List<GameObject>(activeCreatures);
        }
        
        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[Creature3DLoader] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[Creature3DLoader] {message}");
        }
    }
}