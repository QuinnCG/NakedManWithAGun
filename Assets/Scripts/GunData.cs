using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
    [CreateAssetMenu(fileName = "Gun Data", menuName = "Scriptable Objects/Gun Data")]
    public class GunData : ScriptableObject
    {
		[Space, BoxGroup]
		public string Name = "No Name";
		[BoxGroup]
		public Color NameColor = Color.white;
		[BoxGroup]
        public float FireRate = 0.2f;
		[BoxGroup]
		public float ReloadDuration = 1.5f;
		[BoxGroup]
		public bool IsAutomatic;

		[Space, BoxGroup]
        public int MagazineSize = 10;
		[BoxGroup]
		public int ReserveAmmo = 150;

		[Space, BoxGroup]
        public float Spread = 15f;
		[BoxGroup]
		public int Count = 1;

		[FoldoutGroup("Graphics"), Required]
		public Sprite Sprite;
		[FoldoutGroup("Graphics"), Required]
		public Sprite DroppedSprite;

		[Space, BoxGroup("Audio"), Required]
		public RandomAudioClip ReloadSound;
		[BoxGroup("Audio"), Required]
		public bool PlayReloadOnEquip = true;

		[Space, FoldoutGroup("Effects")]
		public float ScreenShakeAmplitude = 1f;
		[FoldoutGroup("Effects"), Range(0f, 0.25f)]
		public float KickbackOffset = 0.1f;

		[Space, FoldoutGroup("Projectile")]
		public Vector2 SpawnOffset = new(0.5f, 0f);
		[FoldoutGroup("Projectile"), Required, InlineEditor]
        public ProjectileData Projectile;
    }
}
