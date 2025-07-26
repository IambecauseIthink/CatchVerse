using UnityEngine;

namespace RokidAR.Utils
{
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("Swipe Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float timeThreshold = 0.3f;
        
        private Vector2 startPos;
        private float startTime;
        private bool couldBeSwipe;
        
        public event System.Action<float> OnSwipe;
        public event System.Action OnTap;
        
        private void Update()
        {
            HandleInput();
        }
        
        private void HandleInput()
        {
            #if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        startPos = touch.position;
                        startTime = Time.time;
                        couldBeSwipe = true;
                        break;
                        
                    case TouchPhase.Ended:
                        if (couldBeSwipe)
                        {
                            Vector2 deltaPos = touch.position - startPos;
                            float deltaTime = Time.time - startTime;
                            
                            if (deltaTime < timeThreshold)
                            {
                                if (Mathf.Abs(deltaPos.x) > swipeThreshold)
                                {
                                    OnSwipe?.Invoke(Mathf.Sign(deltaPos.x));
                                }
                                else if (deltaPos.magnitude < swipeThreshold)
                                {
                                    OnTap?.Invoke();
                                }
                            }
                        }
                        break;
                        
                    case TouchPhase.Canceled:
                        couldBeSwipe = false;
                        break;
                }
            }
            #endif
            
            // Mouse input for editor testing
            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                startPos = Input.mousePosition;
                startTime = Time.time;
                couldBeSwipe = true;
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                if (couldBeSwipe)
                {
                    Vector2 deltaPos = (Vector2)Input.mousePosition - startPos;
                    float deltaTime = Time.time - startTime;
                    
                    if (deltaTime < timeThreshold)
                    {
                        if (Mathf.Abs(deltaPos.x) > swipeThreshold)
                        {
                            OnSwipe?.Invoke(Mathf.Sign(deltaPos.x));
                        }
                        else if (deltaPos.magnitude < swipeThreshold)
                        {
                            OnTap?.Invoke();
                        }
                    }
                }
            }
            #endif
        }
    }
}