using UnityEngine;

namespace RokidAR
{
    public class CreatureMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float minWaitTime = 2f;
        [SerializeField] private float maxWaitTime = 5f;
        [SerializeField] private float detectionRadius = 5f;
        [SerializeField] private float escapeSpeed = 3f;
        [SerializeField] private float escapeDistance = 8f;
        
        [Header("Height Variation")]
        [SerializeField] private float minHeight = 0.5f;
        [SerializeField] private float maxHeight = 2f;
        [SerializeField] private float heightVariationSpeed = 0.5f;
        
        [Header("Behavior States")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool isFlying = false;
        [SerializeField] private bool isShy = false;
        
        private Vector3 targetPosition;
        private Vector3 startPosition;
        private bool isMoving = false;
        private bool isEscaping = false;
        private float currentHeight;
        private float nextMoveTime;
        private Transform playerTransform;
        private CreatureInstanceData creatureData;
        
        public event System.Action OnMovementComplete;
        public event System.Action OnEscapeTriggered;
        
        private void Awake()
        {
            creatureData = GetComponent<CreatureInstanceData>();
            startPosition = transform.position;
            currentHeight = transform.position.y;
            
            // Find player (AR camera)
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
        
        private void Start()
        {
            InitializeMovement();
        }
        
        private void Update()
        {
            UpdateHeightVariation();
            UpdateMovement();
            UpdatePlayerDetection();
        }
        
        private void InitializeMovement()
        {
            if (!isMoving)
            {
                StartCoroutine(MovementRoutine());
            }
        }
        
        private IEnumerator MovementRoutine()
        {
            while (true)
            {
                if (!isEscaping)
                {
                    // Wait before next move
                    float waitTime = Random.Range(minWaitTime, maxWaitTime);
                    yield return new WaitForSeconds(waitTime);
                    
                    // Generate new target position
                    GenerateRandomTarget();
                    yield return StartCoroutine(MoveToTarget());
                }
                else
                {
                    // Escape movement
                    yield return StartCoroutine(EscapeMovement());
                }
            }
        }
        
        private void GenerateRandomTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
            
            targetPosition = startPosition + randomDirection;
            
            // Add height variation
            float targetHeight = Random.Range(minHeight, maxHeight);
            targetPosition.y = targetHeight;
            
            // Ensure position is within bounds
            targetPosition = ClampPositionToBounds(targetPosition);
        }
        
        private IEnumerator MoveToTarget()
        {
            isMoving = true;
            Vector3 startPos = transform.position;
            float journeyLength = Vector3.Distance(startPos, targetPosition);
            float journeyDuration = journeyLength / moveSpeed;
            
            float startTime = Time.time;
            
            while (Time.time - startTime < journeyDuration)
            {
                float distanceCovered = (Time.time - startTime) * moveSpeed;
                float fractionOfJourney = distanceCovered / journeyLength;
                float curveValue = movementCurve.Evaluate(fractionOfJourney);
                
                transform.position = Vector3.Lerp(startPos, targetPosition, curveValue);
                
                // Smooth rotation towards target
                Vector3 direction = (targetPosition - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                yield return null;
            }
            
            transform.position = targetPosition;
            isMoving = false;
            OnMovementComplete?.Invoke();
        }
        
        private IEnumerator EscapeMovement()
        {
            Vector3 escapeDirection = (transform.position - playerTransform.position).normalized;
            Vector3 escapeTarget = transform.position + escapeDirection * escapeDistance;
            
            float escapeStartTime = Time.time;
            Vector3 escapeStartPos = transform.position;
            
            while (Vector3.Distance(transform.position, escapeStartPos) < escapeDistance)
            {
                float progress = (Time.time - escapeStartTime) * escapeSpeed;
                transform.position = Vector3.MoveTowards(transform.position, escapeTarget, escapeSpeed * Time.deltaTime);
                
                yield return null;
            }
            
            isEscaping = false;
            startPosition = transform.position; // Update start position after escape
        }
        
        private void UpdateHeightVariation()
        {
            if (isFlying)
            {
                float heightOffset = Mathf.Sin(Time.time * heightVariationSpeed) * 0.3f;
                Vector3 newPosition = transform.position;
                newPosition.y = Mathf.Clamp(currentHeight + heightOffset, minHeight, maxHeight);
                transform.position = newPosition;
            }
        }
        
        private void UpdatePlayerDetection()
        {
            if (playerTransform == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer < detectionRadius)
            {
                if (isShy && !isEscaping)
                {
                    TriggerEscape();
                }
            }
        }
        
        private void UpdateMovement()
        {
            // Add subtle idle animation
            if (!isMoving && !isEscaping)
            {
                float idleOffset = Mathf.Sin(Time.time * 0.5f) * 0.05f;
                transform.position = new Vector3(
                    transform.position.x,
                    transform.position.y + idleOffset,
                    transform.position.z
                );
            }
        }
        
        private Vector3 ClampPositionToBounds(Vector3 position)
        {
            // Ensure creature stays within reasonable bounds
            Vector3 clampedPos = position;
            clampedPos.x = Mathf.Clamp(clampedPos.x, startPosition.x - wanderRadius, startPosition.x + wanderRadius);
            clampedPos.z = Mathf.Clamp(clampedPos.z, startPosition.z - wanderRadius, startPosition.z + wanderRadius);
            clampedPos.y = Mathf.Clamp(clampedPos.y, minHeight, maxHeight);
            
            return clampedPos;
        }
        
        public void TriggerEscape()
        {
            if (!isEscaping)
            {
                isEscaping = true;
                OnEscapeTriggered?.Invoke();
            }
        }
        
        public void SetWanderArea(Vector3 center, float radius)
        {
            startPosition = center;
            wanderRadius = radius;
        }
        
        public void SetMovementSpeed(float speed)
        {
            moveSpeed = speed;
        }
        
        public void SetDetectionRadius(float radius)
        {
            detectionRadius = radius;
        }
        
        public bool IsMoving()
        {
            return isMoving;
        }
        
        public bool IsEscaping()
        {
            return isEscaping;
        }
        
        public float GetDistanceToPlayer()
        {
            if (playerTransform != null)
            {
                return Vector3.Distance(transform.position, playerTransform.position);
            }
            return float.MaxValue;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPosition, wanderRadius);
            
            Gizmos.color = Color.red;
            if (playerTransform != null)
            {
                Gizmos.DrawWireSphere(playerTransform.position, detectionRadius);
            }
        }
    }
}