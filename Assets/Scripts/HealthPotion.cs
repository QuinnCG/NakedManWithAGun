using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
    public class HealthPotion : MonoBehaviour
    {
		[SerializeField, Required]
		private RandomAudioClip DrinkSound;

		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (collision.CompareTag("Player"))
			{
				var health = collision.gameObject.GetComponent<Health>();
				if (health.Current < health.Max)
				{
					RandomAudioClip.Play(DrinkSound, transform.position);

					health.FullHealth();
					Destroy(gameObject);
				}
			}
		}
	}
}
