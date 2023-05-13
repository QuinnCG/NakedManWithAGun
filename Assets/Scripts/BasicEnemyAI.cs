using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
    public class BasicEnemyAI : AIController
    {
        private float FireDuration = 5f;
        private float FireCooldown = 3f;

		[SerializeField]
		private bool FollowPlayer = true;
        [SerializeField]
        private float MinDistanceToPlayer = 2.5f;
		[SerializeField]
		private bool IsBoss = false;
		[SerializeField, ShowIf("IsBoss")]
		private float AlternativeFireSpeed = 0.5f;

        private float _nextFireSessionTime;
        private float _nextCooldownTime;

        private bool _isFiring;
		private float _minDst;

		private float _roomSize;
		private Vector2 _roomCenter;

		protected override void Awake()
		{
			base.Awake();

			_minDst = Random.Range(MinDistanceToPlayer * 0.7f, MinDistanceToPlayer * 1.3f);
		}

		protected override void Start()
		{
			base.Start();

			var index = DungeonGenerator.Instance.GetIndex(transform.position.x, transform.position.y);
			var room = DungeonGenerator.GetRoom(index.Item1, index.Item2);

			_roomSize = DungeonGenerator.Instance.RoomSize;
			_roomCenter = room.transform.position;
		}

		protected override void Update()
        {
			if (IsDead) return;

            base.Update();

			if (IsBoss)
			{
				var health = GetComponent<Health>();
				if (health.Current <= health.Max * 0.5f)
				{
					FireRate = AlternativeFireSpeed;
				}
			}

			if (FollowPlayer)
			{
				if (DistanceToPlayer > _minDst)
				{
					TargetPosition = PlayerPosition;
				}
				else
				{
					TargetPosition = transform.position;
				}
			}

            if (Time.time >= _nextFireSessionTime)
            {
                _isFiring = true;
                _nextCooldownTime = Time.time + FireDuration;
            }

            if (Time.time >= _nextCooldownTime)
            {
                _isFiring = false;
                _nextFireSessionTime = Time.time + FireCooldown;
            }

			if (_isFiring)
			{
				Fire(DirectionToPlayer);
			}
        }

		protected override void OnReachTarget()
		{
			float halfSize = _roomSize / 2f;
			halfSize -= 4f;

			TargetPosition = new Vector2()
			{
				x = Mathf.Clamp(Random.Range(PlayerPosition.x - MinDistanceToPlayer, PlayerPosition.x + MinDistanceToPlayer), _roomCenter.x - halfSize, _roomCenter.x + halfSize),
				y = Mathf.Clamp(Random.Range(PlayerPosition.y - MinDistanceToPlayer, PlayerPosition.y + MinDistanceToPlayer), _roomCenter.y - halfSize, _roomCenter.y + halfSize),
			};

			GizmosHelper.DrawCircle(TargetPosition, 0.2f, Color.red, 10f);
		}
	}
}
