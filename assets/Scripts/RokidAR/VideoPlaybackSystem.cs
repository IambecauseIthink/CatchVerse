using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;

namespace RokidAR
{
    public class VideoPlaybackSystem : MonoBehaviour
    {
        [Header("Video Settings")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private AudioSource audioSource;
        
        [Header("Starlight Effects")]
        [SerializeField] private ParticleSystem starlightParticles;
        [SerializeField] private GameObject starlightContainer;
        [SerializeField] private Material starMaterial;
        [SerializeField] private int maxStars = 50;
        
        [Header("UI References")]
        [SerializeField] private Canvas videoCanvas;
        [SerializeField] private RawImage videoDisplay;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float starlightDuration = 2f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        
        public System.Action OnVideoComplete;
        public System.Action OnStarlightComplete;
        
        private string currentVideoName;
        private bool isPlaying = false;
        
        public static VideoPlaybackSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupVideoPlayer();
                SetupStarlightEffects();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void SetupVideoPlayer()
        {
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
            
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            videoPlayer.SetTargetAudioSource(0, audioSource);
            
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1920, 1080, 24);
                videoPlayer.targetTexture = renderTexture;
            }
            
            if (videoDisplay != null)
            {
                videoDisplay.texture = renderTexture;
            }
        }
        
        private void SetupStarlightEffects()
        {
            if (starlightContainer == null)
            {
                starlightContainer = new GameObject("StarlightContainer");
                starlightContainer.transform.SetParent(transform);
                starlightContainer.transform.localPosition = Vector3.zero;
            }
            
            if (starlightParticles == null)
            {
                starlightParticles = starlightContainer.AddComponent<ParticleSystem>();
                ConfigureStarlightParticles();
            }
            
            starlightContainer.SetActive(false);
        }
        
        private void ConfigureStarlightParticles()
        {
            var main = starlightParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.1f;
            main.startColor = Color.white;
            main.maxParticles = maxStars;
            
            var emission = starlightParticles.emission;
            emission.rateOverTime = 25f;
            
            var shape = starlightParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;
            
            var velocityOverLifetime = starlightParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(1f);
            
            var colorOverLifetime = starlightParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0f), 
                    new GradientColorKey(Color.yellow, 0.5f), 
                    new GradientColorKey(Color.white, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f), 
                    new GradientAlphaKey(1f, 0.5f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = gradient;
            
            var sizeOverLifetime = starlightParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }
        
        public void PlayVideo(string videoName)
        {
            currentVideoName = videoName;
            string videoPath = $"Videos/{videoName}";
            
            VideoClip clip = Resources.Load<VideoClip>(videoPath);
            if (clip != null)
            {
                StartCoroutine(PlayVideoSequence(clip));
            }
            else
            {
                Debug.LogError($"Video clip not found: {videoPath}");
                OnVideoComplete?.Invoke();
            }
        }
        
        private IEnumerator PlayVideoSequence(VideoClip clip)
        {
            isPlaying = true;
            
            // Show video canvas
            if (videoCanvas != null)
            {
                videoCanvas.gameObject.SetActive(true);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0;
                    yield return canvasGroup.DOFade(1, fadeInDuration).WaitForCompletion();
                }
            }
            
            // Play video
            videoPlayer.clip = clip;
            videoPlayer.Play();
            
            // Wait for video to complete
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }
            
            // Video complete callback
            OnVideoComplete?.Invoke();
            
            // Start starlight effect
            yield return StartCoroutine(PlayStarlightEffect());
            
            // Hide video canvas
            if (videoCanvas != null)
            {
                if (canvasGroup != null)
                {
                    yield return canvasGroup.DOFade(0, fadeInDuration).WaitForCompletion();
                }
                videoCanvas.gameObject.SetActive(false);
            }
            
            isPlaying = false;
        }
        
        private IEnumerator PlayStarlightEffect()
        {
            starlightContainer.SetActive(true);
            starlightParticles.Play();
            
            yield return new WaitForSeconds(starlightDuration);
            
            starlightParticles.Stop();
            starlightContainer.SetActive(false);
            
            OnStarlightComplete?.Invoke();
        }
        
        public void StopVideo()
        {
            if (isPlaying)
            {
                videoPlayer.Stop();
                isPlaying = false;
                
                if (videoCanvas != null)
                    videoCanvas.gameObject.SetActive(false);
                    
                if (starlightContainer != null)
                    starlightContainer.SetActive(false);
            }
        }
        
        public bool IsPlaying()
        {
            return isPlaying;
        }
        
        public void SetVideoDisplay(RawImage display)
        {
            videoDisplay = display;
            if (display != null)
            {
                display.texture = renderTexture;
            }
        }
    }
}