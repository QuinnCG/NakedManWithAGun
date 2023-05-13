using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Quinn
{
	public class Room : MonoBehaviour
	{
		[SerializeField]
		private float DoorOpenDelay = 1f;
		[Space, SerializeField]
		private RandomAudioClip DoorsOpen;
		[SerializeField]
		private RandomAudioClip DoorsClose;

		public event System.Action OnPlayerEnter;

		public List<Door> Doors { get; } = new();

		private void Start()
		{
			if (transform.position != Vector3.zero)
			{
				OnRoomBegin();
				OnPlayerEnter?.Invoke();
			}
		}

		public void OpenDoors()
		{
			StartCoroutine(OpenDoorsAfterDelay(DoorOpenDelay));
		}

		private IEnumerator OpenDoorsAfterDelay(float delay)
		{
			yield return new WaitForSeconds(delay);
			RandomAudioClip.Play(DoorsOpen, Camera.main.transform.position, Camera.main.transform);

			foreach (var door in Doors)
			{
				door.Open();
			}

			yield return null;
		}

		private void OnRoomBegin()
		{
			var playerHealth = Player.Instance.GetComponent<Health>();
			if (playerHealth.Current < playerHealth.Max * 0.75f)
			{
				float chance = Random.Range(0f, 1f);
				if (chance <= 0.33f)
				{
					var key = "Assets/Prefabs/Chest.prefab";
					var instance = Addressables.InstantiateAsync(key).WaitForCompletion();

					instance.transform.position = transform.position;
					return;
				}
			}

			var spawner = gameObject.AddComponent<EnemySpawner>();
			var difficulty = GameManager.Difficulty + 1;

			// The difficulty value clamped to not be larger than num of unique difficulty states.
			var index = Mathf.Min(difficulty - 1, GameManager.Instance.Difficulties.Length - 1);
			// Current difficulty state.
			var difficultyState = GameManager.Instance.Difficulties[index];

			// Waves being generated.
			var waveCount = difficultyState.WaveCount;
			var waves = new EnemyWave[Random.Range(waveCount.x, waveCount.y + 1)];

			var selectedEnemies = new List<EnemySpawnData>();
			for (int i = 0; i < Mathf.Min(3, Random.Range(1, difficultyState.Enemies.Length)); i++)
			{
				EnemySpawnData selected;
				do
				{
					selected = difficultyState.Enemies[Random.Range(0, difficultyState.Enemies.Length)];
				}
				while (selectedEnemies.Contains(selected));
				selectedEnemies.Add(selected);
			}

			for (int i = 0; i < waves.Length; i++)
			{
				waves[i] = new EnemyWave()
				{
					EnemySpawns = difficultyState.Enemies
				};
			}
			
			spawner.Initialize(this, waves);
			RandomAudioClip.Play(DoorsClose, Camera.main.transform.position, Camera.main.transform);

			foreach (var door in Doors)
			{
				door.Close();
			}
		}
	}
}
