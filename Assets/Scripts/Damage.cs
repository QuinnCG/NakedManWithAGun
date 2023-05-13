using System;
using System.Collections;
using UnityEngine;

namespace Quinn
{
	[RequireComponent(typeof(SpriteRenderer))]
    public class Damage : MonoBehaviour
    {
		[SerializeField]
		private float ImmunityDuration = 0f;
		[SerializeField]
		private bool FlickerOnDamage;

		public bool CanTakeDamage { get; set; } = true;
		public bool IsImmune { get => Time.time < _nextVulnerableTime; }

        public event Action<DamageInfo> OnDamage;

		private SpriteRenderer _spriteRenderer;
		private float _nextVulnerableTime;
		private float _hurtEndTime;

		private void Awake()
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
		}

		private void Update()
		{
			if (Time.time >= _hurtEndTime)
			{
				_spriteRenderer.material.SetFloat("_WhiteFactor", 0f);
			}
		}

		public bool ApplyDamage(int amount, Vector2 origin)
        {
			if (!CanTakeDamage || Time.time < _nextVulnerableTime) return false;
			if (ImmunityDuration > 0f)
			{
				_nextVulnerableTime = Time.time + ImmunityDuration;
			}

			if (FlickerOnDamage) StartCoroutine(FlickerSequence());

            var info = new DamageInfo()
            {
                Damage = amount,
                Origin = origin
            };

			_spriteRenderer.material.SetFloat("_WhiteFactor", 1f);
			_hurtEndTime = Time.time + 0.1f;

            OnDamage?.Invoke(info);
			return true;
        }

		private IEnumerator FlickerSequence()
		{
			float delta = 0f;
			bool toggle = false;

			for (float t = 0f; t < ImmunityDuration; t += Time.deltaTime)
			{
				if (delta > 0.2f)
				{
					delta = 0f;
					_spriteRenderer.enabled = toggle;
					toggle = !toggle;
				}

				delta += Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			_spriteRenderer.enabled = true;
		}
	}
}
