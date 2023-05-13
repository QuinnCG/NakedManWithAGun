using UnityEngine;

namespace Quinn
{
    public class UIAnimator : MonoBehaviour
    {
		public bool Position = false;
		public bool Rotation = false;
		public bool Scale = true;
		public float TimeOffset = 0f;
		public float MinAmp = 0.9f;
		public float MaxAmp = 1.1f;
		public float Frequency = 1f;

        void Update()
		{
			if (Position)
				transform.localPosition = Vector3.up * Mathf.Lerp(MinAmp, MaxAmp, (Mathf.Sin((Time.time + TimeOffset) * Frequency) + 1f) / 2f);

			if (Rotation)
				transform.localRotation = Quaternion.AngleAxis(Mathf.Lerp(MinAmp, MaxAmp, (Mathf.Sin((Time.time + TimeOffset) * Frequency) + 1f) / 2f), Vector3.forward);

			if (Scale)
				transform.localScale = Vector3.one * Mathf.Lerp(MinAmp, MaxAmp, (Mathf.Sin((Time.time + TimeOffset) * Frequency) + 1f) / 2f);
		}
    }
}
