using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
	[RequireComponent(typeof(SpriteRenderer))]
	public class Door : MonoBehaviour
	{
		[SerializeField, Required]
		private Sprite OpenSprite, ClosedSprite;
		[SerializeField, Required]
		private Collider2D DoorCollider;

		public bool IsOpen { get; private set; } = true;
		public (int, int) DirToNextRoom { private get; set; }
		public (int, int) NextRoomIndex { private get; set; }

		private SpriteRenderer _spriteRenderer;
		private (int, int) _playerLastRoom;

		private void Awake()
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (IsOpen && collision.gameObject.CompareTag("Player"))
			{
				var playerPos = Player.Instance.transform.position;
				_playerLastRoom = DungeonGenerator.Instance.GetIndex(playerPos.x, playerPos.y);
			}
		}

		private void OnTriggerExit2D(Collider2D collision)
		{
			if (IsOpen && collision.gameObject.CompareTag("Player"))
			{
				var playerPos = Player.Instance.transform.position;
				var currentRoom = DungeonGenerator.Instance.GetIndex(playerPos.x, playerPos.y);

				// Player has entered a new room.
				if (currentRoom != _playerLastRoom)
				{
					var index = DungeonGenerator.Instance.GetIndex(playerPos.x, playerPos.y);
					DungeonGenerator.Instance.GenerateRoom(index.Item1, index.Item2);

					var room = DungeonGenerator.GetRoom(index.Item1, index.Item2);
					var confiner = room.GetComponent<Collider2D>();

					var vcam = CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera.VirtualCameraGameObject;
					vcam.GetComponent<CinemachineConfiner2D>().m_BoundingShape2D = confiner;
				}
			}
		}

		public void Open()
		{
			if (!IsOpen)
			{
				IsOpen = true;
				_spriteRenderer.sprite = OpenSprite;
				DoorCollider.enabled = false;
			}
		}

		public void Close()
		{
			if (IsOpen)
			{
				IsOpen = false;
				_spriteRenderer.sprite = ClosedSprite;
				DoorCollider.enabled = true;
			}
		}
	}
}
