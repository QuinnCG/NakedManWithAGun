using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Quinn
{
	public class ProjectileController : MonoBehaviour
	{
		private const float PROJECTILE_DESPAWN_DISTANCE = 50f;

		public ProjectileData ProjectileData { get; set; }
		public bool IsPlayerTeam { get; set; } = false;
		public Vector2 Direction { get; set; }

		public event System.Action<ProjectileController> OnDeath;

		private float _collisionRadius = 0.5f;
		private float _timeOffset;
		private float _spinDir;

		private float _nextCollisionTime;

		private void Update()
		{
			// Spin.
			transform.rotation = Quaternion.AngleAxis(Time.time * ProjectileData.SpinSpeed * _spinDir, Vector3.forward);

			// Movement.
			var playerPos = Player.Instance.transform.position;
			Vector2 vecToPlayer = playerPos - transform.position;

			// Rotation.
			Direction = Quaternion.AngleAxis(ProjectileData.TurnRate * Time.deltaTime, Vector3.forward) * Direction;

			var finalDir = Direction;
			finalDir += ProjectileData.HomingFactor * Mathf.Max(0f, 1f - (vecToPlayer.magnitude / ProjectileData.HomingRange)) * vecToPlayer.normalized;

			transform.position += ProjectileData.Speed * Time.deltaTime * (Vector3)finalDir;

			Direction.Normalize();
			var perpendicularDir = new Vector2(-Direction.y, Direction.x);

			var waveOffset =
				ProjectileData.WaveAmplitude
				* Time.deltaTime
				* Mathf.Sin((Time.time + _timeOffset) * ProjectileData.WaveFrequency)
				* (Vector3)perpendicularDir;

			transform.position += waveOffset;

			if (ProjectileData.RotateToDirection)
			{
				float angle = Mathf.Atan2(Direction.y, Direction.x);
				transform.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
			}
		}

		private void FixedUpdate()
		{
			if (Time.time < _nextCollisionTime) return;

			var colliders = Physics2D.OverlapCircleAll(transform.position, _collisionRadius);
			foreach (var collider in colliders)
			{
				if (collider.TryGetComponent(out Damage damage))
				{
					if ((IsPlayerTeam && collider.gameObject.layer != LayerMask.NameToLayer("Player"))
						|| (!IsPlayerTeam && collider.gameObject.layer != LayerMask.NameToLayer("Enemy")))
					{
						if (damage.ApplyDamage(ProjectileData.Damage, transform.position))
						{
							PrefabOnHit(damage.transform);

							Destroy(gameObject);
							_nextCollisionTime = Time.time + 0.1f;
							break;
						}
					}
				}
				else if (collider.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
				{
					if (!collider.gameObject.GetComponent<Collider2D>().isTrigger)
					{
						if (ProjectileData.Ricochet)
						{
							Direction *= -1f;
						}
						else
						{
							PrefabOnHit(transform);
							Destroy(gameObject);
						}

						_nextCollisionTime = Time.time + 0.1f;
						break;
					}
				}
			}
		}

		private void OnDestroy()
		{
			OnDeath?.Invoke(this);
		}

		public static ProjectileController[] SpawnProjectile(Vector2 origin, ProjectileData projectileData, Vector2 direction, bool playerTeam = false, int count = 1, float spread = 0f)
		{
			if (count <= 1)
			{
				// Single shot.
				var instance = Addressables.InstantiateAsync("Assets/Prefabs/Projectile.prefab").WaitForCompletion();
				var projectile = instance.GetComponent<ProjectileController>();

				var dir = Quaternion.AngleAxis(Random.Range(-(spread / 2f), spread / 2f), Vector3.forward) * direction;

				projectile.transform.position = origin;
				projectile.ProjectileData = projectileData;
				projectile.Direction = dir;
				projectile.IsPlayerTeam = playerTeam;
				projectile._timeOffset = Random.Range(0f, 2f);

				projectile.Initialize();

				var spriteRenderer = projectile.GetComponent<SpriteRenderer>();
				var col = spriteRenderer.color;
				col.a = projectileData.Opacity;
				spriteRenderer.color = col;

				if (projectileData.FireClip && !projectileData.Loop)
				{
					if (Random.Range(0f, 1f) <= projectileData.FireSoundChance)
						RandomAudioClip.Play(projectileData.FireClip, origin);
				}

				return new ProjectileController[] { projectile };
			}
			else
			{
				// Multi, shotgun-style.
				float angleOffset = spread / count;

				var projectiles = new List<ProjectileController>();

				for (int i = 0; i < count; i++)
				{
					var key = "Assets/Prefabs/Projectile.prefab";
					var instance = Addressables.InstantiateAsync(key).WaitForCompletion();
					var projectile = instance.GetComponent<ProjectileController>();

					float angle = (angleOffset * i) - (spread / 2f);
					var dir = Quaternion.AngleAxis(angle, Vector3.forward) * direction;

					projectile.transform.position = origin;
					projectile.ProjectileData = projectileData;
					projectile.Direction = dir;
					projectile.IsPlayerTeam = playerTeam;
					projectile._timeOffset = Random.Range(0f, 2f);

					projectile.Initialize();

					if (projectileData.FireClip && !projectileData.Loop)
						RandomAudioClip.Play(projectileData.FireClip, origin);

					projectiles.Add(projectile);
				}

				return projectiles.ToArray();
			}
		}

		public void Initialize()
		{
			Debug.Assert(ProjectileData != null);

			var spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.sprite = ProjectileData.Sprite;
			spriteRenderer.material.SetFloat("_Glow", ProjectileData.Glow);

			Direction.Normalize();
			_collisionRadius = ProjectileData.CollisionSize;

			if (ProjectileData.RandomDirection)
			{
				int chance = Random.Range(0, 2);
				_spinDir = chance == 0 ? -1f : 1f;
			}
			else _spinDir = ProjectileData.SpinClockwise ? 1f : -1f;

			Destroy(gameObject, 1f / ProjectileData.Speed * PROJECTILE_DESPAWN_DISTANCE);
		}

		private void PrefabOnHit(Transform transform)
		{
			if (ProjectileData.PrefabOnHit)
			{
				if (Random.Range(0f, 1f) <= ProjectileData.PrefabOnHitChance)
				{
					var instance = Instantiate(ProjectileData.PrefabOnHit);
					instance.transform.position = transform.position;
				}
			}
		}
	}
}
