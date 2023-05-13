using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Quinn
{
    public class GameManager : SerializedMonoBehaviour
    {
        public static GameManager Instance { get; private set; }

		public static int Difficulty { get; private set; } = 0;
		public static AudioMixer SFXMixer { get; private set; }
		public static AudioMixer MusicMixer { get; private set; }

		public static bool IsFirstRound { get; private set; } = true;

		public DifficultyState[] Difficulties;

		private static int _roomGenDelta = -1;

		[SerializeField, Required]
		private TextMeshProUGUI BossTitle;
		[SerializeField, Required]
		private Slider BossBar;

		[Space, SerializeField, Required]
		private AudioSource MusicSource;
		[SerializeField, Required]
		private AudioClip DefaultSoundtrack;

		[field: SerializeField, Space, Required]
		public RandomAudioClip ProjectileDespawn { get; private set; }

		private Health _bossHealth = null;

		public static void OnRoomGen()
		{
			_roomGenDelta++;

			var currentState = Instance.Difficulties[Mathf.Min(Difficulty, Instance.Difficulties.Length - 1)];
			int threshold = currentState.CustomRoomCount > -1 ? currentState.CustomRoomCount : 2;

			if (_roomGenDelta >= threshold)
			{
				_roomGenDelta = 0;
				Difficulty++;
			}
		}

		public static void ReloadScene()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			IsFirstRound = false;
		}

		public static void SetBossBar(EnemySpawnData spawnData, Health health)
		{
			Instance.BossTitle.text = spawnData.BossTitle;
			Instance.BossTitle.color = spawnData.BossTitleColor;

			Instance._bossHealth = health;

			Instance.BossBar.gameObject.SetActive(true);
			Instance.BossTitle.gameObject.SetActive(true);
		}

		public static void HideBossBar()
		{
			Instance.BossBar.gameObject.SetActive(false);
			Instance.BossTitle.gameObject.SetActive(false);

			Instance._bossHealth = null;
		}

		public static void StopMusic()
		{
			Instance.MusicSource.Stop();
		}

		public static void StartBossMusic(AudioClip music)
		{
			Instance.MusicSource.clip = music;
			Instance.MusicSource.Play();
		}

		public static void StartNormalMusic()
		{
			Instance.MusicSource.clip = Instance.DefaultSoundtrack;
			Instance.MusicSource.Play();
		}

		private void Awake()
		{
			Instance = this;
		}

		private void Start()
		{
			SFXMixer = Addressables.LoadAssetAsync<AudioMixer>("Assets/Audio/SFXMixer.mixer").WaitForCompletion();
			SFXMixer = Addressables.LoadAssetAsync<AudioMixer>("Assets/Audio/MusicMixer.mixer").WaitForCompletion();
		}

		private void Update()
		{
			if (_bossHealth)
			{
				Instance.BossBar.value = (float)_bossHealth.Current / _bossHealth.Max;
			}
		}

		private void OnDestroy()
		{
			Difficulty = 0;
			_roomGenDelta = 0;

			SFXMixer = null;
			MusicMixer = null;
		}
	}
}
