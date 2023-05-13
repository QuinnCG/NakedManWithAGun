using UnityEngine;
using UnityEngine.Audio;

namespace Quinn
{
	public class RandomAudioClipSource : MonoBehaviour
	{
		[field: SerializeField]
		public RandomAudioClip Clip { get; set; }
		[SerializeField]
		private bool LoopByDefault;
		[SerializeField]
		private AudioMixerGroup Mixer;

		public bool Loop { get => _source.loop; set => _source.loop = value; }

		private AudioSource _source;

		private void Awake()
		{
			_source = gameObject.AddComponent<AudioSource>();
			_source.playOnAwake = false;
			_source.outputAudioMixerGroup = Mixer;
			Loop = LoopByDefault;
		}

		public void Play()
		{
			_source.clip = Clip.Clips[Random.Range(0, Clip.Clips.Length)].Clip;
			_source.Play();
		}

		public void Stop()
		{
			_source.Stop();
		}
	}
}
