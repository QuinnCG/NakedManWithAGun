using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Quinn
{
    [RequireComponent(typeof(Damage))]
    public class Health : MonoBehaviour
    {
		[SerializeField]
		private bool HalfHeartSave = false;
        [field: SerializeField]
        public int Max { get; set; } = 100;

		[field: SerializeField, ReadOnly]
        public int Current { get; private set; }

		public bool OnHalfHeart { get; private set; }

		public event Action<int> OnHeal;
		public event Action OnFullHealth;
		public event Action<int> OnDamage;
		public event Action OnDeath;
		public event Action OnHalfHeartSave;

		private void Awake()
        {
            GetComponent<Damage>().OnDamage += info => RemoveHealth(info.Damage);
            Current = Max;
        }

        public void AddHealth(int amount)
        {
            Current = Mathf.Min(Current + amount, Max);
            int delta = Mathf.Min(amount, Max - Current);

			OnHalfHeart = false;

            OnHeal?.Invoke(delta);
            if (delta > 0 && Current ==  Max)
            {
                OnFullHealth?.Invoke();
            }
        }

        public void FullHealth()
        {
            AddHealth(Max - Current);
        }

        public void RemoveHealth(int amount)
        {
            Current = Mathf.Max(0, Current - amount);

            OnDamage?.Invoke(Current);
            if (Current == 0)
            {
				if (HalfHeartSave && !OnHalfHeart)
				{
					OnHalfHeart = true;
					OnHalfHeartSave?.Invoke();
				}
				else if (HalfHeartSave && OnHalfHeart)
				{
					OnHalfHeart = false;
					OnDeath?.Invoke();
				}
				else
				{
					OnDeath?.Invoke();
				}
            }
        }

        public void Kill()
        {
            RemoveHealth(Current);
        }
    }
}
