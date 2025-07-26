using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

namespace RokidAR
{
    public class ARSceneManager : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARSessionOrigin arSessionOrigin;
        [SerializeField] private ARRaycastManager arRaycastManager;
        [SerializeField] private ARPlaneManager arPlaneManager;
        [SerializeField] private ARAnchorManager arAnchorManager;
        
        [Header("UI References")]
        [SerializeField] private ImageGalleryController imageGallery;
        [SerializeField] private Canvas uiCanvas;
        
        [Header("Creature Management")]
        [SerializeField] private Creature3DLoader creatureLoader;
        [SerializeField] private List<CreatureConfig> defaultCreatures = new List<CreatureConfig>();
        
        [Header("Gallery Images")]
        [SerializeField] private List<Sprite> gallerySprites = new List<Sprite>();
        
        public static ARSceneManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeAR();
            SetupUI();
            SetupCreatureLoader();
        }
        
        private void InitializeAR()
        {
            if (arSession == null)
                arSession = FindObjectOfType<ARSession>();
            
            if (arSessionOrigin == null)
                arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            
            if (arRaycastManager == null)
                arRaycastManager = FindObjectOfType<ARRaycastManager>();
            
            if (arPlaneManager == null)
                arPlaneManager = FindObjectOfType<ARPlaneManager>();
            
            Debug.Log("AR Scene Manager initialized");
        }
        
        private void SetupUI()
        {
            if (imageGallery == null)
            {
                var galleryGO = new GameObject("ImageGallery");
                imageGallery = galleryGO.AddComponent<ImageGalleryController>();
            }
        }
        
        private void SetupCreatureLoader()
        {
            if (creatureLoader == null)
            {
                var loaderGO = new GameObject("CreatureLoader");
                creatureLoader = loaderGO.AddComponent<Creature3DLoader>();
                
                // Set up AR references
                creatureLoader.GetType().GetField("arRaycastManager", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(creatureLoader, arRaycastManager);
                    
                creatureLoader.GetType().GetField("arPlaneManager", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(creatureLoader, arPlaneManager);
                    
                creatureLoader.GetType().GetField("arSessionOrigin", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(creatureLoader, arSessionOrigin);
            }
        }
        
        public void ShowImageGallery()
        {
            if (imageGallery != null && gallerySprites.Count > 0)
            {
                imageGallery.ShowGallery(gallerySprites);
            }
        }
        
        public void SpawnCreature(string creatureId)
        {
            if (creatureLoader != null)
            {
                _ = creatureLoader.LoadCreatureAsync(creatureId);
            }
        }
        
        public void ResetARSession()
        {
            if (arSession != null)
            {
                arSession.Reset();
            }
        }
        
        public void TogglePlaneVisualization(bool visible)
        {
            if (arPlaneManager != null)
            {
                arPlaneManager.enabled = visible;
                foreach (var plane in arPlaneManager.trackables)
                {
                    plane.gameObject.SetActive(visible);
                }
            }
        }
        
        public Camera GetARCamera()
        {
            return arSessionOrigin?.camera ?? Camera.main;
        }
        
        public List<string> GetAvailableCreatures()
        {
            return creatureLoader?.GetAvailableCreatures() ?? new List<string>();
        }
    }
}