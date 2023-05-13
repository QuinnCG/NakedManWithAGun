using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
	[CreateAssetMenu(fileName = "Projectile Data", menuName = "Scriptable Objects/Projectile Data")]
    public class ProjectileData : ScriptableObject
    {
		[Space, BoxGroup, Min(0f)]
		public float Speed = 2f;
		[BoxGroup, Min(0)]
		public int Damage = 1;
		[BoxGroup]
		public bool RotateToDirection;
		[BoxGroup]
		public float TurnRate = 0f;
		[BoxGroup]
		public bool Ricochet;

		[Space, BoxGroup]
		public GameObject PrefabOnHit;
		[BoxGroup, AssetsOnly]
		public float PrefabOnHitChance = 0.2f;

		[Space, BoxGroup("Audio"), Required]
		public RandomAudioClip FireClip;
		[Space, BoxGroup("Audio"), Required]
		public float FireSoundChance = 1f;
		[BoxGroup("Audio")]
		public bool Loop;

		[Space, AssetsOnly, FoldoutGroup("Graphics")]
        public Sprite Sprite;
		[AssetsOnly, FoldoutGroup("Graphics")]
		public float Opacity = 1f;
		[FoldoutGroup("Graphics"), Min(0f)]
		public float CollisionSize = 0.2f;
		[FoldoutGroup("Graphics"), Min(1f)]
		public float Glow = 4f;

		[Space, FoldoutGroup("Wave"), Min(0f)]
        public float WaveAmplitude = 0f;
        [FoldoutGroup("Wave"), Min(0f)]
        public float WaveFrequency = 1f;

		[Space, FoldoutGroup("Homing")]
		public float HomingFactor = 0f;
		[FoldoutGroup("Homing"), Min(0f)]
		public float HomingRange = 3;

		[Space, FoldoutGroup("Spin")]
		public bool RandomDirection = false;
		[FoldoutGroup("Spin"), HideIf("RandomDirection")]
		public bool SpinClockwise = true;
		[FoldoutGroup("Spin"), Min(0f)]
        public float SpinSpeed = 0f;
	}
}
