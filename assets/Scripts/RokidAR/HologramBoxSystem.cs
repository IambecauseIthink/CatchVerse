using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace RokidAR
{
    public class HologramBoxSystem : MonoBehaviour
    {
        [Header("Hologram Box Settings")]
        [SerializeField] private Transform hologramBoxTransform;
        [SerializeField] private GameObject hologramBoxPrefab;
        [SerializeField] private Vector3 boxSize = new Vector3(0.3f, 0.3f, 0.3f);
        [SerializeField] private Material boxMaterial;
        [SerializeField] private Color activeColor = Color.cyan;
        [SerializeField] private Color idleColor = Color.gray;
        
        [Header("Android Communication")]
        [SerializeField] private string androidDeviceIP = "192.168.1.100";
        [SerializeField] private int androidPort = 8080;
        [SerializeField] private string apiEndpoint = "/api/creature";
        
        [Header("Projection Settings")]
        [SerializeField] private float projectionDistance = 3f;
        [SerializeField] private float projectionHeight = 1.5f;
        [SerializeField] private LayerMask projectionLayerMask = -1;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem activationEffect;
        [SerializeField] private LineRenderer projectionBeam;
        [SerializeField] private GameObject hologramDisplay;
        
        public static HologramBoxSystem Instance { get; private set; }
        
        private GameObject currentHologramBox;
        private bool isActive = false;
        private Queue<CreatureProjectionData> projectionQueue = new Queue<CreatureProjectionData>();
        
        [System.Serializable]
        public class CreatureProjectionData
        {
            public string creatureId;
            public string creatureName;
            public string modelPath;
            public Vector3 targetPosition;
            public Quaternion targetRotation;
            public Vector3 originalScale;
            public string animationData;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupHologramBox();
                SetupProjectionSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            StartCoroutine(InitializeAndroidConnection());
        }
        
        private void SetupHologramBox()
        {
            if (currentHologramBox == null)
            {
                if (hologramBoxPrefab != null)
                {
                    currentHologramBox = Instantiate(hologramBoxPrefab, transform);
                }
                else
                {
                    currentHologramBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    currentHologramBox.name = "HologramBox";
                    currentHologramBox.transform.SetParent(transform);
                    currentHologramBox.transform.localScale = boxSize;
                    
                    var renderer = currentHologramBox.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = boxMaterial;
                    }
                }
                
                currentHologramBox.SetActive(false);
            }
        }
        
        private void SetupProjectionSystem()
        {
            if (projectionBeam == null)
            {
                GameObject beamObj = new GameObject("ProjectionBeam");
                beamObj.transform.SetParent(transform);
                projectionBeam = beamObj.AddComponent<LineRenderer>();
                
                projectionBeam.material = new Material(Shader.Find("Sprites/Default"));
                projectionBeam.startWidth = 0.02f;
                projectionBeam.endWidth = 0.02f;
                projectionBeam.positionCount = 2;
                projectionBeam.enabled = false;
            }
        }
        
        public void ActivateHologramBox(Vector3 position, Quaternion rotation)
        {
            if (currentHologramBox != null)
            {
                currentHologramBox.transform.position = position;
                currentHologramBox.transform.rotation = rotation;
                currentHologramBox.SetActive(true);
                
                isActive = true;
                UpdateBoxVisual(true);
                
                if (activationEffect != null)
                {
                    activationEffect.transform.position = position;
                    activationEffect.Play();
                }
            }
        }
        
        public void DeactivateHologramBox()
        {
            if (currentHologramBox != null)
            {
                currentHologramBox.SetActive(false);
                isActive = false;
                
                if (activationEffect != null)
                {
                    activationEffect.Stop();
                }
            }
        }
        
        public void ProjectCreatureToBox(GameObject creature, Vector3 throwDirection)
        {
            if (creature == null) return;
            
            var creatureData = creature.GetComponent<CreatureInstanceData>();
            if (creatureData == null) return;
            
            CreatureProjectionData projectionData = new CreatureProjectionData
            {
                creatureId = creatureData.GetCreatureId(),
                creatureName = creatureData.Config != null ? creatureData.Config.displayName : "Unknown",
                modelPath = creatureData.Config != null ? creatureData.Config.modelPrefab.name : "fallback",
                targetPosition = CalculateProjectionTarget(throwDirection),
                targetRotation = Quaternion.identity,
                originalScale = creature.transform.localScale,
                animationData = "idle"
            };
            
            StartCoroutine(ProjectCreatureSequence(projectionData, creature));
        }
        
        private Vector3 CalculateProjectionTarget(Vector3 throwDirection)
        {
            if (currentHologramBox == null || !isActive)
            {
                return transform.position + throwDirection * projectionDistance;
            }
            
            return currentHologramBox.transform.position;
        }
        
        private IEnumerator ProjectCreatureSequence(CreatureProjectionData data, GameObject creature)
        {
            // Show projection beam
            if (projectionBeam != null)
            {
                projectionBeam.enabled = true;
                projectionBeam.SetPosition(0, creature.transform.position);
                projectionBeam.SetPosition(1, data.targetPosition);
                
                // Animate beam
                Color beamColor = Color.cyan;
                beamColor.a = 0.5f;
                projectionBeam.startColor = beamColor;
                projectionBeam.endColor = beamColor;
            }
            
            // Send to Android device
            yield return StartCoroutine(SendToAndroidDevice(data));
            
            // Hide beam
            if (projectionBeam != null)
            {
                projectionBeam.enabled = false;
            }
            
            // Clean up creature in Rokid glasses
            Destroy(creature);
        }
        
        private IEnumerator SendToAndroidDevice(CreatureProjectionData data)
        {
            string jsonData = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            string url = $"http://{androidDeviceIP}:{androidPort}{apiEndpoint}";
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Successfully sent creature {data.creatureId} to Android device");
                }
                else
                {
                    Debug.LogError($"Failed to send creature to Android device: {request.error}");
                }
            }
        }
        
        private IEnumerator InitializeAndroidConnection()
        {
            string healthUrl = $"http://{androidDeviceIP}:{androidPort}/api/health";
            
            using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Android device connection established");
                }
                else
                {
                    Debug.LogWarning("Could not connect to Android device, will retry");
                }
            }
        }
        
        public void SetAndroidDeviceIP(string ip, int port = 8080)
        {
            androidDeviceIP = ip;
            androidPort = port;
        }
        
        public Vector3 GetHologramBoxPosition()
        {
            return currentHologramBox != null ? currentHologramBox.transform.position : Vector3.zero;
        }
        
        public bool IsActive()
        {
            return isActive;
        }
        
        public void UpdateBoxVisual(bool active)
        {
            if (currentHologramBox != null)
            {
                var renderer = currentHologramBox.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = active ? activeColor : idleColor;
                }
            }
        }
    }
}