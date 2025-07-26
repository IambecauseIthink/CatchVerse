using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace RokidAR
{
    public class ProceduralPathMovement : MonoBehaviour
    {
        [Header("Path Generation")]
        [SerializeField] private int pathPoints = 8;
        [SerializeField] private float pathRadius = 3f;
        [SerializeField] private float pathHeight = 1.5f;
        [SerializeField] private float pathSmoothness = 0.3f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float waitTime = 2f;
        [SerializeField] private float heightVariation = 0.5f;
        [SerializeField] private float heightFrequency = 0.5f;
        
        [Header("Randomization")]
        [SerializeField] private bool randomizeHeight = true;
        [SerializeField] private bool randomizeSpeed = true;
        [SerializeField] private float speedVariation = 0.5f;
        [SerializeField] private float radiusVariation = 0.8f;
        
        [Header("Debug")]
        [SerializeField] private bool showPathGizmos = true;
        [SerializeField] private Color pathColor = Color.cyan;
        
        private List<Vector3> currentPath = new List<Vector3>();
        private int currentPathIndex = 0;
        private bool isMoving = false;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float currentMoveSpeed;
        private float currentHeight;
        private float journeyStartTime;
        private float journeyDuration;
        
        public event System.Action OnPathComplete;
        public event System.Action OnNewPathGenerated;
        
        private void Start()
        {
            startPosition = transform.position;
            currentHeight = transform.position.y;
            GenerateNewPath();
            StartCoroutine(PathMovementRoutine());
        }
        
        private void Update()
        {
            if (isMoving)
            {
                UpdateMovement();
                UpdateHeightVariation();
            }
        }
        
        private IEnumerator PathMovementRoutine()
        {
            while (true)
            {
                yield return StartCoroutine(FollowPath());
                yield return new WaitForSeconds(waitTime);
                GenerateNewPath();
            }
        }
        
        private IEnumerator FollowPath()
        {
            if (currentPath.Count == 0) yield break;
            
            for (int i = 0; i < currentPath.Count; i++)
            {
                currentPathIndex = i;
                targetPosition = currentPath[i];
                
                // Calculate journey parameters
                float distance = Vector3.Distance(transform.position, targetPosition);
                currentMoveSpeed = randomizeSpeed ? 
                    moveSpeed * UnityEngine.Random.Range(1 - speedVariation, 1 + speedVariation) : 
                    moveSpeed;
                
                journeyDuration = distance / currentMoveSpeed;
                journeyStartTime = Time.time;
                
                isMoving = true;
                
                yield return StartCoroutine(MoveToPoint(targetPosition, journeyDuration));
            }
            
            isMoving = false;
            OnPathComplete?.Invoke();
        }
        
        private IEnumerator MoveToPoint(Vector3 target, float duration)
        {
            Vector3 startPos = transform.position;
            float startTime = Time.time;
            
            while (Time.time - startTime < duration)
            {
                float t = (Time.time - startTime) / duration;
                float smoothT = movementCurve.Evaluate(t);
                
                transform.position = Vector3.Lerp(startPos, target, smoothT);
                
                // Rotate towards movement direction
                Vector3 direction = (target - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                yield return null;
            }
            
            transform.position = target;
        }
        
        public void GenerateNewPath()
        {
            currentPath.Clear();
            
            float currentRadius = randomizeRadius ? 
                pathRadius * UnityEngine.Random.Range(1 - radiusVariation, 1 + radiusVariation) : 
                pathRadius;
            
            Vector3 centerPoint = startPosition;
            
            // Generate random path points
            for (int i = 0; i < pathPoints; i++)
            {
                float angle = (float)i / pathPoints * 2 * Mathf.PI;
                float randomOffset = UnityEngine.Random.Range(-pathSmoothness, pathSmoothness);
                
                Vector3 point = centerPoint + new Vector3(
                    Mathf.Cos(angle + randomOffset) * currentRadius,
                    randomizeHeight ? UnityEngine.Random.Range(-heightVariation, heightVariation) : 0,
                    Mathf.Sin(angle + randomOffset) * currentRadius
                );
                
                // Add height variation
                point.y += pathHeight + 
                    (randomizeHeight ? UnityEngine.Random.Range(-heightVariation, heightVariation) : 0);
                
                currentPath.Add(point);
            }
            
            // Add some random mid-points for more organic movement
            AddBezierCurves();
            
            OnNewPathGenerated?.Invoke();
        }
        
        private void AddBezierCurves()
        {
            if (currentPath.Count < 3) return;
            
            List<Vector3> bezierPath = new List<Vector3>();
            
            for (int i = 0; i < currentPath.Count; i++)
            {
                Vector3 p0 = currentPath[i];
                Vector3 p1 = currentPath[(i + 1) % currentPath.Count];
                Vector3 p2 = currentPath[(i + 2) % currentPath.Count];
                
                // Add original point
                bezierPath.Add(p0);
                
                // Add bezier curve points
                int curvePoints = 3;
                for (int j = 1; j <= curvePoints; j++)
                {
                    float t = (float)j / (curvePoints + 1);
                    Vector3 bezierPoint = CalculateBezierPoint(p0, p1, p2, t);
                    bezierPath.Add(bezierPoint);
                }
            }
            
            currentPath = bezierPath;
        }
        
        private Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            Vector3 controlPoint1 = p0 + (p1 - p0) * 0.5f;
            Vector3 controlPoint2 = p1 + (p2 - p1) * 0.5f;
            
            return Vector3.Lerp(
                Vector3.Lerp(p0, p1, t),
                Vector3.Lerp(p1, p2, t),
                t
            );
        }
        
        private void UpdateMovement()
        {
            if (!isMoving) return;
            
            // Add some noise to movement
            Vector3 noiseOffset = new Vector3(
                Mathf.PerlinNoise(Time.time * 0.5f, 0) * 0.1f,
                Mathf.PerlinNoise(0, Time.time * 0.5f) * 0.1f,
                Mathf.PerlinNoise(Time.time * 0.5f, Time.time * 0.5f) * 0.1f
            );
            
            transform.position += noiseOffset * 0.01f;
        }
        
        private void UpdateHeightVariation()
        {
            if (randomizeHeight)
            {
                float heightOffset = Mathf.Sin(Time.time * heightFrequency) * heightVariation;
                Vector3 newPosition = transform.position;
                newPosition.y = currentHeight + heightOffset;
                transform.position = newPosition;
            }
        }
        
        public void SetCenterPosition(Vector3 center)
        {
            startPosition = center;
            GenerateNewPath();
        }
        
        public void SetPathParameters(float radius, float height, int points)
        {
            pathRadius = radius;
            pathHeight = height;
            pathPoints = points;
            GenerateNewPath();
        }
        
        public void UpdateMovementSpeed(float newSpeed)
        {
            moveSpeed = newSpeed;
        }
        
        public List<Vector3> GetCurrentPath()
        {
            return new List<Vector3>(currentPath);
        }
        
        public bool IsMoving()
        {
            return isMoving;
        }
        
        public void StopMovement()
        {
            isMoving = false;
            StopAllCoroutines();
        }
        
        public void ResumeMovement()
        {
            if (!isMoving)
            {
                StartCoroutine(PathMovementRoutine());
            }
        }
        
        private void OnDrawGizmos()
        {
            if (showPathGizmos && currentPath.Count > 0)
            {
                Gizmos.color = pathColor;
                
                for (int i = 0; i < currentPath.Count; i++)
                {
                    Vector3 current = currentPath[i];
                    Vector3 next = currentPath[(i + 1) % currentPath.Count];
                    
                    Gizmos.DrawLine(current, next);
                    Gizmos.DrawWireSphere(current, 0.1f);
                }
                
                if (isMoving)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, 0.2f);
                    Gizmos.DrawLine(transform.position, targetPosition);
                }
            }
        }
    }
}