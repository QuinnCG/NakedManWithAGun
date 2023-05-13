using Sirenix.OdinInspector;
using UnityEngine;

namespace Quinn
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class DroppedGun : MonoBehaviour, IInteractable
    {
        [field: SerializeField, Required, AssetsOnly]
        public GunData Gun { get; set; }
        [PropertyTooltip("Set to -1 to make the gun have its magazine fully loaded.")]
        [field: SerializeField]
        public int AmmoInMagazine { get; set; } = -1;
        [field: SerializeField]
        public int AmmoInReserve { get; set; }

		public bool DroppedByPlayer { get; set; } = false;

        private void Start()
        {
            if (Gun != null)
            {
                GetComponent<SpriteRenderer>().sprite = Gun.DroppedSprite;

				if (AmmoInMagazine == -1)
				{
					AmmoInMagazine = Gun.MagazineSize;
				}

				if (!DroppedByPlayer)
				{
					AmmoInReserve = Gun.ReserveAmmo;
				}
			}
		}

        public void OnInteract(Player player)
        {
			// Disabled the ability to pick up guns.
			//if (player.CanEquip())
			//{
			//	player.PickupGun(this);
			//}
        }
    }
}
