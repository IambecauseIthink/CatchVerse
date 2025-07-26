using System;
using UnityEngine;

namespace RokidAR
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewCreatureConfig", menuName = "RokidAR/Creature Config")]
    public class CreatureConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public string creatureId;
        public string displayName;
        public GameObject modelPrefab;
        public bool isRare = false;
        
        [Header("Transform Settings")]
        public Vector3 defaultScale = Vector3.one * 0.5f;
        public Vector3 defaultPosition = Vector3.zero;
        public Quaternion defaultRotation = Quaternion.identity;
        
        [Header("Animation Settings")]
        public string defaultAnimation = "idle";
        public string[] availableAnimations;
        
        [Header("Rendering Settings")]
        public bool enableShadows = true;
        public bool enableOcclusion = true;
        
        [Header("Physics Settings")]
        public Vector3 colliderSize = Vector3.one;
        public Vector3 colliderCenter = Vector3.zero;
        public bool useRigidbody = false;
        public float mass = 1f;
        
        [Header("Fallback Settings")]
        public GameObject fallbackPrefab;
        public Color fallbackColor = Color.red;
        
        [Header("AR Placement Settings")]
        public bool requireGroundPlane = true;
        public bool allowAirPlacement = false;
        public float minPlacementDistance = 0.5f;
        public float maxPlacementDistance = 3f;
    }
}