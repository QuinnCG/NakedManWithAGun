using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Quinn
{
    public class AcidPuddle : MonoBehaviour
    {
		[SerializeField]
		private int DamagePerTick = 25;
		[SerializeField]
		private float TickInterval = 2f;

		[Space, SerializeField, MinMaxSlider(0f, 20f, ShowFields = true)]
		private Vector2 Amplitude = new(1f, 1f);
		[SerializeField]
		private float Frequency = 1f;

		private readonly List<Collider2D> _colliders = new();
		private SpriteRenderer _spriteRenderer;

		private void Awake()
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
		}

		private void FixedUpdate()
		{
			float ratio = (Mathf.Sin(Time.time * Frequency) + 1f) / 2f;
			_spriteRenderer.material.SetFloat("_Glow", Mathf.Lerp(Amplitude.x, Amplitude.y, ratio));
		}

		private void OnTriggerStay2D(Collider2D collision)
		{
			if (!_colliders.Contains(collision))
			{
				if (collision.TryGetComponent(out Damage damage) && collision.gameObject.TryGetComponent(out AIController ai))
				{
					damage.ApplyDamage(DamagePerTick, transform.position);
					ai.OverrideMoveSpeed = ai.MoveSpeed * 0.2f;

					_colliders.Add(collision);
					Task.Run(async () =>
					{
						await Task.Delay(System.TimeSpan.FromSeconds(TickInterval));
						_colliders.Remove(collision);
					});
				}
			}
		}

		private void OnTriggerExit2D(Collider2D collision)
		{
			if (collision.TryGetComponent(out AIController ai))
			{
				ai.OverrideMoveSpeed = -1f;
			}
		}
	}
}
