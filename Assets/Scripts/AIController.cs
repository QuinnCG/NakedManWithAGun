using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;

namespace Quinn
{
	[RequireComponent(typeof(Locomotion))]
	[RequireComponent(typeof(Health))]
	[RequireComponent(typeof(Direction))]
	public abstract class AIController : MonoBehaviour
	{
		private const float STOPPING_DISTANCE = 0.15f;
		private const float ROOM_PADDING = 1f;

		[field: SerializeField, BoxGroup]
		public float MoveSpeed { get; private set; } = 2.5f;
		[SerializeField]
		private VisualEffectAsset DeathVFX;
		[field: SerializeField, Space, BoxGroup]
		protected float FireRate { get; set; } = 0.5f;

		[Space, SerializeField, Required, FoldoutGroup("References")]
		private Transform SpawnPoint;
		[field: SerializeField, Required, AssetsOnly, FoldoutGroup("References")]
		protected ProjectileData Projectile { get; private set; }

		[Space, SerializeField, FoldoutGroup("Projectile")]
		private ProjectileSpawnMode ProjectileSpawnMode = ProjectileSpawnMode.Normal;
		[SerializeField, FoldoutGroup("Projectile")]
		private float Spread = 10f;
		[SerializeField, FoldoutGroup("Projectile")]
		private int Count = 1;

		[Space, SerializeField]
		private float DeathLingerDuration = 5f;
		[SerializeField, Required, FoldoutGroup("Animation")]
		private AnimationClip SpawnAnim;
		[SerializeField, Required, FoldoutGroup("Animation")]
		private AnimationClip IdleAnim;
		[SerializeField, Required, FoldoutGroup("Animation")]
		private AnimationClip MoveAnim;
		[SerializeField, Required, FoldoutGroup("Animation")]
		private AnimationClip DeathAnim;

		[Space, SerializeField, FoldoutGroup("Audio")]
		private RandomAudioClip DeathSound;
		[field: SerializeField, FoldoutGroup("Audio")]
		public RandomAudioClip SpawnSound { get; private set; }
		[SerializeField, FoldoutGroup("Audio")]
		private RandomAudioClip FireSound;

		public event System.Action<ProjectileController[]> OnFireProjectiles;
		public bool InstantSpawn { private get; set; }
		public float OverrideMoveSpeed { get; set; } = -1f;

		protected Vector2 TargetPosition;
		protected float DistanceToPlayer => Vector2.Distance(transform.position, _playerTransform.position);
		protected Vector2 PlayerPosition => _playerTransform.position;
		protected Vector2 DirectionToPlayer => (_playerTransform.position - transform.position).normalized;

		protected bool IsDead { get; private set; }

		private Locomotion _locomotion;
		private Health _health;
		private PlayableAnimator _animator;
		private Direction _direction;

		private Transform _playerTransform;

		private float _nextFireTime;
		private bool _canMove;

		private readonly List<ProjectileController> _projectiles = new();
		private RandomAudioClip _hurtClip;

		private float _nextStopTime;

		protected virtual void Awake()
		{
			_hurtClip = Addressables.LoadAssetAsync<RandomAudioClip>("Assets/Audio/RandomClips/EnemyHurt.asset").WaitForCompletion();

			_locomotion = GetComponent<Locomotion>();
			_health = GetComponent<Health>();
			_animator = gameObject.AddComponent<PlayableAnimator>();
			_direction = GetComponent<Direction>();

			_nextFireTime = Time.time + FireRate;
			StartCoroutine(SpawnSequence());

			_health.OnDamage += _ =>
			{
				if (IsDead) return;
				RandomAudioClip.Play(_hurtClip, transform.position);
			};

			_health.OnDeath += () =>
			{
				if (IsDead) return;
				IsDead = true;

				enabled = false;
				_health.enabled = false;

				GetComponent<Collider2D>().enabled = false;
				GetComponent<Damage>().CanTakeDamage = false;
				_canMove = false;

				_animator.Clip = DeathAnim;
				StartCoroutine(StopDeathAnim());

				foreach (var projectile in _projectiles)
				{
					Destroy(projectile.gameObject);
				}

				if (DeathVFX)
				{
					var instance = new GameObject("Particle Effect");
					var vfx = instance.AddComponent<VisualEffect>();
					vfx.visualEffectAsset = DeathVFX;
					vfx.Play();
					instance.transform.position = transform.position;
					Destroy(instance, 5f);
				}

				if (DeathSound)
					RandomAudioClip.Play(DeathSound, transform.position);

				Destroy(gameObject, DeathAnim.length + DeathLingerDuration);
			};
		}

		protected virtual void Start()
		{
			TargetPosition = transform.position;
			_playerTransform = Player.Instance.transform;
		}

		protected virtual void Update()
		{
			if (IsDead) return;

			var dir = TargetPosition - (Vector2)transform.position;
			dir.Normalize();

			_direction.SetFacing(((Vector2)(_playerTransform.position - transform.position)).normalized.x);

			if (Vector2.Distance(transform.position, TargetPosition) > STOPPING_DISTANCE)
			{
				_locomotion.AddVelocity(dir * (OverrideMoveSpeed > -1f ? OverrideMoveSpeed : MoveSpeed));
				_nextStopTime = Time.time + 0.2f;

				if (_canMove)
					_animator.Clip = MoveAnim;
			}
			else if (Time.time >= _nextStopTime)
			{
				OnReachTarget();

				if (_canMove)
					_animator.Clip = IdleAnim;
			}
		}

		protected abstract void OnReachTarget();

		protected void Fire(Vector2 direction)
		{
			if (Time.time >= _nextFireTime)
			{
				if (FireSound)
					RandomAudioClip.Play(FireSound, transform.position);

				_nextFireTime = Time.time + FireRate;
				var origin = SpawnPoint.position;

				if (ProjectileSpawnMode == ProjectileSpawnMode.Normal)
				{
					var spawnedProjectiles = ProjectileController.SpawnProjectile(origin, Projectile, direction, count: Count, spread: Spread);
					OnFireProjectiles?.Invoke(spawnedProjectiles);

					//_projectiles.AddRange(spawnedProjectiles);
					//foreach (var projectile in spawnedProjectiles)
					//{
					//	projectile.OnDeath += projectile =>
					//	{
					//		_projectiles.Remove(projectile);
					//	};
					//}
				}
				else if (ProjectileSpawnMode == ProjectileSpawnMode.Circle)
				{
					float delta = 360f / Count;

					for (int i = 0; i < Count; i++)
					{
						direction = Quaternion.AngleAxis(delta * i, Vector3.forward) * Vector2.up;
						var spawnedProjectiles = ProjectileController.SpawnProjectile(origin, Projectile, direction);
						OnFireProjectiles?.Invoke(spawnedProjectiles);

						//_projectiles.AddRange(spawnedProjectiles);
						//foreach (var projectile in spawnedProjectiles)
						//{
						//	projectile.OnDeath += projectile =>
						//	{
						//		_projectiles.Remove(projectile);
						//	};
						//}
					}
				}
				else if (ProjectileSpawnMode == ProjectileSpawnMode.Explosion)
				{
					for (int i = 0; i < Count; i++)
					{
						float angle = Random.Range(0f, 360f);

						direction = Quaternion.AngleAxis(angle * i, Vector3.forward) * Vector2.up;
						var spawnedProjectiles = ProjectileController.SpawnProjectile(origin, Projectile, direction);
						OnFireProjectiles?.Invoke(spawnedProjectiles);

						//_projectiles.AddRange(spawnedProjectiles);
						//foreach (var p in spawnedProjectiles)
						//{
						//	p.OnDeath += projectile =>
						//	{
						//		_projectiles.Remove(projectile);
						//	};
						//}
					}
				}
			}
		}

		protected Vector2 GetRandomPositionInActiveRoom()
		{
			var playerPos = Player.Instance.transform.position;
			var index = DungeonGenerator.Instance.GetIndex(playerPos.x, playerPos.y);

			var size = DungeonGenerator.Instance.RoomSize;
			var center = new Vector2(index.Item1, index.Item2) * size;

			float halfSize = size / 2f;

			var pos = new Vector2()
			{
				x = Random.Range(center.x - halfSize, center.x + halfSize),
				y = Random.Range(center.y - halfSize, center.y + halfSize)
			};

			return new Vector2()
			{
				x = Mathf.Clamp(pos.x, -halfSize + ROOM_PADDING, halfSize - ROOM_PADDING),
				y = Mathf.Clamp(pos.y, -halfSize + ROOM_PADDING, halfSize - ROOM_PADDING)
			};
		}

		protected Vector2 GetDirectionToPlayerAfterDuration(float duration)
		{
			var vel = _playerTransform.GetComponent<Locomotion>().Velocity;
			float mag = vel.magnitude * duration;

			return vel.normalized * mag;
		}

		protected void SpawnProjectileInLine(Vector2 direction, float spread)
		{
			ProjectileController.SpawnProjectile(SpawnPoint.position, Projectile, direction, spread: spread);
		}

		protected void SpawnProjectilesInCircle(int count)
		{
			float delta = 360f / Count;
			for (int i = 0; i < count; i++)
			{
				var direction = Quaternion.AngleAxis(delta * i, Vector3.forward) * Vector2.up;
				ProjectileController.SpawnProjectile(SpawnPoint.position, Projectile, direction);
			}
		}

		protected void SpawnProjectilesInArc(Vector2 direction, int count, float spread)
		{
			ProjectileController.SpawnProjectile(SpawnPoint.position, Projectile, direction, count: count, spread: spread);
		}

		protected void SpawnProjectilesInRandomDirection(int count)
		{
			for (int i = 0; i < count; i++)
			{
				float angle = Random.Range(0f, 360f);

				var direction = Quaternion.AngleAxis(angle * i, Vector3.forward) * Vector2.up;
				ProjectileController.SpawnProjectile(SpawnPoint.position, Projectile, direction);
			}
		}

		private IEnumerator SpawnSequence()
		{
			_animator.Clip = SpawnAnim;
			yield return new WaitForSeconds(SpawnAnim.length);

			if (!IsDead)
				_canMove = true;
		}

		private IEnumerator StopDeathAnim()
		{
			yield return new WaitForSeconds(DeathAnim.length - 0.1f);
			_animator.Stop();
		}
	}
}
