using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using DG.Tweening;

namespace RokidAR
{
    public class CaptureSystem : MonoBehaviour
    {
        [Header("Capture Settings")]
        [SerializeField] private float captureDistance = 2f;
        [SerializeField] private float captureRadius = 1f;
        [SerializeField] private LayerMask creatureLayerMask = -1;
        [SerializeField] private float captureSuccessRate = 0.8f;
        
        [Header("UI References")]
        [SerializeField] private Canvas captureCanvas;
        [SerializeField] private Image captureCircle;
        [SerializeField] private Button captureButton;
        [SerializeField] private Text captureText;
        [SerializeField] private Slider captureProgress;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem captureEffect;
        [SerializeField] private GameObject captureRing;
        [SerializeField] private Material captureMaterial;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip captureSound;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip failSound;
        
        [Header("Feedback")]
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeStrength = 0.1f;
        [SerializeField] private Color successColor = Color.green;
        [SerializeField] private Color failColor = Color.red;
        [SerializeField] private Color warningColor = Color.yellow;
        
        private ARSessionOrigin arSessionOrigin;
        private Camera arCamera;
        private GameObject targetCreature;
        private bool isCapturing = false;
        private bool inCaptureMode = false;
        
        public static CaptureSystem Instance { get; private set; }
        
        public event System.Action<GameObject> OnCaptureSuccess;
        public event System.Action<GameObject> OnCaptureFail;
        public event System.Action<GameObject> OnCreatureDetected;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupCaptureUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            arCamera = arSessionOrigin?.camera ?? Camera.main;
            
            if (captureCanvas != null)
                captureCanvas.gameObject.SetActive(false);
        }
        
        private void Update()
        {
            if (inCaptureMode)
            {
                UpdateCaptureMode();
            }
            else
            {
                ScanForCreatures();
            }
        }
        
        public void EnterCaptureMode(GameObject creature)
        {
            if (creature == null || isCapturing) return;
            
            targetCreature = creature;
            inCaptureMode = true;
            
            if (captureCanvas != null)
            {
                captureCanvas.gameObject.SetActive(true);
                UpdateCaptureUI();
            }
            
            OnCreatureDetected?.Invoke(creature);
            StartCaptureSequence();
        }
        
        public void ExitCaptureMode()
        {
            inCaptureMode = false;
            isCapturing = false;
            targetCreature = null;
            
            if (captureCanvas != null)
                captureCanvas.gameObject.SetActive(false);
                
            if (captureRing != null)
                captureRing.SetActive(false);
        }
        
        private void UpdateCaptureMode()
        {
            if (targetCreature == null)
            {
                ExitCaptureMode();
                return;
            }
            
            float distance = Vector3.Distance(arCamera.transform.position, targetCreature.transform.position);
            
            if (distance <= captureDistance)
            {
                UpdateCaptureRing();
                UpdateCaptureProgress(distance);
            }
            else
            {
                ShowWarning("太近或太远！请调整位置");
            }
        }
        
        private void ScanForCreatures()
        {
            Collider[] creatures = Physics.OverlapSphere(arCamera.transform.position, captureDistance * 2, creatureLayerMask);
            
            foreach (var creature in creatures)
            {
                GameObject creatureObj = creature.gameObject;
                if (creatureObj.GetComponent<CreatureInstanceData>() != null)
                {
                    float distance = Vector3.Distance(arCamera.transform.position, creatureObj.transform.position);
                    if (distance <= captureDistance)
                    {
                        EnterCaptureMode(creatureObj);
                        break;
                    }
                }
            }
        }
        
        public void AttemptCapture()
        {
            if (isCapturing || targetCreature == null) return;
            
            StartCoroutine(CaptureSequence());
        }
        
        private IEnumerator CaptureSequence()
        {
            isCapturing = true;
            
            // Show capture effect
            if (captureEffect != null)
            {
                captureEffect.transform.position = targetCreature.transform.position;
                captureEffect.Play();
            }
            
            // Play capture sound
            if (audioSource != null && captureSound != null)
            {
                audioSource.PlayOneShot(captureSound);
            }
            
            // Visual feedback
            if (captureRing != null)
            {
                captureRing.SetActive(true);
                captureRing.transform.position = targetCreature.transform.position;
                
                // Animate capture ring
                captureRing.transform.localScale = Vector3.one * 0.5f;
                yield return captureRing.transform.DOScale(Vector3.one * 1.5f, 1f).WaitForCompletion();
            }
            
            // Calculate success
            bool success = CalculateCaptureSuccess();
            
            if (success)
            {
                yield return StartCoroutine(CaptureSuccess());
            }
            else
            {
                yield return StartCoroutine(CaptureFail());
            }
            
            isCapturing = false;
        }
        
        private bool CalculateCaptureSuccess()
        {
            if (targetCreature == null) return false;
            
            float distance = Vector3.Distance(arCamera.transform.position, targetCreature.transform.position);
            float accuracy = 1f - (distance / captureDistance);
            
            // Add creature-specific modifiers
            var creatureData = targetCreature.GetComponent<CreatureInstanceData>();
            if (creatureData != null && creatureData.IsRare())
            {
                accuracy *= 0.7f; // Harder to catch rare creatures
            }
            
            float successChance = captureSuccessRate * accuracy;
            return Random.value < successChance;
        }
        
        private IEnumerator CaptureSuccess()
        {
            // Success animation
            if (audioSource != null && successSound != null)
            {
                audioSource.PlayOneShot(successSound);
            }
            
            // Screen flash
            if (captureCircle != null)
            {
                captureCircle.color = successColor;
                yield return captureCircle.DOColor(Color.white, 0.5f).WaitForCompletion();
            }
            
            // Creature capture animation
            if (targetCreature != null)
            {
                // Shrink and disappear
                yield return targetCreature.transform.DOScale(Vector3.zero, 1f).WaitForCompletion();
                
                OnCaptureSuccess?.Invoke(targetCreature);
                
                // Clean up
                Destroy(targetCreature);
            }
            
            ShowSuccessMessage("捕获成功！");
            yield return new WaitForSeconds(1f);
            
            ExitCaptureMode();
        }
        
        private IEnumerator CaptureFail()
        {
            // Fail animation
            if (audioSource != null && failSound != null)
            {
                audioSource.PlayOneShot(failSound);
            }
            
            // Screen shake
            if (captureCircle != null)
            {
                captureCircle.color = failColor;
                captureCircle.transform.DOShakePosition(shakeDuration, shakeStrength);
                yield return new WaitForSeconds(shakeDuration);
            }
            
            // Creature escape animation
            if (targetCreature != null)
            {
                var creatureMovement = targetCreature.GetComponent<CreatureMovement>();
                if (creatureMovement != null)
                {
                    creatureMovement.TriggerEscape();
                }
                
                OnCaptureFail?.Invoke(targetCreature);
            }
            
            ShowFailMessage("捕获失败！精灵逃跑了");
            yield return new WaitForSeconds(1f);
            
            ExitCaptureMode();
        }
        
        private void UpdateCaptureRing()
        {
            if (captureRing != null && targetCreature != null)
            {
                captureRing.transform.position = targetCreature.transform.position;
                captureRing.transform.LookAt(arCamera.transform);
            }
        }
        
        private void UpdateCaptureProgress(float distance)
        {
            if (captureProgress != null)
            {
                float progress = 1f - (distance / captureDistance);
                captureProgress.value = progress;
                
                // Change color based on distance
                if (progress > 0.7f)
                {
                    captureProgress.fillRect.GetComponent<Image>().color = successColor;
                }
                else if (progress > 0.3f)
                {
                    captureProgress.fillRect.GetComponent<Image>().color = warningColor;
                }
                else
                {
                    captureProgress.fillRect.GetComponent<Image>().color = failColor;
                }
            }
        }
        
        private void SetupCaptureUI()
        {
            if (captureButton != null)
            {
                captureButton.onClick.AddListener(AttemptCapture);
            }
            
            if (canvasGroup == null && captureCanvas != null)
            {
                canvasGroup = captureCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = captureCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
        
        private void ShowSuccessMessage(string message)
        {
            if (captureText != null)
            {
                captureText.text = message;
                captureText.color = successColor;
            }
        }
        
        private void ShowFailMessage(string message)
        {
            if (captureText != null)
            {
                captureText.text = message;
                captureText.color = failColor;
            }
        }
        
        private void ShowWarning(string message)
        {
            if (captureText != null)
            {
                captureText.text = message;
                captureText.color = warningColor;
            }
        }
        
        private void UpdateCaptureUI()
        {
            if (targetCreature != null)
            {
                var creatureData = targetCreature.GetComponent<CreatureInstanceData>();
                if (creatureData != null)
                {
                    string creatureName = creatureData.Config != null ? 
                        creatureData.Config.displayName : "未知精灵";
                    
                    if (captureText != null)
                    {
                        captureText.text = $"发现 {creatureName}!";
                    }
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (arCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(arCamera.transform.position, captureDistance);
            }
        }
    }
}