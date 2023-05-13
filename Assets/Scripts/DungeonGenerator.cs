using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Sirenix.OdinInspector;
using TMPro;

namespace Quinn
{
	public class DungeonGenerator : MonoBehaviour
	{
		public static DungeonGenerator Instance { get; private set; }

		[field: SerializeField]
		public float RoomSize { get; private set; } = 5f;
		[SerializeField]
		private float DoorChance = 0.6f;
		[SerializeField, Required]
		private CinemachineVirtualCamera VirtualCamera;

		[Space, SerializeField]
		private GameObject RoomPrefab;
		[SerializeField]
		private GameObject VerticalWallPrefab, HorizontalWallPrefab;
		[SerializeField]
		private GameObject VerticalDoorwayPrefab, HorizontalDoorwayPrefab;

		[Space, SerializeField, FoldoutGroup("UI"), Required]
		private TextMeshProUGUI RoomCount;

		private readonly Dictionary<(int, int), GameObject> _rooms = new();
		private int _roomCount = -1;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void Start()
		{
			GenerateRoom(0, 0, doorCount: 4);

			var room = DungeonGenerator.GetRoom(0, 0);
			var confiner = room.GetComponent<Collider2D>();

			VirtualCamera.GetComponent<CinemachineConfiner2D>().m_BoundingShape2D = confiner;
		}

		public static GameObject GetRoom(int x, int y)
		{
			if (Instance._rooms.TryGetValue((x, y), out var room))
			{
				return room;
			}

			return null;
		}

		public void GenerateRoom(int x, int y, int doorCount = -1)
		{
			if (RoomExists(x, y)) return;
			GameManager.OnRoomGen();
			_roomCount++;

			RoomCount.text = $"Rooms: {_roomCount}";

			var pos = new Vector2(x, y) * RoomSize;
			var instance = Instantiate(RoomPrefab, pos, Quaternion.identity, transform);

			_rooms.Add((x, y), instance);
			GenerateSides(x, y, instance.transform, doorCount);
		}

		public (int, int) GetIndex(float x, float y)
		{
			return (Mathf.RoundToInt(x / RoomSize), Mathf.RoundToInt(y / RoomSize));
		}

		private bool RoomExists(int x, int y)
		{
			return _rooms.ContainsKey((x, y));
		}

		private bool RoomExistsAdjacent(int xOrigin, int yOrigin, int xOffset, int yOffset)
		{
			return RoomExists(xOrigin + xOffset, yOrigin + yOffset);
		}

		private void GenerateSides(int x, int y, Transform parentRoom, int doorCount = -1)
		{
			var directions = new (int, int)[]
			{
			(-1, 0), (1, 0), (0, 1), (0, -1)
			};

			var roomHandle = parentRoom.GetComponent<Room>();

			int doorCounter = 0;
			foreach (var dir in directions)
			{
				if (RoomExistsAdjacent(x, y, dir.Item1, dir.Item2))
				{
					var doors = _rooms[(x + dir.Item1, y + dir.Item2)].GetComponent<Room>().Doors;
					roomHandle.Doors.AddRange(doors);
				}
				else
				{
					GameObject prefab;
					bool isDoor = false;

					var wallPrefab = dir.Item1 == 0 ? VerticalWallPrefab : HorizontalWallPrefab;
					var doorwayPrefab = dir.Item1 == 0 ? VerticalDoorwayPrefab : HorizontalDoorwayPrefab;

					if (doorCount == -1)
					{
						if (Random.Range(0f, 1f) <= DoorChance)
						{
							prefab = doorwayPrefab;
							isDoor = true;
						}
						else prefab = wallPrefab;
					}
					else
					{
						doorCounter++;
						if (doorCounter > doorCount)
							prefab = wallPrefab;
						else
						{
							prefab = doorwayPrefab;
							isDoor = true;
						}
					}

					var instance = Instantiate(prefab, parentRoom.position, Quaternion.identity, parentRoom);
					if (isDoor)
					{
						var door = instance.GetComponent<Door>();
						door.DirToNextRoom = dir;
						door.NextRoomIndex = (x + dir.Item1, y + dir.Item2);

						roomHandle.Doors.Add(door);
					}

					float xDir = dir.Item1 != 0f ? dir.Item1 : 1f;
					instance.transform.localScale = new Vector3(xDir, 1f, 1f);

					if (dir.Item1 != 0f)
					{
						instance.transform.position += new Vector3(dir.Item1, 0f, 0f) * RoomSize / 2f;
					}
					else
					{
						instance.transform.position += new Vector3(0f, dir.Item2, 0f) * RoomSize / 2f;
						instance.transform.position += Vector3.down;
					}
				}
			}
		}
	}
}
