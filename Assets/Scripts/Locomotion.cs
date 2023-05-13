using UnityEngine;

namespace Quinn
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Locomotion : MonoBehaviour
    {
		public Vector2 Velocity => _overrideVelocity == Vector2.zero ? _velocity : _overrideVelocity;

        private Rigidbody2D _rb;

        private Vector2 _velocity;
        private Vector2 _overrideVelocity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void LateUpdate()
        {
            _rb.velocity = _overrideVelocity != Vector2.zero
                ? _overrideVelocity : _velocity;

            _velocity = Vector2.zero;
            _overrideVelocity = Vector2.zero;
        }

        public void AddVelocity(Vector2 velocity)
        {
            _velocity += velocity;
        }

        public void SetVelocity(Vector2 velocity)
        {
            _overrideVelocity = velocity;
        }
    }
}
