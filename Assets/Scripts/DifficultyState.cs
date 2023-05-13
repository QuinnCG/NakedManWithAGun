using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
	[System.Serializable]
	public class DifficultyState
	{
		public EnemySpawnData[] Enemies;
		[MinMaxSlider(0f, 20f, showFields: true)]
		public Vector2Int WaveCount;
		public int CustomRoomCount = -1;
	}
}
