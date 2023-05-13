using UnityEngine;

namespace Quinn
{
	public class Timer
	{
		public bool Finished { get => Time.time >= NextTime; }

		public float NextTime { get; private set; }
		public float StartTime { get => NextTime - _duration; }
		public float Elapsed { get => Time.time - StartTime; }
		public float Remaining { get => _duration - Elapsed; }

		private float _duration;

		public Timer()
		{
			NextTime = 0f;
		}
		public Timer(float duration)
		{
			_duration = duration;
			NextTime = 0f;
		}

		public void Reset()
		{
			NextTime = Time.time + _duration;
		}
		public void Reset(float newDuration)
		{
			_duration = newDuration;
			NextTime = Time.time + _duration;
		}
	}
}
