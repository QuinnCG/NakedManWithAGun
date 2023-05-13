using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
    public class PaintSplatter : MonoBehaviour
    {
		[SerializeField]
		private Sprite[] Splatters;
		[SerializeField, Required]
		private RandomAudioClip SplatSound;

		private void Start()
		{
			//var circleCollider = GetComponent<CircleCollider2D>();
			//var colliders = Physics2D.OverlapCircleAll(circleCollider.bounds.center, circleCollider.radius, LayerMask.NameToLayer("PaintSplatter"));
			//if (colliders.Length > 1)
			//{
			//	Destroy(gameObject);
			//	return;
			//}

			RandomAudioClip.Play(SplatSound, transform.position);

			GetComponent<SpriteRenderer>().sprite = Splatters[Random.Range(0, Splatters.Length - 1)];
			Splatters = System.Array.Empty<Sprite>();

			transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
		}
	}
}
