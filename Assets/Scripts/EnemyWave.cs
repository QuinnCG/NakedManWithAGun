using Sirenix.OdinInspector;

namespace Quinn
{
    [System.Serializable]
    public class EnemyWave
    {
        [RequiredListLength(MinLength = 1)]
        public EnemySpawnData[] EnemySpawns;
    }
}
