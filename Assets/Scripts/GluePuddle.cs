using UnityEngine;

namespace Quinn
{
    public class GluePuddle : MonoBehaviour
    {
		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (collision.TryGetComponent(out AIController ai))
			{
				ai.OverrideMoveSpeed = ai.MoveSpeed * 0.1f;
			}
		}

		private void OnTriggerExit2D(Collider2D collision)
		{
			if (collision.TryGetComponent(out AIController ai))
			{
				ai.OverrideMoveSpeed = -1f;
			}
		}
	}
}
