using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RokidAR
{
    public class HandGestureRecognition : MonoBehaviour
    {
        [Header("Hand Tracking")]
        [SerializeField] private ARSessionOrigin arSessionOrigin;
        [SerializeField] private Camera arCamera;
        [SerializeField] private LayerMask interactionLayerMask = -1;
        
        [Header("Gesture Settings")]
        [SerializeField] private float grabThreshold = 0.1f;
        [SerializeField] private float releaseThreshold = 0.2f;
        [SerializeField] private float gestureTimeout = 2f;
        [SerializeField] private float handVelocityThreshold = 0.5f;
        
        [Header("Visual Feedback")]
        [SerializeField] private LineRenderer handRay;
        [SerializeField] private GameObject handCursor;
        [SerializeField] private Material grabMaterial;
        [SerializeField] private Material normalMaterial;
        
        public event System.Action<Vector3> OnGrabGesture;
        public event System.Action<Vector3> OnThrowGesture;
        public event System.Action<GameObject> OnGrabTarget;
        public event System.Action<GameObject, Vector3> OnThrowTarget;
        
        public static HandGestureRecognition Instance { get; private set; }
        
        private bool isGrabbing = false;
        private bool isTracking = false;
        private Vector3 lastHandPosition;
        private Vector3 handVelocity;
        private GameObject grabbedObject;
        private float grabStartTime;
        private Vector3 grabStartPosition;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupHandTracking();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (arCamera == null)
                arCamera = arSessionOrigin?.camera ?? Camera.main;
                
            SetupVisualFeedback();
        }
        
        private void Update()
        {
            UpdateHandTracking();
            DetectGestures();
            UpdateVisualFeedback();
        }
        
        private void SetupHandTracking()
        {
            // Initialize hand tracking components
            if (arSessionOrigin == null)
                arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        }
        
        private void SetupVisualFeedback()
        {
            if (handRay == null)
            {
                GameObject rayObj = new GameObject("HandRay");
                rayObj.transform.SetParent(transform);
                handRay = rayObj.AddComponent<LineRenderer>();
                
                handRay.material = normalMaterial;
                handRay.startWidth = 0.01f;
                handRay.endWidth = 0.01f;
                handRay.positionCount = 2;
                handRay.enabled = false;
            }
            
            if (handCursor == null)
            {
                handCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                handCursor.transform.SetParent(transform);
                handCursor.transform.localScale = Vector3.one * 0.05f;
                handCursor.GetComponent<Renderer>().material = normalMaterial;
                handCursor.SetActive(false);
            }
        }
        
        private void UpdateHandTracking()
        {
            // Simulate hand position for development
            Vector3 handPosition = GetSimulatedHandPosition();
            
            if (handPosition != Vector3.zero)
            {
                isTracking = true;
                handVelocity = (handPosition - lastHandPosition) / Time.deltaTime;
                lastHandPosition = handPosition;
            }
            else
            {
                isTracking = false;
            }
        }
        
        private Vector3 GetSimulatedHandPosition()
        {
            // In development, use mouse/controller position
            #if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 10f, interactionLayerMask))
                {
                    return hit.point;
                }
            }
            #endif
            
            // For actual Rokid AR implementation, replace with hand tracking data
            return Vector3.zero;
        }
        
        private void DetectGestures()
        {
            if (!isTracking) return;
            
            // Detect grab gesture
            if (!isGrabbing && IsGrabGesture())
            {
                StartGrabGesture();
            }
            
            // Detect release/throw gesture
            else if (isGrabbing && IsReleaseGesture())
            {
                EndGrabGesture();
            }
            
            // Handle timeout
            if (isGrabbing && Time.time - grabStartTime > gestureTimeout)
            {
                CancelGrab();
            }
        }
        
        private bool IsGrabGesture()
        {
            // Detect grab based on hand velocity and position
            return Input.GetMouseButtonDown(0); // Simplified for development
        }
        
        private bool IsReleaseGesture()
        {
            // Detect release based on hand velocity and button release
            return Input.GetMouseButtonUp(0); // Simplified for development
        }
        
        private void StartGrabGesture()
        {
            isGrabbing = true;
            grabStartTime = Time.time;
            grabStartPosition = lastHandPosition;
            
            // Raycast to find grab target
            Ray ray = new Ray(arCamera.transform.position, GetHandDirection());
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, interactionLayerMask))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject.GetComponent<CreatureInstanceData>() != null)
                {
                    grabbedObject = hitObject;
                    OnGrabTarget?.Invoke(grabbedObject);
                    OnGrabGesture?.Invoke(hit.point);
                }
            }
            
            UpdateHandVisual(true);
        }
        
        private void EndGrabGesture()
        {
            if (grabbedObject != null)
            {
                Vector3 throwDirection = GetThrowDirection();
                OnThrowTarget?.Invoke(grabbedObject, throwDirection);
                OnThrowGesture?.Invoke(throwDirection);
                
                grabbedObject = null;
            }
            
            isGrabbing = false;
            UpdateHandVisual(false);
        }
        
        private void CancelGrab()
        {
            isGrabbing = false;
            grabbedObject = null;
            UpdateHandVisual(false);
        }
        
        private Vector3 GetHandDirection()
        {
            // Calculate direction from camera to hand position
            return (lastHandPosition - arCamera.transform.position).normalized;
        }
        
        private Vector3 GetThrowDirection()
        {
            // Calculate throw direction based on hand velocity
            return handVelocity.normalized;
        }
        
        private void UpdateVisualFeedback()
        {
            if (!isTracking)
            {
                handRay.enabled = false;
                handCursor.SetActive(false);
                return;
            }
            
            // Update hand ray
            Vector3 handDirection = GetHandDirection();
            Vector3 rayEnd = arCamera.transform.position + handDirection * 10f;
            
            handRay.enabled = true;
            handRay.SetPosition(0, arCamera.transform.position);
            handRay.SetPosition(1, rayEnd);
            
            // Update hand cursor
            RaycastHit hit;
            if (Physics.Raycast(arCamera.transform.position, handDirection, out hit, 10f, interactionLayerMask))
            {
                handCursor.SetActive(true);
                handCursor.transform.position = hit.point;
                
                // Color based on interactable
                bool isInteractable = hit.collider.GetComponent<CreatureInstanceData>() != null;
                handCursor.GetComponent<Renderer>().material = isInteractable ? grabMaterial : normalMaterial;
            }
            else
            {
                handCursor.SetActive(false);
            }
        }
        
        private void UpdateHandVisual(bool isGrabbing)
        {
            if (handRay != null)
            {
                handRay.material = isGrabbing ? grabMaterial : normalMaterial;
            }
            
            if (handCursor != null)
            {
                handCursor.GetComponent<Renderer>().material = isGrabbing ? grabMaterial : normalMaterial;
            }
        }
        
        public bool IsGrabbing()
        {
            return isGrabbing;
        }
        
        public GameObject GetGrabbedObject()
        {
            return grabbedObject;
        }
        
        public Vector3 GetHandPosition()
        {
            return lastHandPosition;
        }
        
        public Vector3 GetHandVelocity()
        {
            return handVelocity;
        }
        
        private void OnDrawGizmos()
        {
            if (isTracking)
            {
                Gizmos.color = isGrabbing ? Color.red : Color.green;
                Gizmos.DrawWireSphere(lastHandPosition, 0.1f);
                
                if (grabbedObject != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(lastHandPosition, grabbedObject.transform.position);
                }
            }
        }
    }
}