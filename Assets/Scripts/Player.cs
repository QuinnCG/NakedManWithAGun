using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Linq;
using Cinemachine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

namespace Quinn
{
	[RequireComponent(typeof(Locomotion))]
	[RequireComponent(typeof(Direction))]
	[RequireComponent(typeof(Damage))]
	[RequireComponent(typeof(Health))]
	[RequireComponent(typeof(PlayableAnimator))]
	public class Player : MonoBehaviour
	{
		public static Player Instance { get; private set; }

		[SerializeField, BoxGroup]
		private float MoveSpeed = 6f;
		[SerializeField, BoxGroup]
		private float InteractionRadius = 1f;
		[SerializeField, BoxGroup]
		private float FireCameraShakeDuration = 0.15f;
		[SerializeField, BoxGroup, Required]
		private Transform CrosshairTransform;
		[SerializeField, Required]
		private CinemachineVirtualCamera VirtualCamera;

		[Space, SerializeField, BoxGroup]
		private float RollSpeed = 3f;
		[SerializeField, BoxGroup]
		private float RollCooldown = 0.35f;

		[Space, SerializeField, BoxGroup, Required]
		private Transform DeathSpriteMask;
		[SerializeField, BoxGroup]
		private Vector2 DeathSpriteOffset = new(0f, 0.5f);
		[SerializeField, BoxGroup]
		private float DeathSpriteShrinkTime = 5f;
		[SerializeField, BoxGroup]
		private float DeathSequenceFadeDelay = 2f;

		[Space, SerializeField, BoxGroup, Required, AssetsOnly]
		private GameObject CrosshairPrefab;
		[SerializeField, BoxGroup, Required]
		private Volume GlobalVolume;

		[SerializeField, FoldoutGroup("Look At Target"), Required]
		private Transform LookAtTransform;
		[SerializeField, FoldoutGroup("Look At Target")]
		private float PlayerToMouseLookAtFactor = 0.3f;

		[Space, SerializeField, FoldoutGroup("Gun"), SceneObjectsOnly, Required]
		private Transform GunTransform;
		[SerializeField, FoldoutGroup("Gun")]
		private Vector2 GunOffset = new(0.5f, 0.25f);
		[SerializeField, FoldoutGroup("Gun")]
		private GunData StartingGun;
		[field: SerializeField, FoldoutGroup("Gun"), Required]
		public GunData EquippedGun { get; private set; }
		[SerializeField, FoldoutGroup("Gun"), Required]
		private Transform ProjectileSpawnPoint;

		[Space, SerializeField, FoldoutGroup("UI"), Required]
		private TextMeshProUGUI AmmoText;
		[SerializeField, FoldoutGroup("UI"), Required]
		private HorizontalLayoutGroup Hearts;
		[SerializeField, FoldoutGroup("UI"), Required]
		private TextMeshProUGUI DirectionsPrompt;

		[Space, SerializeField, FoldoutGroup("UI"), Required]
		private GameObject FullHeartPrefab;
		[SerializeField, FoldoutGroup("UI"), Required]
		private GameObject HalfHeartPrefab;
		[SerializeField, FoldoutGroup("UI"), Required]
		private GameObject EmptyHeartPrefab;
		[SerializeField, FoldoutGroup("UI"), Required]
		private Image GunIcon;
		[SerializeField, FoldoutGroup("UI"), Required]
		private TextMeshProUGUI GunName;

		[Space, SerializeField, FoldoutGroup("Effects")]
		private float _kickbackDecayRate = 0.05f;
		[SerializeField, FoldoutGroup("Effects")]
		private float _maxKickBackOffset = 0.2f;

		[Space, SerializeField, FoldoutGroup("Effects")]
		private float DamageCamShakeLowAmp = 1f;
		[SerializeField, FoldoutGroup("Effects")]
		private float DamageCamShakeMidAmp = 2f;
		[SerializeField, FoldoutGroup("Effects")]
		private float DamageCamShakeHighAmp = 3f;
		[SerializeField, FoldoutGroup("Effects")]
		private float DamageCamShakeDur = 0.3f;
		[SerializeField, FoldoutGroup("Effects")]
		private float MaxCamShakeAmp = 3.5f;

		[Space, SerializeField, FoldoutGroup("Animation"), Required]
		private AnimationClip IdleAnim;
		[SerializeField, FoldoutGroup("Animation"), Required]
		private AnimationClip MoveAnim;
		[SerializeField, FoldoutGroup("Animation"), Required]
		private AnimationClip RollAnim;
		[SerializeField, FoldoutGroup("Animation"), Required]
		private AnimationClip DeathAnim;

		[Space, FoldoutGroup("Audio"), SerializeField, Required]
		private RandomAudioClipSource FireAudioSource;
		[Space, FoldoutGroup("Audio"), SerializeField, Required]
		private RandomAudioClip FootstepSound;
		[Space, FoldoutGroup("Audio"), SerializeField, Required]
		private RandomAudioClip RollSound;
		[Space, FoldoutGroup("Audio"), SerializeField, Required]
		private RandomAudioClip GunDrop;
		[FoldoutGroup("Audio"), SerializeField, Required]
		private RandomAudioClip HurtSound;
		[FoldoutGroup("Audio"), SerializeField, Required]
		private RandomAudioClip DeathSound;
		[FoldoutGroup("Audio"), SerializeField, Required]
		private RandomAudioClip NoAmmoSound;
		[FoldoutGroup("Audio"), SerializeField, Required]
		private AudioSource MusicSource;

		public int MagazineAmmo { get; private set; }
		public int ReserveAmmo { get; private set; }

		// Components.
		private InputMap _input;
		private Locomotion _locomotion;
		private Direction _direction;
		private Damage _damage;
		private Health _health;
		private PlayableAnimator _animator;

		// Gun.
		private Vector2 _gunDirection;

		private bool _wantsToFire;
		private bool _isReloading;

		private float _gunNextFireTime;
		private float _gunKickbackOffset;

		private GunData[] _guns;
		private GunData _previousGun;
		private bool _lockFire;

		// Roll.
		private float _nextRollTime;
		private bool _isRolling;
		private Vector2 _rollDir = Vector2.right;

		// Camera shake.
		private CinemachineBasicMultiChannelPerlin _camNoise;
		private float _cameraShakeTimer;

		// Other.
		private int _lastHealth;
		private bool _isDead;
		private bool _shouldShowReloadPromptLastFrame;
		private float _showReloadPromptTime;

		private AudioSource _loopingFireSource;
		private bool _usingGamepad;
		private CapsuleCollider2D _collider;

		private void Awake()
		{
			Instance = this;
			_input = new InputMap();
			_collider = GetComponent<CapsuleCollider2D>();

			_input.PlayerMap.Fire.performed += _ =>
			{
				_wantsToFire = true;
				_lockFire = false;

				if (EquippedGun.Projectile.Loop && CanFire())
				{
					_loopingFireSource = RandomAudioClip.Play(EquippedGun.Projectile.FireClip, transform.position, GunTransform, destroyAfterClip: false);
					_loopingFireSource.loop = true;
				}

				if (!EquippedGun.IsAutomatic)
				{
					Fire();
				}
			};
			_input.PlayerMap.Fire.canceled += _ =>
			{
				_wantsToFire = false;
				if (EquippedGun.Projectile.Loop)
				{
					StopLoopingFireSound();
				}
			};
			_input.PlayerMap.Roll.performed += _ => OnRoll();
			_input.PlayerMap.Interact.performed += _ => OnInteract();
			_input.PlayerMap.Reload.performed += _ => OnReload();

			_locomotion = GetComponent<Locomotion>();
			_direction = GetComponent<Direction>();
			_damage = GetComponent<Damage>();
			_health = GetComponent<Health>();
			_animator = GetComponent<PlayableAnimator>();

			Cursor.lockState = CursorLockMode.Confined;
			Cursor.visible = false;

			MagazineAmmo = EquippedGun.MagazineSize;
			ReserveAmmo = EquippedGun.ReserveAmmo;

			_damage.OnDamage += info =>
			{
				if (_isDead) return;

				float amplitude = info.Damage switch
				{
					1 => DamageCamShakeLowAmp,
					2 => DamageCamShakeMidAmp,
					_ => DamageCamShakeHighAmp
				};

				ShakeCamera(amplitude, DamageCamShakeDur);
				RandomAudioClip.Play(HurtSound, transform.position);
			};
			_health.OnHalfHeartSave += () => ReconstructHearts(halfHeart: true);
			_health.OnDeath += () =>
			{
				if (!_isDead)
				{
					StopLoopingFireSound();

					MusicSource.Stop();
					RandomAudioClip.Play(DeathSound, transform.position);

					ReconstructHearts();

					_isDead = true;
					enabled = false;
					_damage.CanTakeDamage = false;
					StartCoroutine(DeathSequence());
				}
			};
		}

		private void OnEnable()
		{
			_input.Enable();
		}

		private void Start()
		{
			GunTransform.parent = null;
			LookAtTransform.parent = null;

			GunTransform.GetComponent<SpriteRenderer>().sprite = EquippedGun.Sprite;
			_camNoise = VirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

			_guns = Addressables.LoadAssetsAsync<GunData>("guns", null).WaitForCompletion().ToArray();

			if (GameManager.IsFirstRound)
			{
				EquipGun(StartingGun);
			}
			else
			{
				EquipGun(GetRandomGun());
			}

			UpdateGunUI();
		}

		private void Update()
		{
#if UNITY_EDITOR
			if (Input.GetKeyDown(KeyCode.H))
			{
				EquipGun(GetRandomGun());
			}
#endif

			if (Input.anyKeyDown)
			{
				CrosshairTransform.gameObject.SetActive(true);
				_usingGamepad = false;
			}

			if (Gamepad.current != null)
			{
				foreach (var control in Gamepad.current.allControls)
				{
					if (control.IsPressed())
					{
						CrosshairTransform.gameObject.SetActive(false);
						_usingGamepad = true;
						break;
					}
				}
			}

			// Croshair shows sometimes

			OnMove(_input.PlayerMap.Move.ReadValue<Vector2>());
			UpdateGunTransform();
			UpdateLookAtTargetPosition();
			UpdateLowAmmo();

			// Firing.
			if (_wantsToFire && EquippedGun.IsAutomatic)
			{
				if (EquippedGun.Projectile.Loop && CanFire() && _loopingFireSource == null)
				{
					_loopingFireSource = RandomAudioClip.Play(EquippedGun.Projectile.FireClip, transform.position, GunTransform, destroyAfterClip: false);
					_loopingFireSource.loop = true;
				}

				Fire();
			}

			// Post-processing.
			var colorAdjustment = GlobalVolume.profile.components.Where(x => x is ColorAdjustments).ToArray()[0] as ColorAdjustments;
			colorAdjustment.saturation.value = _health.Current <= 1 ? -25f : 10f;

			// Gun kickback.
			if (_gunKickbackOffset > 0f)
			{
				_gunKickbackOffset = Mathf.Max(0f, _gunKickbackOffset - (_kickbackDecayRate * Time.deltaTime));
			}

			// Camera shake.
			if (_cameraShakeTimer > 0f)
				_cameraShakeTimer = Mathf.Max(0f, _cameraShakeTimer - Time.deltaTime);
			else
				_camNoise.m_AmplitudeGain = 0f;

			// Roll.
			if (_isRolling)
			{
				_locomotion.AddVelocity(_rollDir.normalized * RollSpeed);
				_direction.SetFacing(_rollDir.x);
			}
			else
			{
				var inputDir = _input.PlayerMap.Move.ReadValue<Vector2>().normalized;
				if (inputDir.sqrMagnitude > 0f) _rollDir = inputDir;
			}

			// Crosshair.
			var newPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
			newPos.z = 0f;
			CrosshairTransform.position = newPos;

			// Ammo UI.
			string redHex = "#aa0000";
			string magazineColor = MagazineAmmo <= Mathf.Ceil(EquippedGun.MagazineSize * 0.25f) ? redHex : "white";
			string reserveColor = ReserveAmmo <= EquippedGun.ReserveAmmo * 0.25f ? redHex : "white";
			AmmoText.text = $"<color={magazineColor}>{MagazineAmmo}</color> <color={reserveColor}>({ReserveAmmo})</color>";

			// Health UI.
			if (_health.Current != _lastHealth && !_health.OnHalfHeart)
			{
				_lastHealth = _health.Current;
				ReconstructHearts();
			}

			// Directions prompt UI.
			DirectionsPrompt.enabled = false;

			var colliders = Physics2D.OverlapCircleAll(transform.position, InteractionRadius);
			foreach (var collider in colliders)
			{
				if (collider.gameObject.TryGetComponent(out Chest chest))
				{
					if (!chest.IsOpened)
					{
						DirectionsPrompt.text = _usingGamepad ? "PS: Square or Xbox: X to Open" : "E to Open";
						DirectionsPrompt.enabled = true;
						break;
					}
				}
			}

			if (MagazineAmmo == 0 && ReserveAmmo > 0 && !_isRolling)
			{
				if (!_shouldShowReloadPromptLastFrame)
				{
					_shouldShowReloadPromptLastFrame = true;
					_showReloadPromptTime = Time.time + 1f;
				}

				if (Time.time >= _showReloadPromptTime)
				{
					DirectionsPrompt.text = _usingGamepad ? "PS: Circle or Xbox: B to Reload" : "Spacebar to Reload";
					DirectionsPrompt.enabled = true;
				}
			}
			else
			{
				_shouldShowReloadPromptLastFrame = false;
			}
		}

		//private void FixedUpdate()
		//{
		//	var colliders = Physics2D.OverlapCapsuleAll(_collider.bounds.center, _collider.bounds.size, _collider.direction, 0f, LayerMask.GetMask("Enemy"));
		//	Debug.Log(colliders.Length);
		//	foreach (var collider in colliders)
		//	{
		//		if (collider.TryGetComponent(out AIController _))
		//		{
		//			Debug.Log("!");
		//			_damage.ApplyDamage(1, collider.transform.position);
		//			break;
		//		}
		//	}
		//}

		private void OnDisable()
		{
			_input.Disable();
		}

		private void StopLoopingFireSound()
		{
			if (_loopingFireSource)
			{
				_loopingFireSource.Stop();
				Destroy(_loopingFireSource.gameObject);
				_loopingFireSource = null;
			}
		}

		public void PlayFootstep()
		{
			RandomAudioClip.Play(FootstepSound, transform.position);
		}

		public bool CanEquip()
		{
			return !_isReloading;
		}

		public void PickupGun(DroppedGun droppedGun)
		{
			DropGun();

			// Pick up new.
			EquippedGun = droppedGun.Gun;
			MagazineAmmo = droppedGun.AmmoInMagazine;
			ReserveAmmo = droppedGun.AmmoInReserve;

			GunTransform.GetComponent<SpriteRenderer>().sprite = EquippedGun.Sprite;

			_gunNextFireTime = Time.time;
			Destroy(droppedGun.gameObject);
		}

		private void EquipGun(GunData gun)
		{
			try
			{
				if (gun == null) return;

				_previousGun = EquippedGun;
				EquippedGun = gun;

				MagazineAmmo = gun.MagazineSize;
				ReserveAmmo = gun.ReserveAmmo;

				StopLoopingFireSound();
				GunTransform.GetComponent<SpriteRenderer>().sprite = EquippedGun.Sprite;

				if (EquippedGun.ReloadSound != null && EquippedGun.PlayReloadOnEquip)
					RandomAudioClip.Play(EquippedGun.ReloadSound, transform.position, transform);

				_gunNextFireTime = Time.time;
				UpdateGunUI();
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"{e.GetType()}: {e.Message}");
			}
		}

		private void DropGun()
		{
			StopLoopingFireSound();
			RandomAudioClip.Play(GunDrop, transform.position, transform);

			var droppedGunInstance = Addressables.InstantiateAsync("Assets/Prefabs/DroppedGun.prefab")
				.WaitForCompletion()
				.GetComponent<DroppedGun>();

			droppedGunInstance.DroppedByPlayer = true;
			droppedGunInstance.transform.position = ProjectileSpawnPoint.position;
			droppedGunInstance.GetComponent<Rigidbody2D>()
				.AddForce(new Vector2(_direction.FaceDirection, _direction.LookDirection).normalized * 2f, ForceMode2D.Impulse);

			droppedGunInstance.transform.localScale = new Vector3(_direction.FaceDirection, 1f, 1f);

			droppedGunInstance.Gun = EquippedGun;
			droppedGunInstance.AmmoInMagazine = MagazineAmmo;
			droppedGunInstance.AmmoInReserve = ReserveAmmo;
		}

		private void UpdateGunUI()
		{
			GunIcon.sprite = EquippedGun.DroppedSprite;

			GunName.text = EquippedGun.Name;
			GunName.color = EquippedGun.NameColor;
		}

		private void ShakeCamera(float amplitude, float duration)
		{
			_camNoise.m_AmplitudeGain = Mathf.Min(_camNoise.m_AmplitudeGain + amplitude, MaxCamShakeAmp);
			_cameraShakeTimer = duration;
		}

		private void ReconstructHearts(bool halfHeart = false)
		{
			foreach (Transform child in Hearts.transform)
			{
				Destroy(child.gameObject);
			}

			if (halfHeart)
			{
				foreach (Transform child in Hearts.transform)
				{
					Destroy(child.gameObject);
				}

				Instantiate(HalfHeartPrefab, Hearts.transform);

				for (int i = 0; i < _health.Max - 1; i++)
				{
					Instantiate(EmptyHeartPrefab, Hearts.transform);
				}
			}
			else
			{
				for (int i = 0; i < _health.Current; i++)
				{
					Instantiate(FullHeartPrefab, Hearts.transform);
				}

				for (int i = 0; i < _health.Max - _health.Current; i++)
				{
					Instantiate(EmptyHeartPrefab, Hearts.transform);
				}
			}
		}

		private void OnMove(Vector2 direction)
		{
			if (!_isRolling && !_isDead)
			{
				direction.Normalize();
				_locomotion.AddVelocity(direction * MoveSpeed);

				_animator.Clip = direction.sqrMagnitude > 0f ? MoveAnim : IdleAnim;
			}
		}

		private void UpdateLowAmmo()
		{
			if (MagazineAmmo == 0)
			{
				StopLoopingFireSound();
			}

			if (MagazineAmmo == 0 && ReserveAmmo == 0)
			{
				_lockFire = true;

				var currentIndex = _guns.ToList().IndexOf(EquippedGun);
				var gun = _guns[currentIndex + 1 >= _guns.Length ? 0 : currentIndex + 1];
				_previousGun = EquippedGun;

				DropGun();
				EquipGun(GetRandomGun());
			}
		}

		private GunData GetRandomGun()
		{
			int attemptCount = 0;

			GunData gun;
			do
			{
				gun = _guns[Random.Range(0, _guns.Length)];

				if (attemptCount > 100) break;
				attemptCount++;
			}
			while (gun == EquippedGun || gun == _previousGun);

			return gun;
		}

		private void OnReload()
		{
			if (ReserveAmmo > 0 && MagazineAmmo < EquippedGun.MagazineSize && !_isReloading)
			{
				_isReloading = true;

				FireAudioSource.Stop();
				FireAudioSource.Clip = EquippedGun.ReloadSound;
				FireAudioSource.Loop = false;
				FireAudioSource.Play();

				StartCoroutine(ReloadSequence());
			}
		}

		private IEnumerator ReloadSequence()
		{
			float counter = 0f;
			while (counter < EquippedGun.ReloadDuration)
			{
				counter += Time.deltaTime;
				if (_isRolling)
				{
					_isReloading = false;
					yield return null;
				}

				yield return new WaitForEndOfFrame();
			}

			Reload();
		}

		private void Reload()
		{
			_isReloading = false;

			int amount = Mathf.Min(EquippedGun.MagazineSize - MagazineAmmo, ReserveAmmo);
			ReserveAmmo -= amount;
			MagazineAmmo += amount;
		}

		private void OnInteract()
		{
			var colliders = Physics2D.OverlapCircleAll(transform.position, InteractionRadius);
			foreach (var collider in colliders)
			{
				if (collider.TryGetComponent(out IInteractable interactable))
				{
					interactable.OnInteract(this);
					break;
				}
			}
		}

		private bool CanFire()
		{
			return Time.time >= _gunNextFireTime && MagazineAmmo - EquippedGun.Count >= 0 && !_isReloading && !_isRolling && !_isDead && !_lockFire;
		}

		private void Fire()
		{
			if (CanFire())
			{
				MagazineAmmo -= EquippedGun.Count;
				if (MagazineAmmo == 0)
				{
					RandomAudioClip.Play(NoAmmoSound, transform.position);
				}

				ProjectileSpawnPoint.localPosition = EquippedGun.SpawnOffset;
				var origin = (Vector2)ProjectileSpawnPoint.position;

				_gunNextFireTime = Time.time + EquippedGun.FireRate;
				ProjectileController.SpawnProjectile(origin, EquippedGun.Projectile, _gunDirection,
					playerTeam: true,
					count: EquippedGun.Count,
					spread: EquippedGun.Spread);

				_gunKickbackOffset = Mathf.Min(_maxKickBackOffset, _gunKickbackOffset + EquippedGun.KickbackOffset);
				ShakeCamera(EquippedGun.ScreenShakeAmplitude, FireCameraShakeDuration);
			}
			else if (MagazineAmmo == 0 && Time.time > _gunNextFireTime && !_isDead && !_isRolling)
			{
				_gunNextFireTime = Time.time + 0.3f;
				RandomAudioClip.Play(NoAmmoSound, transform.position);
			}
		}

		private void OnRoll()
		{
			if (Time.time >= _nextRollTime && !_isRolling && !_isDead)
			{
				if (ReserveAmmo - EquippedGun.Count >= 0 && MagazineAmmo < EquippedGun.MagazineSize && EquippedGun.ReloadSound)
				{
					RandomAudioClip.Play(EquippedGun.ReloadSound, transform.position);
				}

				RandomAudioClip.Play(RollSound, transform.position, transform);

				StopLoopingFireSound();

				_gunNextFireTime = Time.time;
				StartCoroutine(RollSequence());
			}
		}

		private void UpdateGunTransform()
		{
			if (_isDead) return;

			if (_usingGamepad)
			{
				if (Gamepad.current != null)
				{
					var value = Gamepad.current.rightStick.value.normalized;

					if (value.magnitude > 0.2f)
					{
						_gunDirection = value;
						_gunDirection.Normalize();
					}
				}
			}
			else
			{
				// Get input delta.
				if (Mouse.current != null)
				{
					var mouseDelta = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);

					if (mouseDelta != Vector3.zero)
					{
						_gunDirection = mouseDelta - transform.position;
						_gunDirection.Normalize();
					}
				}
			}

			// Gun rotation.
			var lookDir = GunTransform.position - (transform.position + (Vector3.up * GunOffset.y));
			//var lookDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - GunTransform.position;
			lookDir.Normalize();

			// Gun position.
			float offset = GunOffset.x - _gunKickbackOffset;
			GunTransform.position = (Vector2)transform.position + (_gunDirection * offset) + (Vector2.up * GunOffset.y);

			float angle = Mathf.Atan2(lookDir.y, lookDir.x);
			GunTransform.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);

			// Update character facing direction.
			var dirToGun = GunTransform.position - transform.position;
			dirToGun.Normalize();

			// Gun scale.
			GunTransform.localScale = new Vector3(1f, _direction.FaceDirection, 1f);

			_direction.SetFacing(lookDir.x);
			_direction.SetLooking(lookDir.y);
		}

		private void UpdateLookAtTargetPosition()
		{
			var mousePos = Vector2.zero;
			if (Mouse.current != null)
				mousePos = Mouse.current.position.value;

			mousePos = Camera.main.ScreenToWorldPoint(mousePos);
			mousePos = mousePos.normalized * mousePos.magnitude;

			LookAtTransform.position = Vector2.Lerp(transform.position, mousePos, PlayerToMouseLookAtFactor);
		}

		private IEnumerator RollSequence()
		{
			_animator.Clip = RollAnim;
			_isRolling = true;
			_damage.CanTakeDamage = false;
			GunTransform.gameObject.SetActive(false);

			yield return new WaitForSeconds(RollAnim.length - 0.1f);
			_nextRollTime = Time.time + RollCooldown;
			_isRolling = false;
			_damage.CanTakeDamage = true;
			GunTransform.gameObject.SetActive(true);

			Reload();
		}

		private IEnumerator DeathSequence()
		{
			GunTransform.gameObject.SetActive(false);
			_animator.Clip = DeathAnim;
			Invoke(nameof(StopAnimation), DeathAnim.length - 0.1f);

			yield return new WaitForSeconds(DeathSequenceFadeDelay);

			for (float t = 0f; t < DeathSpriteShrinkTime; t += Time.deltaTime)
			{
				DeathSpriteMask.localScale = Vector3.Lerp(Vector3.one * 5f, Vector3.zero, t / DeathSpriteShrinkTime);
				DeathSpriteMask.position = transform.position + (Vector3)(DeathSpriteOffset * (Vector2.right * transform.localScale.x));

				yield return new WaitForEndOfFrame();
			}

			GameManager.ReloadScene();
		}

		private void StopAnimation()
		{
			_animator.Stop();
		}
	}
}
