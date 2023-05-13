using UnityEngine;

namespace Quinn
{
    public class StartScreen : MonoBehaviour
    {
		private static bool GameStarted;

		private void Start()
		{

			if (!GameStarted)
			{
				GameStarted = true;

				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;

				Player.Instance.gameObject.SetActive(false);
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void Update()
		{
			if (Input.anyKeyDown)
			{
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Confined;

				Player.Instance.gameObject.SetActive(true);
				Destroy(gameObject);
			}
		}
	}
}
