using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
	[CreateAssetMenu(fileName = "Audio Clip", menuName = "Scriptable Objects/Audio Clip")]
    public class RandomAudioClip : ScriptableObject
    {
		[TableList, ListDrawerSettings(DraggableItems = true, ShowFoldout = true)]
		public RandomAudioClipData[] Clips;
		[MinMaxSlider(0f, 10f, ShowFields = true)]
		public Vector2 Volume = new(0.9f, 1.1f);
		[MinMaxSlider(0f, 10f, ShowFields = true)]
		public Vector2 Pitch = new(0.9f, 1.1f);

		public static AudioSource Play(RandomAudioClip clip, Vector2 position, Transform parent = null, bool destroyAfterClip = true, bool isSFX = true)
		{
			var selected = clip.Clips[Random.Range(0, clip.Clips.Length)].Clip;

			var instance = new GameObject("Audio One Shot");
			instance.transform.position = position;
			if (parent) instance.transform.parent = parent;
			var source = instance.AddComponent<AudioSource>();
			source.volume = Random.Range(clip.Volume.x, clip.Volume.y);
			source.pitch = Random.Range(clip.Pitch.x, clip.Pitch.y);
			source.clip = selected;

			if (source == null) return null;

			if (isSFX)
				source.outputAudioMixerGroup = GameManager.SFXMixer.outputAudioMixerGroup;

			if (selected != null)
				source.Play();

			if (destroyAfterClip)
				Destroy(instance, selected.length);

			return source;
		}
    }
}
