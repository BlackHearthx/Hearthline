using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackHearthx.Hearthline
{
	/// <summary>
	/// Carry / steal young. Detection mirrors Offspring Portal (RaycastAll + Growup),
	/// but also allows untamed Growup cubs so wild steal works.
	/// Pickup / put-down: Z key (Tick). Plain E is vanilla pet / interact.
	/// Attach is visual: AI frozen, colliders stay live so the carried animal can be hit;
	/// player↔carried collisions ignored. Damage on the carrier can force a drop.
	/// </summary>
	internal static class CubCarry
	{
		// Match Player.m_interactMask (decompiled Awake) so cubs on character_ghost still hit.
		private static readonly int InteractMask = LayerMask.GetMask(
			"item", "piece", "piece_nonsolid", "Default", "static_solid", "Default_small",
			"character", "character_net", "terrain", "vehicle", "character_ghost");

		/// <summary>Arms / chest, not skull — was (0, 1.55, 0.15) which sat on the head.</summary>
		private static readonly Vector3 CarryLocalPos = new Vector3(0.28f, 1.05f, 0.42f);
		private static readonly Quaternion CarryLocalRot = Quaternion.Euler(-10f, 110f, 8f);

		private static Character _carried;
		private static Transform _originalParent;
		private static bool _aiWasEnabled;
		private static bool _rbWasKinematic;
		private static float _animSpeed = 1f;
		private static readonly List<(Collider a, Collider b)> IgnoredPairs = new List<(Collider, Collider)>();
		private static float _nextToggle;
		private static float _nextStealAllowed;

		internal static bool IsCarrying => _carried != null;

		internal static Character Carried => _carried;

		internal static void Tick()
		{
			if (!Plugin.EnableMod.Value)
			{
				return;
			}

			Player player = Player.m_localPlayer;
			if (player == null || ((Character)player).IsDead() || ((Character)player).InIntro() || ((Character)player).IsTeleporting())
			{
				if (IsCarrying)
				{
					Vector3 fallback = player != null ? player.transform.position : Vector3.zero;
					ReleaseAt(fallback, force: true, message: string.Empty);
				}

				return;
			}

			FollowCarrier(player);

			if (!Plugin.EnableCubCarry.Value && !Plugin.ClaimWildYoung.Value)
			{
				return;
			}

			if (InputBlocked() || Time.unscaledTime < _nextToggle)
			{
				return;
			}

			if (!Input.GetKeyDown(KeyCode.Z))
			{
				return;
			}

			if (IsCarrying)
			{
				TryRelease(player);
				return;
			}

			Character target = FindCarryTargetUnderCrosshair(player, player.m_maxInteractDistance);
			if (target != null && CanCarry(target))
			{
				TryPickup(player, target);
			}
		}

		internal static string CarryHotkeyHint => "[<color=yellow>Z</color>]";

		/// <summary>Z while carrying puts down.</summary>
		internal static bool TryRelease(Player player)
		{
			if (!IsCarrying || player == null)
			{
				return false;
			}

			if (Time.unscaledTime < _nextToggle)
			{
				return true;
			}

			_nextToggle = Time.unscaledTime + 0.45f;
			ReleaseAt(DropPoint(player), force: false, message: Lines.T("released"));
			return true;
		}

		/// <summary>Called when the local carrier takes damage — drops the animal if configured.</summary>
		internal static void OnCarrierDamaged(Player player)
		{
			if (!Plugin.DropCarryOnDamage.Value || !IsCarrying || player == null || player != Player.m_localPlayer)
			{
				return;
			}

			ReleaseAt(DropPoint(player), force: false, message: Lines.T("dropped"));
		}

		internal static bool IsYoung(Character character)
		{
			if (character == null || character.IsPlayer())
			{
				return false;
			}

			bool hasGrowup = ((Component)character).GetComponent<Growup>() != null;
			bool hasTameable = ((Component)character).GetComponent<Tameable>() != null;
			bool hasProcreation = ((Component)character).GetComponent<Procreation>() != null;
			if (WildHerd.IsYoungAnimal(hasGrowup, hasTameable, hasProcreation))
			{
				return true;
			}

			return WildHerd.IsYoungPrefabName(character.gameObject != null ? character.gameObject.name : character.name);
		}

		/// <summary>Tamed adult livestock (Tameable, not a Growup cub). Wild adults are excluded.</summary>
		internal static bool IsTamedAdult(Character character)
		{
			if (character == null || character.IsPlayer() || !character.IsTamed() || IsYoung(character))
			{
				return false;
			}

			return ((Component)character).GetComponent<Tameable>() != null;
		}

		internal static bool CanCarry(Character character)
		{
			if (character == null || character.IsPlayer())
			{
				return false;
			}

			if (IsYoung(character))
			{
				return true;
			}

			return IsTamedAdult(character);
		}

		internal static Character FindCarryTargetUnderCrosshair(Player player, float maxDistance)
		{
			if (player == null || GameCamera.instance == null || IsCarrying)
			{
				return null;
			}

			Vector3 origin = GameCamera.instance.transform.position;
			Vector3 direction = GameCamera.instance.transform.forward;
			Vector3 eye = player.m_eye != null ? player.m_eye.position : ((Character)player).GetEyePoint();

			RaycastHit[] hits = Physics.RaycastAll(origin, direction, 50f, InteractMask);
			Character best = null;
			float bestDistance = maxDistance;

			foreach (RaycastHit hit in hits.OrderBy(h => h.distance))
			{
				if (hit.collider == null)
				{
					continue;
				}

				if (hit.collider.attachedRigidbody != null
				    && hit.collider.attachedRigidbody.gameObject == player.gameObject)
				{
					continue;
				}

				Character character = hit.collider.attachedRigidbody != null
					? hit.collider.attachedRigidbody.GetComponent<Character>()
					: hit.collider.GetComponentInParent<Character>();

				if (character == null || character.IsPlayer() || character == _carried || !CanCarry(character))
				{
					continue;
				}

				// Prefer young over adult when both are hit (piggy next to parent).
				float distance = Vector3.Distance(eye, hit.point);
				if (distance > bestDistance)
				{
					continue;
				}

				if (best != null && IsYoung(best) && !IsYoung(character))
				{
					continue;
				}

				best = character;
				bestDistance = distance;
			}

			return best;
		}

		/// <summary>
		/// Saddle only when the closest hit is the saddle object, not the horse body.
		/// </summary>
		internal static Sadle ClosestAimedSaddle(Player player, float maxDistance)
		{
			if (player == null || GameCamera.instance == null)
			{
				return null;
			}

			Vector3 origin = GameCamera.instance.transform.position;
			Vector3 direction = GameCamera.instance.transform.forward;
			Vector3 eye = player.m_eye != null ? player.m_eye.position : ((Character)player).GetEyePoint();
			RaycastHit[] hits = Physics.RaycastAll(origin, direction, 50f, InteractMask);

			foreach (RaycastHit hit in hits.OrderBy(h => h.distance))
			{
				if (hit.collider == null)
				{
					continue;
				}

				if (hit.collider.attachedRigidbody != null
				    && hit.collider.attachedRigidbody.gameObject == player.gameObject)
				{
					continue;
				}

				if (Vector3.Distance(eye, hit.point) > maxDistance)
				{
					continue;
				}

				Sadle saddle = hit.collider.GetComponentInParent<Sadle>();
				if (saddle == null)
				{
					return null;
				}

				Character mount = saddle.GetCharacter();
				if (mount != null && saddle.transform == mount.transform)
				{
					return null;
				}

				return saddle;
			}

			return null;
		}

		/// <summary>Backward-compatible name used by older call sites.</summary>
		internal static Character FindYoungUnderCrosshair(Player player, float maxDistance)
		{
			return FindCarryTargetUnderCrosshair(player, maxDistance);
		}

		internal static bool TryPickup(Player player, Character target)
		{
			if (player == null || target == null)
			{
				return false;
			}

			if (Time.unscaledTime < _nextToggle)
			{
				return false;
			}

			bool wasTamed = target.IsTamed();
			bool isYoung = IsYoung(target);
			bool isTamedAdult = IsTamedAdult(target);

			float staminaCost = Plugin.StealStaminaCost.Value;
			bool stealOnCooldown = !wasTamed && Time.unscaledTime < _nextStealAllowed;
			bool hasStealStamina = wasTamed || staminaCost <= 0f || player.HaveStamina(staminaCost);

			bool tameOk = true;
			if (isYoung && !wasTamed && Plugin.ClaimWildYoung.Value && Plugin.EnableCubCarry.Value && !IsCarrying
			    && !stealOnCooldown && hasStealStamina)
			{
				Tameable tameable = ((Component)target).GetComponent<Tameable>();
				tameOk = TameUtil.EnsureTamed(target, tameable);
			}

			StealCarryLogic.Outcome outcome = StealCarryLogic.Evaluate(
				alreadyCarrying: IsCarrying,
				carryEnabled: Plugin.EnableCubCarry.Value,
				claimEnabled: Plugin.ClaimWildYoung.Value,
				isYoung: isYoung,
				isTamedAdult: isTamedAdult,
				isTamed: wasTamed,
				tameSucceeded: tameOk,
				stealOnCooldown: stealOnCooldown,
				hasStealStamina: hasStealStamina);

			string message = StealCarryLogic.PlayerMessage(outcome, isYoung, Lines.Language());
			if (!string.IsNullOrEmpty(message)
			    && (outcome == StealCarryLogic.Outcome.RefuseClaimDisabled
			        || outcome == StealCarryLogic.Outcome.RefuseTameFailed
			        || outcome == StealCarryLogic.Outcome.RefuseWildAdult
			        || outcome == StealCarryLogic.Outcome.RefuseStealCooldown
			        || outcome == StealCarryLogic.Outcome.RefuseStealStamina))
			{
				((Character)player).Message(MessageHud.MessageType.Center, message);
			}

			if (outcome == StealCarryLogic.Outcome.RefuseTameFailed)
			{
				Plugin.Log.LogWarning(
					$"[Hearthline] EnsureTamed failed name={YardTables.StripClone(target.gameObject.name)}");
			}

			if (outcome == StealCarryLogic.Outcome.RefuseNotEligible
			    || outcome == StealCarryLogic.Outcome.RefuseWildAdult)
			{
				Plugin.Log.LogInfo($"[Hearthline] refuse carry {YardTables.StripClone(target.gameObject.name)} outcome={outcome}");
			}

			if (outcome != StealCarryLogic.Outcome.OkStolenCarry && outcome != StealCarryLogic.Outcome.OkCarry)
			{
				return false;
			}

			bool stole = outcome == StealCarryLogic.Outcome.OkStolenCarry;

			ZNetView nview = ((Component)target).GetComponent<ZNetView>();
			if (nview != null && nview.IsValid() && !nview.IsOwner())
			{
				nview.ClaimOwnership();
			}

			if (stole)
			{
				if (staminaCost > 0f)
				{
					player.UseStamina(staminaCost);
				}

				float cooldown = Plugin.StealCooldownSeconds.Value;
				if (cooldown > 0f)
				{
					_nextStealAllowed = Time.unscaledTime + cooldown;
				}

				StealRescue.AlertGuardians(target, player);
			}

			_carried = target;
			_originalParent = target.transform.parent;
			_nextToggle = Time.unscaledTime + 0.45f;
			Attach(player, target);
			MarkCarried(nview, carried: true);

			string hud = StealCarryLogic.PlayerMessage(outcome, isYoung, Lines.Language());
			((Character)player).Message(MessageHud.MessageType.Center, hud);

			if (Plugin.DebugLogging.Value)
			{
				Plugin.Log.LogInfo(
					$"[Hearthline] carry pickup {YardTables.StripClone(target.gameObject.name)} stole={stole} young={isYoung}");
			}

			return true;
		}

		internal static void ReleaseAt(Vector3 point, bool force, string message = "Released")
		{
			Character carried = _carried;
			if (carried == null)
			{
				return;
			}

			Detach(carried);
			if (!force)
			{
				carried.transform.position = point;
			}

			ZNetView nview = ((Component)carried).GetComponent<ZNetView>();
			MarkCarried(nview, carried: false);
			_carried = null;
			_nextToggle = Time.unscaledTime + 0.45f;

			if (string.IsNullOrEmpty(message))
			{
				return;
			}

			Player player = Player.m_localPlayer;
			if (player != null)
			{
				((Character)player).Message(MessageHud.MessageType.Center, message);
			}
		}

		private static void Attach(Player player, Character target)
		{
			Transform t = target.transform;
			t.SetParent(player.transform, worldPositionStays: false);
			t.localPosition = CarryLocalPos;
			t.localRotation = CarryLocalRot;

			// Freeze AI so pathing does not fight the parent pose — colliders stay on (can be hit).
			BaseAI ai = ((Component)target).GetComponent<BaseAI>();
			_aiWasEnabled = ai != null && ai.enabled;
			if (ai != null)
			{
				ai.enabled = false;
			}

			Rigidbody rb = ((Component)target).GetComponent<Rigidbody>();
			if (rb != null)
			{
				_rbWasKinematic = rb.isKinematic;
				rb.isKinematic = true;
				rb.detectCollisions = true;
				// Do not assign velocity on kinematic bodies (Unity 6 warning spam while carrying).
			}

			Animator anim = ((Component)target).GetComponentInChildren<Animator>();
			if (anim != null)
			{
				_animSpeed = anim.speed;
				anim.speed = 0f;
			}
			else
			{
				_animSpeed = 1f;
			}

			IgnoreCarrierCollisions(player, target, ignore: true);
		}

		private static void Detach(Character target)
		{
			Transform t = target.transform;
			t.SetParent(_originalParent, worldPositionStays: true);
			_originalParent = null;

			BaseAI ai = ((Component)target).GetComponent<BaseAI>();
			if (ai != null)
			{
				ai.enabled = _aiWasEnabled;
			}

			Rigidbody rb = ((Component)target).GetComponent<Rigidbody>();
			if (rb != null)
			{
				rb.isKinematic = _rbWasKinematic;
				rb.detectCollisions = true;
			}

			Animator anim = ((Component)target).GetComponentInChildren<Animator>();
			if (anim != null)
			{
				anim.speed = _animSpeed > 0f ? _animSpeed : 1f;
			}

			RestoreIgnoredCollisions();
		}

		private static void IgnoreCarrierCollisions(Player player, Character target, bool ignore)
		{
			RestoreIgnoredCollisions();
			if (!ignore || player == null || target == null)
			{
				return;
			}

			Collider[] playerCols = ((Component)player).GetComponentsInChildren<Collider>();
			Collider[] targetCols = ((Component)target).GetComponentsInChildren<Collider>();
			foreach (Collider a in playerCols)
			{
				if (a == null || !a.enabled)
				{
					continue;
				}

				foreach (Collider b in targetCols)
				{
					if (b == null || !b.enabled)
					{
						continue;
					}

					Physics.IgnoreCollision(a, b, true);
					IgnoredPairs.Add((a, b));
				}
			}
		}

		private static void RestoreIgnoredCollisions()
		{
			foreach ((Collider a, Collider b) in IgnoredPairs)
			{
				if (a != null && b != null)
				{
					Physics.IgnoreCollision(a, b, false);
				}
			}

			IgnoredPairs.Clear();
		}

		private static void FollowCarrier(Player player)
		{
			if (!IsCarrying || _carried == null || player == null)
			{
				return;
			}

			if (_carried.transform.parent != player.transform)
			{
				_carried.transform.SetParent(player.transform, worldPositionStays: false);
			}

			_carried.transform.localPosition = CarryLocalPos;
			_carried.transform.localRotation = CarryLocalRot;
		}

		private static void MarkCarried(ZNetView nview, bool carried)
		{
			if (nview == null || !nview.IsValid())
			{
				return;
			}

			nview.GetZDO().Set(HearthlineZdo.CubCarried, carried ? 1 : 0);
		}

		private static Vector3 DropPoint(Player player)
		{
			Vector3 origin = ((Character)player).GetEyePoint();
			Vector3 dir = ((Character)player).GetLookDir();
			if (Physics.Raycast(origin, dir, out RaycastHit hit, Plugin.CubCarryRange.Value + 2f, InteractMask))
			{
				return hit.point + Vector3.up * 0.05f;
			}

			return player.transform.position + player.transform.forward * 1.2f + Vector3.up * 0.1f;
		}

		private static bool InputBlocked()
		{
			if (Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible() || InventoryGui.IsVisible())
			{
				return true;
			}

			return Chat.instance != null && Chat.instance.HasFocus();
		}
	}
}
