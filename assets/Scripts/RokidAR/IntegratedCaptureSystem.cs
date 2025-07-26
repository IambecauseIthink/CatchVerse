using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Events;

namespace RokidAR
{
    public class IntegratedCaptureSystem : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private ImageGalleryController imageGallery;
        [SerializeField] private VideoPlaybackSystem videoSystem;
        [SerializeField] private Creature3DLoader creatureLoader;
        [SerializeField] private HandGestureRecognition handRecognition;
        [SerializeField] private CaptureSystem captureSystem;
        [SerializeField] private HologramBoxSystem hologramSystem;
        
        [Header("Gallery Settings")]
        [SerializeField] private List<Sprite> galleryImages = new List<Sprite>();
        [SerializeField] private Transform galleryAnchor;
        [SerializeField] private float galleryShowDistance = 0.5f;
        
        [Header("Capture Settings")]
        [SerializeField] private int maxCreaturesInScene = 5;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private LayerMask spawnLayerMask = -1;
        
        [Header("Controller Settings")]
        [SerializeField] private float controllerActivationDistance = 0.3f;
        [SerializeField] private KeyCode galleryToggleKey = KeyCode.Tab;
        
        public static IntegratedCaptureSystem Instance { get; private set; }
        
        public UnityEvent<GameObject> OnCreatureCaptured;
        public UnityEvent<GameObject> OnCreatureProjected;
        public UnityEvent OnGalleryExpanded;
        public UnityEvent OnGalleryCollapsed;
        
        private Dictionary<string, GameObject> activeCreatures = new Dictionary<string, GameObject>();
        private bool galleryExpanded = false;
        private bool isProcessingCapture = false;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetupPersistentGallery();
            SpawnInitialCreatures();
        }
        
        private void Update()
        {
            HandleControllerInput();
            UpdateGalleryState();
        }
        
        private void InitializeSystems()
        {
            // Ensure all systems are initialized
            if (imageGallery == null)
                imageGallery = FindObjectOfType<ImageGalleryController>();
                
            if (videoSystem == null)
                videoSystem = FindObjectOfType<VideoPlaybackSystem>();
                
            if (creatureLoader == null)
                creatureLoader = FindObjectOfType<Creature3DLoader>();
                
            if (handRecognition == null)
                handRecognition = FindObjectOfType<HandGestureRecognition>();
                
            if (captureSystem == null)
                captureSystem = FindObjectOfType<CaptureSystem>();
                
            if (hologramSystem == null)
                hologramSystem = FindObjectOfType<HologramBoxSystem>();
            
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            // Gallery events
            imageGallery.OnGalleryClosed.AddListener(() => {
                galleryExpanded = false;
                OnGalleryCollapsed?.Invoke();
            });
            
            // Hand gesture events
            handRecognition.OnGrabTarget.AddListener(OnCreatureGrabbed);
            handRecognition.OnThrowTarget.AddListener(OnCreatureThrown);
            
            // Capture events
            captureSystem.OnCaptureSuccess.AddListener(OnCreatureSuccessfullyCaptured);
            
            // Hologram system events
            hologramSystem.OnCreatureProjected.AddListener(OnCreatureProjectedToAndroid);
        }
        
        private void SetupPersistentGallery()
        {
            if (galleryAnchor != null && imageGallery != null)
            {
                // Position gallery at a fixed location relative to user
                Vector3 galleryPosition = galleryAnchor.position + Vector3.up * 1.5f;
                imageGallery.transform.position = galleryPosition;
                imageGallery.transform.LookAt(Camera.main.transform);
                imageGallery.ShowGallery(galleryImages);
            }
        }
        
        private async void SpawnInitialCreatures()
        {
            var availableCreatures = creatureLoader?.GetAvailableCreatures();
            if (availableCreatures == null || availableCreatures.Count == 0) return;
            
            int spawnCount = Mathf.Min(availableCreatures.Count, maxCreaturesInScene);
            
            for (int i = 0; i < spawnCount; i++)
            {
                string creatureId = availableCreatures[i % availableCreatures.Count];
                Vector3 spawnPosition = GenerateSpawnPosition();
                
                var creature = await creatureLoader.LoadCreatureAsync(creatureId, spawnPosition);
                if (creature != null)
                {
                    SetupCreatureBehavior(creature);
                    activeCreatures[creatureId] = creature;
                }
            }
        }
        
        private Vector3 GenerateSpawnPosition()
        {
            Vector3 playerPos = Camera.main.transform.position;
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            
            Vector3 spawnPos = playerPos + new Vector3(randomCircle.x, 0, randomCircle.y);
            spawnPos.y = Random.Range(0.5f, 2f);
            
            return spawnPos;
        }
        
        private void SetupCreatureBehavior(GameObject creature)
        {
            var movement = creature.GetComponent<CreatureMovement>();
            if (movement == null)
            {
                movement = creature.AddComponent<CreatureMovement>();
            }
            
            movement.SetWanderArea(creature.transform.position, spawnRadius);
            movement.OnEscapeTriggered.AddListener(() => OnCreatureEscape(creature));
        }
        
        private void HandleControllerInput()
        {
            // Toggle gallery with controller or keyboard
            if (Input.GetKeyDown(galleryToggleKey) || 
                Input.GetButtonDown("Fire1") || 
                IsControllerNearGallery())
            {
                ToggleGallery();
            }
        }
        
        private bool IsControllerNearGallery()
        {
            if (galleryAnchor == null) return false;
            
            // Check if controller (or mouse) is near gallery
            Vector3 controllerPos = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
            float distance = Vector3.Distance(controllerPos, galleryAnchor.position);
            return distance < controllerActivationDistance;
        }
        
        private void ToggleGallery()
        {
            galleryExpanded = !galleryExpanded;
            
            if (galleryExpanded)
            {
                ExpandGallery();
            }
            else
            {
                CollapseGallery();
            }
        }
        
        private void ExpandGallery()
        {
            if (imageGallery != null)
            {
                imageGallery.gameObject.SetActive(true);
                OnGalleryExpanded?.Invoke();
            }
        }
        
        private void CollapseGallery()
        {
            if (imageGallery != null)
            {
                imageGallery.HideGallery();
                OnGalleryCollapsed?.Invoke();
            }
        }
        
        private void UpdateGalleryState()
        {
            // Update gallery position to follow user
            if (galleryAnchor != null && imageGallery != null)
            {
                Vector3 targetPos = galleryAnchor.position + Vector3.up * 1.5f;
                imageGallery.transform.position = targetPos;
                imageGallery.transform.LookAt(Camera.main.transform);
            }
        }
        
        public void StartCaptureSequence(string creatureId)
        {
            if (isProcessingCapture) return;
            
            string videoName = creatureId;
            videoSystem.PlayVideo(videoName);
            
            videoSystem.OnVideoComplete += OnVideoComplete;
            videoSystem.OnStarlightComplete += OnStarlightComplete;
        }
        
        private void OnVideoComplete()
        {
            // Video finished, prepare creature spawn
        }
        
        private void OnStarlightComplete()
        {
            // Spawn creature after starlight effect
            SpawnCreatureForCapture();
            
            videoSystem.OnVideoComplete -= OnVideoComplete;
            videoSystem.OnStarlightComplete -= OnStarlightComplete;
        }
        
        private async void SpawnCreatureForCapture()
        {
            Vector3 spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 2f;
            var creature = await creatureLoader.LoadCreatureAsync("current", spawnPosition);
            
            if (creature != null)
            {
                SetupCreatureForCapture(creature);
            }
        }
        
        private void SetupCreatureForCapture(GameObject creature)
        {
            var movement = creature.GetComponent<CreatureMovement>();
            if (movement != null)
            {
                movement.SetDetectionRadius(3f);
                movement.OnEscapeTriggered.AddListener(() => OnCreatureEscape(creature));
            }
            
            captureSystem.EnterCaptureMode(creature);
        }
        
        private void OnCreatureGrabbed(GameObject creature)
        {
            // Creature grabbed by hand gesture
            Debug.Log($"Creature {creature.name} grabbed");
        }
        
        private void OnCreatureThrown(GameObject creature, Vector3 throwDirection)
        {
            // Creature thrown towards hologram box
            if (creature != null)
            {
                hologramSystem.ProjectCreatureToBox(creature, throwDirection);
                
                // Remove from active creatures
                var creatureData = creature.GetComponent<CreatureInstanceData>();
                if (creatureData != null)
                {
                    activeCreatures.Remove(creatureData.GetCreatureId());
                }
            }
        }
        
        private void OnCreatureSuccessfullyCaptured(GameObject creature)
        {
            OnCreatureCaptured?.Invoke(creature);
            
            // Immediately project to Android device
            Vector3 throwDirection = Vector3.forward;
            hologramSystem.ProjectCreatureToBox(creature, throwDirection);
            
            // Remove from active creatures
            var creatureData = creature.GetComponent<CreatureInstanceData>();
            if (creatureData != null)
            {
                activeCreatures.Remove(creatureData.GetCreatureId());
            }
        }
        
        private void OnCreatureProjectedToAndroid(GameObject creature)
        {
            OnCreatureProjected?.Invoke(creature);
            
            // Creature disappears from Rokid glasses
            if (creature != null)
            {
                Destroy(creature);
            }
        }
        
        private void OnCreatureEscape(GameObject creature)
        {
            // Handle creature escape
            var creatureData = creature.GetComponent<CreatureInstanceData>();
            if (creatureData != null)
            {
                Debug.Log($"Creature {creatureData.GetCreatureId()} escaped");
            }
        }
        
        public void AddCreatureToScene(string creatureId)
        {
            if (activeCreatures.Count >= maxCreaturesInScene) return;
            
            StartCoroutine(AddCreatureCoroutine(creatureId));
        }
        
        private async System.Collections.IEnumerator AddCreatureCoroutine(string creatureId)
        {
            Vector3 spawnPosition = GenerateSpawnPosition();
            var creature = await creatureLoader.LoadCreatureAsync(creatureId, spawnPosition);
            
            if (creature != null)
            {
                SetupCreatureBehavior(creature);
                activeCreatures[creatureId] = creature;
            }
            
            yield return null;
        }
        
        public List<string> GetActiveCreatures()
        {
            return new List<string>(activeCreatures.Keys);
        }
        
        public void SetAndroidDeviceIP(string ip, int port = 8080)
        {
            hologramSystem.SetAndroidDeviceIP(ip, port);
        }
        
        public void ActivateHologramBox(Vector3 position, Quaternion rotation)
        {
            hologramSystem.ActivateHologramBox(position, rotation);
        }
        
        public void DeactivateHologramBox()
        {
            hologramSystem.DeactivateHologramBox();
        }
    }
}