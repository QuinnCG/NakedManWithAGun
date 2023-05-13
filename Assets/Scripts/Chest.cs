using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Quinn
{
	[RequireComponent(typeof(SpriteRenderer))]
	public class Chest : MonoBehaviour, IInteractable
	{
		[SerializeField, Required]
		private Sprite OpenedSprite;
		[SerializeField, Required]
		private Transform SpawnPoint;

		public event System.Action OnOpened;
		public bool IsBossChest { private get; set; }
		public bool IsOpened { get; private set; }

		private SpriteRenderer _spriteRenderer;

		private void Awake()
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
		}

		public void OnInteract(Player player)
		{
			if (IsOpened) return;
			IsOpened = true;

			OnOpened?.Invoke();

			if (IsBossChest)
			{
				Destroy(gameObject);
			}
			else
			{
				_spriteRenderer.sprite = OpenedSprite;
				GetComponent<AudioSource>().Play();

				var key = "Assets/Prefabs/HealthPotion.prefab";
				var instance = Addressables.InstantiateAsync(key).WaitForCompletion();
				instance.transform.position = SpawnPoint.position;
			}
		}
	}
}
