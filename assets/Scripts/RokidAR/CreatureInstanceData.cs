using UnityEngine;

namespace RokidAR
{
    public class CreatureInstanceData : MonoBehaviour
    {
        [SerializeField] private CreatureConfig config;
        
        public CreatureConfig Config => config;
        
        public void Initialize(CreatureConfig creatureConfig)
        {
            config = creatureConfig;
            gameObject.name = $"Creature_{config.creatureId}";
        }
        
        public string GetCreatureId()
        {
            return config != null ? config.creatureId : "unknown";
        }
        
        public bool IsRare()
        {
            return config != null && config.isRare;
        }
    }
}