using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RokidAR
{
    public class CreatureSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private List<GameObject> creaturePrefabs = new List<GameObject>();
        [SerializeField] private Transform spawnAreaCenter;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private int maxCreatures = 10;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private LayerMask spawnLayerMask = -1;
        
        [Header("Randomization")]
        [SerializeField] private bool randomizeHeight = true;
        [SerializeField] private float minHeight = 0.5f;
        [SerializeField] private float maxHeight = 3f;
        [SerializeField] private bool randomizeScale = true;
        [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        
        [Header("Path Generation")]
        [SerializeField] private bool useProceduralPaths = true;
        [SerializeField] private ProceduralPathMovement pathMovementTemplate;
        
        public static CreatureSpawner Instance { get; private set; }
        
        private List<GameObject> activeCreatures = new List<GameObject>();
        private Transform playerTransform;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SetupPlayerTransform();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            StartCoroutine(SpawnCreaturesRoutine());
        }
        
        private void SetupPlayerTransform()
        {
            var arSession = FindObjectOfType<ARSessionOrigin>();
            if (arSession != null)
            {
                playerTransform = arSession.camera.transform;
            }
            else
            {
                playerTransform = Camera.main.transform;
            }
        }
        
        private IEnumerator SpawnCreaturesRoutine()
        {
            while (true)
            {
                if (activeCreatures.Count < maxCreatures && creaturePrefabs.Count > 0)
                {
                    SpawnRandomCreature();
                }
                
                yield return new WaitForSeconds(spawnInterval);
            }
        }
        
        public GameObject SpawnRandomCreature()
        {
            if (creaturePrefabs.Count == 0 || activeCreatures.Count >= maxCreatures)
                return null;
            
            GameObject prefab = creaturePrefabs[Random.Range(0, creaturePrefabs.Count)];
            Vector3 spawnPosition = GenerateRandomSpawnPosition();
            
            return SpawnCreature(prefab, spawnPosition);
        }
        
        public GameObject SpawnCreature(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;
            
            GameObject creature = Instantiate(prefab, position, Quaternion.identity);
            
            // Apply randomization
            ApplyRandomTransform(creature);
            
            // Add procedural movement
            if (useProceduralPaths)
            {
                AddProceduralMovement(creature);
            }
            
            // Setup creature components
            SetupCreatureComponents(creature);
            
            activeCreatures.Add(creature);
            return creature;
        }
        
        private Vector3 GenerateRandomSpawnPosition()
        {
            if (playerTransform == null) return transform.position;
            
            // Generate random position around player
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Add height variation
            if (randomizeHeight)
            {
                spawnPos.y = Random.Range(minHeight, maxHeight);
            }
            else
            {
                spawnPos.y = transform.position.y;
            }
            
            return spawnPos;
        }
        
        private void ApplyRandomTransform(GameObject creature)
        {
            // Random scale
            if (randomizeScale)
            {
                float randomScale = Random.Range(scaleRange.x, scaleRange.y);
                creature.transform.localScale = Vector3.one * randomScale;
            }
            
            // Random rotation
            creature.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }
        
        private void AddProceduralMovement(GameObject creature)
        {
            if (pathMovementTemplate != null)
            {
                var pathMovement = creature.AddComponent<ProceduralPathMovement>();
                pathMovement.SetCenterPosition(creature.transform.position);
                pathMovement.SetPathParameters(spawnRadius, 1.5f, 6);
            }
        }
        
        private void SetupCreatureComponents(GameObject creature)
        {
            // Add necessary components for interaction
            if (creature.GetComponent<CreatureInstanceData>() == null)
            {
                creature.AddComponent<CreatureInstanceData>();
            }
            
            if (creature.GetComponent<BoxCollider>() == null)
            {
                creature.AddComponent<BoxCollider>();
            }
            
            if (creature.GetComponent<Rigidbody>() == null)
            {
                var rb = creature.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
        }
        
        public void DespawnCreature(GameObject creature)
        {
            if (creature != null && activeCreatures.Contains(creature))
            {
                activeCreatures.Remove(creature);
                Destroy(creature);
            }
        }
        
        public void DespawnAllCreatures()
        {
            foreach (var creature in new List<GameObject>(activeCreatures))
            {
                DespawnCreature(creature);
            }
        }
        
        public List<GameObject> GetActiveCreatures()
        {
            return new List<GameObject>(activeCreatures);
        }
        
        public void SetSpawnParameters(float radius, float interval, int maxCount)
        {
            spawnRadius = radius;
            spawnInterval = interval;
            maxCreatures = maxCount;
        }
        
        public void AddCreaturePrefab(GameObject prefab)
        {
            if (prefab != null && !creaturePrefabs.Contains(prefab))
            {
                creaturePrefabs.Add(prefab);
            }
        }
        
        public void RemoveCreaturePrefab(GameObject prefab)
        {
            if (prefab != null && creaturePrefabs.Contains(prefab))
            {
                creaturePrefabs.Remove(prefab);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (playerTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
    }
} 
