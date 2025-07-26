using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RokidAR
{
    public class ImageGalleryController : MonoBehaviour
    {
        [Header("Gallery Settings")]
        [SerializeField] private List<Sprite> galleryImages = new List<Sprite>();
        [SerializeField] private Transform imageContainer;
        [SerializeField] private Image mainImage;
        [SerializeField] private Image previewImageLeft;
        [SerializeField] private Image previewImageRight;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("UI Elements")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text pageIndicator;
        [SerializeField] private Button closeButton;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;
        [SerializeField] private float previewScale = 0.8f;
        [SerializeField] private float previewAlpha = 0.5f;
        
        private int currentIndex = 0;
        private bool isAnimating = false;
        
        public static ImageGalleryController Instance { get; private set; }
        
        public event System.Action OnGalleryClosed;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetupButtons();
            SetupInitialState();
        }
        
        public void ShowGallery(List<Sprite> images)
        {
            if (images == null || images.Count == 0)
            {
                Debug.LogWarning("No images provided to gallery");
                return;
            }
            
            galleryImages = new List<Sprite>(images);
            currentIndex = 0;
            
            gameObject.SetActive(true);
            UpdateDisplay();
            
            // Fade in animation
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, animationDuration);
        }
        
        public void HideGallery()
        {
            canvasGroup.DOFade(0, animationDuration).OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnGalleryClosed?.Invoke();
            });
        }
        
        public void NextImage()
        {
            if (isAnimating || galleryImages.Count <= 1) return;
            
            isAnimating = true;
            currentIndex = (currentIndex + 1) % galleryImages.Count;
            
            AnimateSlide(-1, () =>
            {
                UpdateDisplay();
                AnimateSlideIn(1);
            });
        }
        
        public void PreviousImage()
        {
            if (isAnimating || galleryImages.Count <= 1) return;
            
            isAnimating = true;
            currentIndex = (currentIndex - 1 + galleryImages.Count) % galleryImages.Count;
            
            AnimateSlide(1, () =>
            {
                UpdateDisplay();
                AnimateSlideIn(-1);
            });
        }
        
        private void UpdateDisplay()
        {
            if (galleryImages.Count == 0) return;
            
            // Update main image
            mainImage.sprite = galleryImages[currentIndex];
            mainImage.preserveAspect = true;
            
            // Update preview images
            UpdatePreviewImages();
            
            // Update page indicator
            if (pageIndicator != null)
            {
                pageIndicator.text = $"{currentIndex + 1} / {galleryImages.Count}";
            }
            
            // Update button states
            UpdateButtonStates();
        }
        
        private void UpdatePreviewImages()
        {
            if (galleryImages.Count == 1)
            {
                if (previewImageLeft != null) previewImageLeft.gameObject.SetActive(false);
                if (previewImageRight != null) previewImageRight.gameObject.SetActive(false);
                return;
            }
            
            int leftIndex = (currentIndex - 1 + galleryImages.Count) % galleryImages.Count;
            int rightIndex = (currentIndex + 1) % galleryImages.Count;
            
            if (previewImageLeft != null)
            {
                previewImageLeft.sprite = galleryImages[leftIndex];
                previewImageLeft.preserveAspect = true;
                previewImageLeft.gameObject.SetActive(true);
            }
            
            if (previewImageRight != null)
            {
                previewImageRight.sprite = galleryImages[rightIndex];
                previewImageRight.preserveAspect = true;
                previewImageRight.gameObject.SetActive(true);
            }
        }
        
        private void UpdateButtonStates()
        {
            if (previousButton != null)
                previousButton.interactable = galleryImages.Count > 1;
            
            if (nextButton != null)
                nextButton.interactable = galleryImages.Count > 1;
        }
        
        private void AnimateSlide(float direction, System.Action onComplete)
        {
            Vector3 startPos = imageContainer.localPosition;
            Vector3 endPos = startPos + Vector3.right * direction * 1000f;
            
            imageContainer.DOLocalMove(endPos, animationDuration)
                .SetEase(slideEase)
                .OnComplete(() => onComplete?.Invoke());
        }
        
        private void AnimateSlideIn(float fromDirection)
        {
            Vector3 startPos = Vector3.right * fromDirection * 1000f;
            imageContainer.localPosition = startPos;
            
            imageContainer.DOLocalMove(Vector3.zero, animationDuration)
                .SetEase(slideEase)
                .OnComplete(() => isAnimating = false);
        }
        
        private void SetupButtons()
        {
            if (previousButton != null)
                previousButton.onClick.AddListener(PreviousImage);
            
            if (nextButton != null)
                nextButton.onClick.AddListener(NextImage);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(HideGallery);
        }
        
        private void SetupInitialState()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Set initial alpha
            canvasGroup.alpha = 0;
        }
        
        // Touch/Mouse input support for mobile
        private Vector2 touchStartPos;
        private bool isDragging = false;
        
        private void Update()
        {
            HandleTouchInput();
        }
        
        private void HandleTouchInput()
        {
            if (isAnimating) return;
            
            #if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStartPos = touch.position;
                        isDragging = false;
                        break;
                        
                    case TouchPhase.Moved:
                        if (Vector2.Distance(touchStartPos, touch.position) > 50f)
                        {
                            isDragging = true;
                        }
                        break;
                        
                    case TouchPhase.Ended:
                        if (isDragging)
                        {
                            float deltaX = touch.position.x - touchStartPos.x;
                            if (Mathf.Abs(deltaX) > 100f)
                            {
                                if (deltaX > 0)
                                    PreviousImage();
                                else
                                    NextImage();
                            }
                        }
                        isDragging = false;
                        break;
                }
            }
            #endif
            
            // Mouse input for editor testing
            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                touchStartPos = Input.mousePosition;
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                float deltaX = Input.mousePosition.x - touchStartPos.x;
                if (Mathf.Abs(deltaX) > 100f)
                {
                    if (deltaX > 0)
                        PreviousImage();
                    else
                        NextImage();
                }
            }
            #endif
        }
        
        // Utility methods
        public void AddImage(Sprite image)
        {
            if (image != null)
            {
                galleryImages.Add(image);
                UpdateDisplay();
            }
        }
        
        public void RemoveImageAt(int index)
        {
            if (index >= 0 && index < galleryImages.Count)
            {
                galleryImages.RemoveAt(index);
                currentIndex = Mathf.Clamp(currentIndex, 0, galleryImages.Count - 1);
                UpdateDisplay();
            }
        }
        
        public Sprite GetCurrentImage()
        {
            return currentIndex >= 0 && currentIndex < galleryImages.Count ? 
                galleryImages[currentIndex] : null;
        }
        
        public int GetCurrentIndex()
        {
            return currentIndex;
        }
        
        public int GetImageCount()
        {
            return galleryImages.Count;
        }
    }
}