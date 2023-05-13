using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
	public class Direction : MonoBehaviour
	{
		private static readonly float[] DefaultFaceDirections = new[] { -1f, 1f };

		[SerializeField, ValueDropdown(nameof(DefaultFaceDirections))]
        private float DefaultFaceDirection = 1f;

        public float FaceDirection { get; private set; } = 1f;
		public float LookDirection { get; private set; } = -1f;

        private void Awake()
        {
            FaceDirection = Mathf.Sign(DefaultFaceDirection);
        }

        public void SetFacing(float direction)
        {
            transform.localScale = new Vector3()
            {
                x = (direction is > 0f or < 0f) ? Mathf.Sign(direction) : transform.localScale.x,
                y = 1f, z = 1f
            };

            FaceDirection = Mathf.Sign(direction);
        }

        public void SetLooking(float direction)
        {
			LookDirection = Mathf.Sign(direction);
        }
    }
}
