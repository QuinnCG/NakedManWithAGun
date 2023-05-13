using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Quinn
{
	public class PlayableAnimator : MonoBehaviour
	{
		[SerializeField]
		private AnimationClip DefaultClip;

		public AnimationClip Clip
		{
			get => _animClip;
			set
			{
				if (_animClip != value)
				{
					_animClip = value;
					var clip = AnimationClipPlayable.Create(_graph, value);
					_output.SetSourcePlayable(clip);

					_graph.Play();
				}
			}
		}

		private PlayableGraph _graph;
		private AnimationPlayableOutput _output;

		private AnimationClip _animClip;

		private void Awake()
		{
			_graph = PlayableGraph.Create();
			_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

			var animator = gameObject.AddComponent<Animator>();
			_output = AnimationPlayableOutput.Create(_graph, "Animation", animator);

			var clip = AnimationClipPlayable.Create(_graph, DefaultClip);
			_output.SetSourcePlayable(clip);

			_graph.Play();
		}

		private void OnDestroy()
		{
			_graph.Destroy();
		}

		public void Play()
		{
			_graph.Play();
		}

		public void Stop()
		{
			_graph.Stop();
		}
	}
}
