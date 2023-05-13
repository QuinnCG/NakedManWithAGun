using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
    [System.Serializable]
    public class EnemySpawnData
    {
        [Required, AssetsOnly]
        public GameObject Prefab;
		public bool IsBoss;
		[ShowIf("IsBoss")]
		public string BossTitle = "Boss Title";
		[ShowIf("IsBoss")]
		public Color BossTitleColor = Color.white;
		[ShowIf("IsBoss")]
		public AudioClip BossMusic;

		[Space, MinMaxSlider(-20f, 20, showFields: true), HideIf("IsBoss")]
		public Vector2Int Count;
    }
}
