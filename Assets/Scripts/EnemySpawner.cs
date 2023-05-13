using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Quinn
{
	public class EnemySpawner : MonoBehaviour
	{
		private const float ROOM_PADDING = 6f;

		private Room _room;
		private EnemyWave[] _waves;

		private float _roomSize;
		private readonly List<Health> _aliveEnemies = new();
		private int _waveIndex;

		private RandomAudioClip _spawnSound;
		private readonly List<Vector2> _lastTwoSpawnPositions = new();
		private readonly List<ProjectileController> _projectiles = new();

		private void Awake()
		{
			_spawnSound = Addressables.LoadAssetAsync<RandomAudioClip>("Assets/Audio/RandomClips/EnemySpawn.asset").WaitForCompletion();
		}

		public void Initialize(Room room, EnemyWave[] waves)
		{
			_room = room;
			_waves = waves;

			_roomSize = DungeonGenerator.Instance.RoomSize;
			_room.OnPlayerEnter += () => StartCoroutine(SpawnNextWave(spawnImmediately: true));
		}

		private IEnumerator SpawnNextWave(bool spawnImmediately = false, bool forceSpawn = false)
		{
			var wave = _waves[_waveIndex];
			var bosses = wave.EnemySpawns.Where(x => x.IsBoss).ToArray();
			var boss = bosses.Length > 0 ? bosses[0] : null;

			if (_waveIndex == 0 && !forceSpawn && boss != null)
			{
				if (boss.BossMusic != null)
					GameManager.StopMusic();

				var instance = Addressables.InstantiateAsync("Assets/Prefabs/Chest.prefab").WaitForCompletion();
				var chest = instance.GetComponent<Chest>();
				chest.transform.position = transform.position;

				chest.IsBossChest = true;
				chest.OnOpened += () =>
				{
					StartCoroutine(SpawnNextWave(forceSpawn: true));
					if (boss.BossMusic != null)
						GameManager.StartBossMusic(boss.BossMusic);
				};

				yield break;
			}

			foreach (var enemySpawn in wave.EnemySpawns)
			{
				var spawnSFX = enemySpawn.Prefab.GetComponent<AIController>().SpawnSound;
				if (spawnSFX)
					RandomAudioClip.Play(spawnSFX, Camera.main.transform.position, Camera.main.transform);

				int count = Random.Range(Mathf.Max(0, enemySpawn.Count.x), enemySpawn.Count.y);
				if (enemySpawn.IsBoss) count = 1;

				for (int i = 0; i < count; i++)
				{
					if (spawnImmediately)
					{
						Spawn(enemySpawn, GetRandomPositionInRoom(), instantSpawn: true);
					}
					else
					{
						yield return new WaitForSeconds(Random.Range(0f, 0.5f));

						var key = "Assets/Prefabs/SpawnPortal.prefab";
						var instance = Addressables.InstantiateAsync(key, transform).WaitForCompletion();
						var portal = instance.GetComponent<SpawnPortal>();

						var pos = GetRandomPositionInRoom();
						portal.transform.position = pos;

						StartCoroutine(DelayedSpawn(enemySpawn, pos, portal));
					}
				}
			}

			_waveIndex++;
		}

		private IEnumerator DelayedSpawn(EnemySpawnData spawnData, Vector2 position, SpawnPortal portal)
		{
			RandomAudioClip.Play(_spawnSound, Camera.main.transform.position);

			yield return new WaitForSeconds(1f);
			var instance = Spawn(spawnData, position);

			var ai = instance.GetComponent<AIController>();
			ai.enabled = false;

			yield return new WaitForSeconds(0.5f);
			if (ai)
				ai.enabled = true;

			yield return new WaitForSeconds(0.5f);
			portal.Close();
		}

		private Vector2 GetRandomPositionInRoom()
		{
			var origin = transform.position;
			var halfSize = _roomSize / 2f;

			Vector2 pos;
			bool tooClose = false;
			int attemptCount = 0;

			do
			{
				pos = new Vector2()
				{
					x = Random.Range(origin.x - halfSize + ROOM_PADDING, origin.x + halfSize - ROOM_PADDING),
					y = Random.Range(origin.y - halfSize + ROOM_PADDING, origin.y + halfSize - ROOM_PADDING)
				};

				foreach (var p in _lastTwoSpawnPositions)
				{
					if (Vector2.Distance(p, pos) <= 0.6f)
					{
						tooClose = true;
						break;
					}
				}

				attemptCount++;
				if (attemptCount >= 100)
				{
					break;
				}
			}
			while (tooClose);

			_lastTwoSpawnPositions.Add(pos);
			if (_lastTwoSpawnPositions.Count > 2)
			{
				_lastTwoSpawnPositions.RemoveAt(0);
			}

			return pos;
		}

		private GameObject Spawn(EnemySpawnData spawnData, Vector2 position, bool instantSpawn = false)
		{
			var instance = Instantiate(spawnData.Prefab, position, Quaternion.identity, transform);
			var health = instance.GetComponent<Health>();

			if (spawnData.IsBoss)
			{
				GameManager.SetBossBar(spawnData, health);
			}

			instance.GetComponent<AIController>().OnFireProjectiles += projectiles =>
			{
				_projectiles.AddRange(projectiles);
			};

			_aliveEnemies.Add(health);
			health.OnDeath += () =>
			{
				if (_aliveEnemies.Contains(health))
				{
					_aliveEnemies.Remove(health);

					if (spawnData.IsBoss)
					{
						GameManager.HideBossBar();
						GameManager.StartNormalMusic();
						while(_aliveEnemies.Count > 0)
						{
							_aliveEnemies[0].Kill();
						}
					}

					if (_aliveEnemies.Count == 0 && _waveIndex < _waves.Length)
					{
						StartCoroutine(SpawnNextWave());
					}
					else if (_aliveEnemies.Count == 0)
					{
						foreach (var projectile in _projectiles)
						{
							if (projectile)
							{
								RandomAudioClip.Play(GameManager.Instance.ProjectileDespawn, projectile.transform.position);
								Destroy(projectile.gameObject);
							}
						}
						_projectiles.Clear();

						_room.OpenDoors();
					}
				}
			};

			return instance;
		}
	}
}
