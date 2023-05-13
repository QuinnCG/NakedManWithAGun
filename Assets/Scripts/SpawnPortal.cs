using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace Quinn
{
	public class SpawnPortal : MonoBehaviour
	{
		[SerializeField, Required]
		private AnimationClip OpenAnim, LoopAnim, CloseAnim;

		private PlayableAnimator _animator;

		private void Awake()
		{
			_animator = gameObject.AddComponent<PlayableAnimator>();
		}

		private IEnumerator Start()
		{
			_animator.Clip = OpenAnim;

			yield return new WaitForSeconds(OpenAnim.length - 0.1f);
			_animator.Clip = LoopAnim;
		}

		public void Close()
		{
			_animator.Clip = CloseAnim;
			Destroy(gameObject, CloseAnim.length);
		}
	}
}
