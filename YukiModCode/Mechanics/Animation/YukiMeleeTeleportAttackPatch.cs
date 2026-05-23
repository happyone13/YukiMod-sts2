using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using YukiMod.YukiModCode.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YukiMod.YukiModCode.Mechanics.Vfx;

namespace YukiMod.YukiModCode.Mechanics.Animation;

[HarmonyPatch]
public static class YukiMeleeTeleportAttackPatch
{
	private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);
	private static readonly Random Rng = new Random();
	private static readonly AsyncLocal<MegaCrit.Sts2.Core.Commands.Builders.AttackCommand?> CurrentAttackCommand = new();
	private static readonly AsyncLocal<int> AttackCommandDepth = new();
	private static readonly AsyncLocal<bool> InTeleportProxy = new();

	private sealed class MeleeSession
	{
		public bool HasOrigin;
		public Vector2 OriginGlobalPos;
		public bool HasOriginFoot;
		public Vector2 OriginFootPos;
		public int LastRequestId;
		public int DamageRequestId;
		public TaskCompletionSource<bool>? DamageTcs;
		public bool DamageSignaled;
		public bool Teleported;
		public bool ReadyPlaying;
		public int ReadyWatchId;
		public int TargetSignature;
		public bool UseFootAnchor;
		public Vector2 LatestTargetCenter;
		public bool HasPlannedTeleport;
		public Vector2 PlannedTeleportGlobalPos;
		public string ActivePlayAnim = "";
		public string ActiveAttackPlayB = "";
		public string ActiveAttackPlayF = "";
		public bool ReadyTeleportedByEvent;
		public bool StepPlayerMovePlayed;
		public bool ReadyEventConnected;
		public Callable ReadyEventCb;
		public bool PlayEventConnected;
		public Callable PlayEventCb;
		public int PendingDeferredTeleportForRequestId;
		public bool Logged;
		public ChaosTeleportAttackProfile? Profile;
	}

	private static readonly ConditionalWeakTable<Creature, MeleeSession> Sessions = new();
	private sealed class FootAnchorCache
	{
		public bool Initialized;
		public Vector2 OffsetAtScale1;
	}

	private static readonly ConditionalWeakTable<Creature, FootAnchorCache> TargetFootCache = new();
	private static FieldInfo? StateMachineThisField;
	private static readonly MethodInfo? TriggerAnimMethod = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim), new[] { typeof(Creature), typeof(string), typeof(float) });
	private static readonly MethodInfo? ProxyMethod = AccessTools.Method(typeof(YukiMeleeTeleportAttackPatch), nameof(TriggerAnimProxy));

	private static MethodBase? TargetMethod()
	{
		MethodInfo? execute = AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute));
		if (execute == null)
		{
			return null;
		}

		AsyncStateMachineAttribute? attr = execute.GetCustomAttribute<AsyncStateMachineAttribute>();
		if (attr?.StateMachineType == null)
		{
			return null;
		}

		StateMachineThisField = attr.StateMachineType
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.FirstOrDefault(f => f.FieldType == typeof(AttackCommand));

		return AccessTools.Method(attr.StateMachineType, "MoveNext");
	}

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		if (TriggerAnimMethod == null || ProxyMethod == null || StateMachineThisField == null)
		{
			return instructions;
		}

		List<CodeInstruction> list = instructions.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (!list[i].Calls(TriggerAnimMethod))
			{
				continue;
			}

			list.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
			list.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, StateMachineThisField));
			i += 2;
			list[i] = new CodeInstruction(OpCodes.Call, ProxyMethod);
		}
		return list;
	}

	[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand), nameof(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand.Execute))]
	private static class AttackCommandExecuteContextPatch
	{
		public static void Prefix(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand __instance)
		{
			int d = AttackCommandDepth.Value;
			AttackCommandDepth.Value = d + 1;
			if (d == 0)
			{
				CurrentAttackCommand.Value = __instance;
			}
		}

		public static void Postfix(ref Task<MegaCrit.Sts2.Core.Commands.Builders.AttackCommand> __result)
		{
			__result = Wrap(__result);
		}

		private static async Task<MegaCrit.Sts2.Core.Commands.Builders.AttackCommand> Wrap(Task<MegaCrit.Sts2.Core.Commands.Builders.AttackCommand> inner)
		{
			try
			{
				return await inner.ConfigureAwait(false);
			}
			finally
			{
				int d = AttackCommandDepth.Value - 1;
				AttackCommandDepth.Value = d < 0 ? 0 : d;
				if (AttackCommandDepth.Value == 0)
				{
					CurrentAttackCommand.Value = null;
				}
			}
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim), new[] { typeof(Creature), typeof(string), typeof(float) })]
	private static class CreatureCmdTriggerAnimPatch
	{
		public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
		{
			if (InTeleportProxy.Value)
			{
				return true;
			}

			if (creature == null || triggerName == null)
			{
				return true;
			}

			if (!IsAttackTrigger(triggerName))
			{
				return true;
			}

			MegaCrit.Sts2.Core.Commands.Builders.AttackCommand? command = CurrentAttackCommand.Value;
			if (command == null)
			{
				return true;
			}

			if (command.ModelSource is not CardModel card)
			{
				return true;
			}

			ChaosTeleportAttackProfile? profile = ChaosTeleportAttackProfiles.Resolve(card);
			if (profile == null)
			{
				return true;
			}

			__result = RunWithTeleportProxyGuard(TryRunMeleeTeleportAttack(creature, triggerName, waitTime, command, card, profile.Value));
			return false;
		}
	}

	private static async Task RunWithTeleportProxyGuard(Task task)
	{
		bool prev = InTeleportProxy.Value;
		InTeleportProxy.Value = true;
		try
		{
			await task.ConfigureAwait(false);
		}
		finally
		{
			InTeleportProxy.Value = prev;
		}
	}

	public static Task TriggerAnimProxy(Creature creature, string triggerName, float waitTime, AttackCommand command)
	{
		if (TriggerAnimMethod == null)
		{
			return Task.CompletedTask;
		}

		if (command == null || creature == null || triggerName == null)
		{
			return Task.CompletedTask;
		}

		if (!IsAttackTrigger(triggerName))
		{
			return CreatureCmd.TriggerAnim(creature, triggerName, waitTime);
		}

		if (command.ModelSource is not CardModel card)
		{
			return CreatureCmd.TriggerAnim(creature, triggerName, waitTime);
		}

		ChaosTeleportAttackProfile? profile = ChaosTeleportAttackProfiles.Resolve(card);
		if (profile == null)
		{
			return CreatureCmd.TriggerAnim(creature, triggerName, waitTime);
		}

		try
		{
			MeleeSession session = Sessions.GetOrCreateValue(creature);
			session.Profile = profile;
			if (!session.Logged)
			{
				session.Logged = true;
				Log.Info($"[{YukiModInfo.ModId}] MeleeTeleport: card={card.Id.Entry} profile={profile.Value.Id} attacker={creature} hasSpine={(NCombatRoom.Instance?.GetCreatureNode(creature)?.HasSpineAnimation ?? false)}");
			}
		}
		catch
		{
		}

		return TryRunMeleeTeleportAttack(creature, triggerName, waitTime, command, card, profile.Value);
	}

	private static bool IsAttackTrigger(string triggerName)
	{
		return string.Equals(triggerName, "Attack", StringComparison.Ordinal)
		       || triggerName.StartsWith("Attack", StringComparison.Ordinal);
	}

	private static Task TryRunMeleeTeleportAttack(Creature attacker, string triggerName, float waitTime, AttackCommand command, CardModel card, ChaosTeleportAttackProfile profile)
	{
		try
		{
			if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out NCreature attackerNode))
			{
				WarnOnce($"node:{RuntimeHelpers.GetHashCode(attacker)}", $"[{YukiModInfo.ModId}] MeleeTeleport fallback: missing NCreature/SpineAnimation for attacker={attacker}");
				return CreatureCmd.TriggerAnim(attacker, "Attack", waitTime);
			}

			try
			{
				int key = RuntimeHelpers.GetHashCode(attacker);
				if (Warned.Add($"chaoseff:{key}"))
				{
					Marker2D? eff = GetChaosEffMarker(attackerNode);
					string scenePath = "";
					try
					{
						scenePath = attackerNode.Visuals?.SceneFilePath ?? "";
					}
					catch
					{
					}

					Node2D? visuals = null;
					try
					{
						visuals = attackerNode.Visuals;
					}
					catch
					{
					}

					string visualsGlobal = visuals != null ? visuals.GlobalPosition.ToString() : "<null>";

					if (eff != null && GodotObject.IsInstanceValid(eff))
					{
						Log.Info($"[{YukiModInfo.ModId}] ChaosEff: found=true scene='{scenePath}' local={eff.Position} global={eff.GlobalPosition} visualsGlobal={visualsGlobal} creatureGlobal={attackerNode.GlobalPosition}");
					}
					else
					{
						Log.Info($"[{YukiModInfo.ModId}] ChaosEff: found=false scene='{scenePath}' visualsGlobal={visualsGlobal} creatureGlobal={attackerNode.GlobalPosition}");
					}
				}
			}
			catch
			{
			}

			List<Creature> targets = GetAliveTargets(command);
			if (targets.Count == 0)
			{
				WarnOnce($"targets:{RuntimeHelpers.GetHashCode(attacker)}", $"[{YukiModInfo.ModId}] MeleeTeleport fallback: no alive targets attacker={attacker}");
				return CreatureCmd.TriggerAnim(attacker, "Attack", waitTime);
			}

			MeleeSession session = Sessions.GetOrCreateValue(attacker);
			session.Profile = profile;
			session.LastRequestId++;
			int requestId = session.LastRequestId;
			YukiVictoryAnimCoordinator.MarkTeleportStart(attacker);

			session.DamageRequestId = requestId;
			session.DamageTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			session.DamageSignaled = false;

			session.HasOriginFoot = false;
			session.OriginFootPos = Vector2.Zero;
			session.ReadyTeleportedByEvent = false;
			session.StepPlayerMovePlayed = false;

			try
			{
				MegaSprite? spine = attackerNode.Visuals?.SpineBody;
				if (spine != null)
				{
					if (session.ReadyEventConnected)
					{
						spine.DisconnectAnimationEvent(session.ReadyEventCb);
					}
					if (session.PlayEventConnected)
					{
						spine.DisconnectAnimationEvent(session.PlayEventCb);
					}
				}
			}
			catch
			{
			}
			session.ReadyEventConnected = false;
			session.ReadyEventCb = default;
			session.PlayEventConnected = false;
			session.PlayEventCb = default;

			if (profile.ForceLeftmostTarget)
			{
				session.UseFootAnchor = true;
				Vector2 leftFoot = GetLeftmostTargetFoot(room, targets);
				session.LatestTargetCenter = leftFoot != Vector2.Zero ? leftFoot : GetTargetsCenter(room, targets);
			}
			else
			{
				bool useTargetsCenter = profile.ForceTargetsCenter || targets.Count > 1;
				session.UseFootAnchor = !useTargetsCenter;
				session.LatestTargetCenter = useTargetsCenter ? GetTargetsCenter(room, targets) : GetSingleTargetFoot(room, targets[0]);
			}
			int signature = GetTargetSignature(targets);
			if (session.Teleported && session.TargetSignature != signature)
			{
				Vector2 newPos = session.UseFootAnchor
					? ComputeDesiredGlobalPosByFoot(attackerNode, session.LatestTargetCenter, distance: profile.TeleportDistance, attacker.Side)
					: ComputeDesiredGlobalPos(attackerNode, session.LatestTargetCenter, distance: profile.TeleportDistance, attacker.Side);
				attackerNode.GlobalPosition = newPos;
			}
			session.TargetSignature = signature;

			if (!session.HasOrigin)
			{
				session.HasOrigin = true;
				session.OriginGlobalPos = attackerNode.GlobalPosition;
			}

			try
			{
				InitializePlayVariant(attackerNode, profile, session);
			}
			catch
			{
			}

			bool hasCustom = HasCustomMeleeAnims(attackerNode, profile);

			if (!hasCustom)
			{
				if (!session.Teleported)
				{
					StartVanillaTeleportWatcher(attacker, requestId, waitTime);
				}
				else
				{
					float uniformScale = GetChaosEffUniformScale(attackerNode, profile.UniformScaleMultiplier);
					PlayAttackPlayVfx(room, GetFootPos(attackerNode), profile, session, uniformScale);
					StartVanillaEndWatcher(attacker, requestId, waitTime);
				}

				TryPlayAttackSfx(attacker);
				return CreatureCmd.TriggerAnim(attacker, triggerName, waitTime);
			}

			if (!session.Teleported)
			{
				if (!session.ReadyPlaying)
				{
					session.ReadyPlaying = true;
					session.ReadyWatchId++;
					int watchId = session.ReadyWatchId;
					session.PendingDeferredTeleportForRequestId = requestId;
					Vector2 plannedPos = session.UseFootAnchor
						? ComputeDesiredGlobalPosByFoot(attackerNode, session.LatestTargetCenter, distance: profile.TeleportDistance, attacker.Side)
						: ComputeDesiredGlobalPos(attackerNode, session.LatestTargetCenter, distance: profile.TeleportDistance, attacker.Side);
					session.HasPlannedTeleport = true;
					session.PlannedTeleportGlobalPos = plannedPos;
					attackerNode.SpineAnimation.SetAnimation(profile.ReadyAnim, loop: false);
					StartReadyWatcher(attacker, watchId);
				}
			}
			else
			{
				string playAnim = GetActivePlayAnim(attackerNode, profile, session);
				attackerNode.SpineAnimation.SetAnimation(playAnim, loop: false);
				float uniformScale = GetChaosEffUniformScale(attackerNode, profile.UniformScaleMultiplier);
				PlayAttackPlayVfx(room, GetFootPos(attackerNode), profile, session, uniformScale);
				if (string.IsNullOrWhiteSpace(profile.DamageEvent))
				{
					SignalDamage(attacker, requestId);
				}
				else
				{
					TryConnectPlayEvent(attacker, requestId);
				}
				TryAddIdleLoop(attackerNode);
				StartEndWatcher(attacker, requestId);
			}

			TryPlayAttackSfx(attacker);
			float timeout = Mathf.Max(Mathf.Max(waitTime, attackerNode.GetCurrentAnimationLength()), 0.25f) + 0.5f;
			return WaitForDamage(attacker, requestId, timeout);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{YukiModInfo.ModId}] Melee teleport fallback: {ex.GetType().Name}: {ex.Message}");
			return CreatureCmd.TriggerAnim(attacker, "Attack", waitTime);
		}
	}

	private static void StartReadyWatcher(Creature attacker, int watchId)
	{
		try
		{
			if (!TryGetRoomAndNode(attacker, out _, out NCreature attackerNode))
			{
				return;
			}

			ChaosTeleportAttackProfile profile;
			try
			{
				if (!Sessions.TryGetValue(attacker, out MeleeSession? s) || s?.Profile == null)
				{
					profile = ChaosTeleportAttackProfiles.Default;
				}
				else
				{
					profile = s.Profile.Value;
				}
			}
			catch
			{
				profile = ChaosTeleportAttackProfiles.Default;
			}

			MegaSprite? spine = attackerNode.Visuals.SpineBody;
			if (spine == null)
			{
				float len = Mathf.Max(attackerNode.GetCurrentAnimationLength(), 0.01f);
				StartReadyFallbackByTime(attacker, watchId, len);
				return;
			}

			try
			{
				if (Sessions.TryGetValue(attacker, out MeleeSession? s) && s != null && !s.HasOriginFoot)
				{
					s.HasOriginFoot = true;
					s.OriginFootPos = GetFootPos(attackerNode);
				}
			}
			catch
			{
			}

			try
			{
				if (!string.IsNullOrWhiteSpace(profile.ReadyTeleportEvent))
				{
					Callable readyEventCb = default;
					readyEventCb = Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>((a, b, c, spineEvent) =>
					{
						try
						{
							if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out NCreature n))
							{
								return;
							}
							if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
							{
								return;
							}
							if (!session.ReadyPlaying || session.ReadyWatchId != watchId)
							{
								return;
							}
							if (session.ReadyTeleportedByEvent)
							{
								return;
							}

							ChaosTeleportAttackProfile activeProfile = session.Profile ?? profile;
							string eventName = "";
							try
							{
								eventName = new MegaEvent(spineEvent).GetData().GetEventName() ?? "";
							}
							catch
							{
							}
							if (!string.Equals(eventName, activeProfile.ReadyTeleportEvent, StringComparison.Ordinal))
							{
								return;
							}

							try
							{
								if (!session.StepPlayerMovePlayed && !string.IsNullOrWhiteSpace(activeProfile.StepPlayerMoveEvent)
								    && string.Equals(eventName, activeProfile.StepPlayerMoveEvent, StringComparison.Ordinal))
								{
									float us = GetChaosEffUniformScale(n, activeProfile.UniformScaleMultiplier);
									Vector2 pos = session.HasOriginFoot ? session.OriginFootPos : GetFootPos(n);
									ChaosTeleportAttackVfxSet vfx = activeProfile.Vfx;
									PlayOneShotLayers(room, pos, vfx.StepPlayerMoveB, vfx.StepAnimBack, vfx.StepPlayerMoveF, vfx.StepAnimFront, us);
									session.StepPlayerMovePlayed = true;
								}
							}
							catch
							{
							}

							Vector2 plannedGround = session.HasPlannedTeleport
								? session.PlannedTeleportGlobalPos
								: (session.UseFootAnchor
									? ComputeDesiredGlobalPosByFoot(n, session.LatestTargetCenter, distance: activeProfile.TeleportDistance, attacker.Side)
									: ComputeDesiredGlobalPos(n, session.LatestTargetCenter, distance: activeProfile.TeleportDistance, attacker.Side));
							session.HasPlannedTeleport = true;
							session.PlannedTeleportGlobalPos = plannedGround;

							n.GlobalPosition = plannedGround + activeProfile.ReadyTeleportOffset;
							session.ReadyTeleportedByEvent = true;

							try
							{
								spine.DisconnectAnimationEvent(readyEventCb);
							}
							catch
							{
							}
							session.ReadyEventConnected = false;
							session.ReadyEventCb = default;
						}
						catch
						{
						}
					});

					try
					{
						spine.ConnectAnimationEvent(readyEventCb);
						if (Sessions.TryGetValue(attacker, out MeleeSession? session) && session != null)
						{
							session.ReadyEventConnected = true;
							session.ReadyEventCb = readyEventCb;
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}

			ConnectAnimationCompletedOnce(spine, () =>
			{
				try
				{
					if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out attackerNode))
					{
						return;
					}

					if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
					{
						return;
					}

					if (!session.ReadyPlaying || session.ReadyWatchId != watchId || session.Teleported)
					{
						return;
					}

					ChaosTeleportAttackProfile activeProfile = session.Profile ?? profile;
					float uniformScale = GetChaosEffUniformScale(attackerNode, activeProfile.UniformScaleMultiplier);
					ChaosTeleportAttackVfxSet vfx = activeProfile.Vfx;

					try
					{
						if (session.ReadyEventConnected)
						{
							spine.DisconnectAnimationEvent(session.ReadyEventCb);
						}
					}
					catch
					{
					}
					session.ReadyEventConnected = false;
					session.ReadyEventCb = default;

					Vector2 originFoot = session.HasOriginFoot ? session.OriginFootPos : GetFootPos(attackerNode);
					if (string.IsNullOrWhiteSpace(activeProfile.StepPlayerMoveEvent))
					{
						PlayOneShotLayers(room, originFoot, vfx.StepPlayerMoveB, vfx.StepAnimBack, vfx.StepPlayerMoveF, vfx.StepAnimFront, uniformScale);
						session.StepPlayerMovePlayed = true;
					}

					Vector2 plannedPos = session.HasPlannedTeleport
						? session.PlannedTeleportGlobalPos
						: (session.UseFootAnchor
							? ComputeDesiredGlobalPosByFoot(attackerNode, session.LatestTargetCenter, distance: activeProfile.TeleportDistance, attacker.Side)
							: ComputeDesiredGlobalPos(attackerNode, session.LatestTargetCenter, distance: activeProfile.TeleportDistance, attacker.Side));
					session.HasPlannedTeleport = true;
					session.PlannedTeleportGlobalPos = plannedPos;

					attackerNode.GlobalPosition = plannedPos;
					string playAnim = GetActivePlayAnim(attackerNode, activeProfile, session);
					attackerNode.SpineAnimation.SetAnimation(playAnim, loop: false);
					attackerNode.GlobalPosition = plannedPos;

					Vector2 targetFoot = GetFootPos(attackerNode);
					PlayOneShotLayers(room, targetFoot, vfx.StepTargetArriveB, vfx.StepAnimBack, vfx.StepTargetArriveF, vfx.StepAnimFront, uniformScale);
					PlayAttackPlayVfx(room, targetFoot, activeProfile, session, uniformScale);

					bool needPlayEvent = (!string.IsNullOrWhiteSpace(activeProfile.StepPlayerMoveEvent) && !session.StepPlayerMovePlayed)
					                     || !string.IsNullOrWhiteSpace(activeProfile.DamageEvent);
					if (needPlayEvent)
					{
						TryConnectPlayEvent(attacker, session.LastRequestId);
					}

					if (string.IsNullOrWhiteSpace(activeProfile.DamageEvent))
					{
						SignalDamage(attacker, session.LastRequestId);
					}

					session.Teleported = true;
					session.ReadyPlaying = false;
					StartPositionLock(attacker, session.LastRequestId, plannedPos, attackerNode.GetCurrentAnimationLength());
					TryAddIdleLoop(attackerNode);
					StartEndWatcherOnCompleted(attacker, session.LastRequestId);
				}
				catch
				{
				}
			});
		}
		catch
		{
		}
	}

	private static async void StartReadyFallbackByTime(Creature attacker, int watchId, float readyLen)
	{
		try
		{
			await Cmd.CustomScaledWait(readyLen, readyLen);

			if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out NCreature attackerNode))
			{
				return;
			}

			if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
			{
				return;
			}

			if (!session.ReadyPlaying || session.ReadyWatchId != watchId || session.Teleported)
			{
				return;
			}

			ChaosTeleportAttackProfile activeProfile = session.Profile ?? ChaosTeleportAttackProfiles.Default;
			float uniformScale = GetChaosEffUniformScale(attackerNode, activeProfile.UniformScaleMultiplier);
			ChaosTeleportAttackVfxSet vfx = activeProfile.Vfx;

			Vector2 originFoot = GetFootPos(attackerNode);
			PlayOneShotLayers(room, originFoot, vfx.StepPlayerMoveB, vfx.StepAnimBack, vfx.StepPlayerMoveF, vfx.StepAnimFront, uniformScale);

			Vector2 plannedPos = session.HasPlannedTeleport
				? session.PlannedTeleportGlobalPos
				: (session.UseFootAnchor
					? ComputeDesiredGlobalPosByFoot(attackerNode, session.LatestTargetCenter, distance: activeProfile.TeleportDistance, attacker.Side)
					: ComputeDesiredGlobalPos(attackerNode, session.LatestTargetCenter, distance: activeProfile.TeleportDistance, attacker.Side));
			session.HasPlannedTeleport = true;
			session.PlannedTeleportGlobalPos = plannedPos;

			attackerNode.GlobalPosition = plannedPos;
			string playAnim = GetActivePlayAnim(attackerNode, activeProfile, session);
			attackerNode.SpineAnimation.SetAnimation(playAnim, loop: false);
			attackerNode.GlobalPosition = plannedPos;

			Vector2 targetFoot = GetFootPos(attackerNode);
			PlayOneShotLayers(room, targetFoot, vfx.StepTargetArriveB, vfx.StepAnimBack, vfx.StepTargetArriveF, vfx.StepAnimFront, uniformScale);
			PlayAttackPlayVfx(room, targetFoot, activeProfile, session, uniformScale);

			bool needPlayEvent = (!string.IsNullOrWhiteSpace(activeProfile.StepPlayerMoveEvent) && !session.StepPlayerMovePlayed)
			                     || !string.IsNullOrWhiteSpace(activeProfile.DamageEvent);
			if (needPlayEvent)
			{
				TryConnectPlayEvent(attacker, session.LastRequestId);
			}

			if (string.IsNullOrWhiteSpace(activeProfile.DamageEvent))
			{
				SignalDamage(attacker, session.LastRequestId);
			}

			session.Teleported = true;
			session.ReadyPlaying = false;
			StartPositionLock(attacker, session.LastRequestId, plannedPos, attackerNode.GetCurrentAnimationLength());
			TryAddIdleLoop(attackerNode);
			StartEndWatcherOnCompleted(attacker, session.LastRequestId);
		}
		catch
		{
		}
	}

	private static async void StartEndWatcher(Creature attacker, int requestId)
	{
		try
		{
			if (!TryGetRoomAndNode(attacker, out _, out NCreature attackerNode))
			{
				return;
			}

			float playLen = Mathf.Max(attackerNode.GetCurrentAnimationLength(), 0.01f);
			await Cmd.CustomScaledWait(playLen, playLen);

			if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
			{
				return;
			}

			if (session.LastRequestId != requestId || !session.Teleported)
			{
				return;
			}

			if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out attackerNode))
			{
				YukiVictoryAnimCoordinator.MarkTeleportEnd(attacker);
				return;
			}

			Vector2 playEndFoot = GetFootPos(attackerNode);
			ChaosTeleportAttackProfile activeProfile = session.Profile ?? ChaosTeleportAttackProfiles.Default;
			float uniformScale = GetChaosEffUniformScale(attackerNode, activeProfile.UniformScaleMultiplier);
			PlayReturnVfxAtPlayEnd(room, playEndFoot, activeProfile, uniformScale);

			Vector2 origin = session.OriginGlobalPos;
			try
			{
				MegaSprite? spine = attackerNode.Visuals?.SpineBody;
				if (spine != null)
				{
					if (session.ReadyEventConnected)
					{
						spine.DisconnectAnimationEvent(session.ReadyEventCb);
					}
					if (session.PlayEventConnected)
					{
						spine.DisconnectAnimationEvent(session.PlayEventCb);
					}
				}
			}
			catch
			{
			}
			session.Teleported = false;
			session.HasOrigin = false;
			session.HasOriginFoot = false;
			session.OriginFootPos = Vector2.Zero;
			session.ReadyPlaying = false;
			session.TargetSignature = 0;
			session.UseFootAnchor = false;
			session.HasPlannedTeleport = false;
			session.PlannedTeleportGlobalPos = Vector2.Zero;
			session.ReadyTeleportedByEvent = false;
			session.StepPlayerMovePlayed = false;
			session.ReadyEventConnected = false;
			session.ReadyEventCb = default;
			session.PlayEventConnected = false;
			session.PlayEventCb = default;
			session.ActivePlayAnim = "";
			session.ActiveAttackPlayB = "";
			session.ActiveAttackPlayF = "";
			session.PendingDeferredTeleportForRequestId = 0;
			attackerNode.GlobalPosition = origin;
			attackerNode.SpineAnimation.SetAnimation(activeProfile.EndAnim, loop: false);
			attackerNode.GlobalPosition = origin;
			TryAddIdleLoop(attackerNode);

			Vector2 endFoot = GetFootPos(attackerNode);
			PlayReturnVfxAtOrigin(room, endFoot, activeProfile, uniformScale);
			YukiVictoryAnimCoordinator.MarkTeleportEnd(attacker);
		}
		catch
		{
		}
	}

	private static void StartEndWatcherOnCompleted(Creature attacker, int requestId)
	{
		try
		{
			if (!TryGetRoomAndNode(attacker, out _, out NCreature attackerNode))
			{
				return;
			}

			MegaSprite? spine = attackerNode.Visuals.SpineBody;
			if (spine == null)
			{
				StartEndWatcher(attacker, requestId);
				return;
			}

			ConnectAnimationCompletedOnce(spine, () =>
			{
				try
				{
					if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
					{
						return;
					}

					if (session.LastRequestId != requestId || !session.Teleported)
					{
						return;
					}

					if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out attackerNode))
					{
						YukiVictoryAnimCoordinator.MarkTeleportEnd(attacker);
						return;
					}

					ChaosTeleportAttackProfile activeProfile = session.Profile ?? ChaosTeleportAttackProfiles.Default;
					float uniformScale = GetChaosEffUniformScale(attackerNode, activeProfile.UniformScaleMultiplier);

					Vector2 playEndFoot = GetFootPos(attackerNode);
					PlayReturnVfxAtPlayEnd(room, playEndFoot, activeProfile, uniformScale);

					Vector2 origin = session.OriginGlobalPos;
					try
					{
						MegaSprite? spine = attackerNode.Visuals?.SpineBody;
						if (spine != null)
						{
							if (session.ReadyEventConnected)
							{
								spine.DisconnectAnimationEvent(session.ReadyEventCb);
							}
							if (session.PlayEventConnected)
							{
								spine.DisconnectAnimationEvent(session.PlayEventCb);
							}
						}
					}
					catch
					{
					}
					session.Teleported = false;
					session.HasOrigin = false;
					session.HasOriginFoot = false;
					session.OriginFootPos = Vector2.Zero;
					session.ReadyPlaying = false;
					session.TargetSignature = 0;
					session.UseFootAnchor = false;
					session.HasPlannedTeleport = false;
					session.PlannedTeleportGlobalPos = Vector2.Zero;
					session.ReadyTeleportedByEvent = false;
					session.StepPlayerMovePlayed = false;
					session.ReadyEventConnected = false;
					session.ReadyEventCb = default;
					session.PlayEventConnected = false;
					session.PlayEventCb = default;
					session.ActivePlayAnim = "";
					session.ActiveAttackPlayB = "";
					session.ActiveAttackPlayF = "";
					session.PendingDeferredTeleportForRequestId = 0;

					attackerNode.GlobalPosition = origin;
					attackerNode.SpineAnimation.SetAnimation(activeProfile.EndAnim, loop: false);
					attackerNode.GlobalPosition = origin;
					TryAddIdleLoop(attackerNode);

					Vector2 endFoot = GetFootPos(attackerNode);
					PlayReturnVfxAtOrigin(room, endFoot, activeProfile, uniformScale);
					YukiVictoryAnimCoordinator.MarkTeleportEnd(attacker);
				}
				catch
				{
				}
			});
		}
		catch
		{
		}
	}

	private static Task PlayAttackSfxAndWait(Creature creature, float waitTime)
	{
		TryPlayAttackSfx(creature);

		return Cmd.CustomScaledWait(Mathf.Min(waitTime * 0.5f, 0.25f), waitTime);
	}

	private static void SignalDamage(Creature attacker, int requestId)
	{
		try
		{
			if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
			{
				return;
			}
			if (session.DamageRequestId != requestId)
			{
				return;
			}
			if (session.DamageSignaled)
			{
				return;
			}
			session.DamageSignaled = true;
			session.DamageTcs?.TrySetResult(true);
		}
		catch
		{
		}
	}

	private static async Task WaitForDamage(Creature attacker, int requestId, float timeoutSeconds)
	{
		try
		{
			TaskCompletionSource<bool>? tcs = null;
			if (Sessions.TryGetValue(attacker, out MeleeSession? session) && session != null && session.DamageRequestId == requestId)
			{
				tcs = session.DamageTcs;
			}
			if (tcs == null)
			{
				return;
			}

			timeoutSeconds = Mathf.Max(timeoutSeconds, 0.05f);
			Task finished = await Task.WhenAny(tcs.Task, Task.Delay((int)(timeoutSeconds * 1000f)));
			if (finished == tcs.Task)
			{
				await tcs.Task;
			}
		}
		catch
		{
		}
	}

	private static void TryConnectPlayEvent(Creature attacker, int requestId)
	{
		try
		{
			if (!TryGetRoomAndNode(attacker, out _, out NCreature attackerNode))
			{
				return;
			}
			MegaSprite? spine = attackerNode.Visuals?.SpineBody;
			if (spine == null)
			{
				return;
			}
			if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
			{
				return;
			}
			if (session.PlayEventConnected)
			{
				return;
			}

			Callable cb = default;
			cb = Callable.From<GodotObject, GodotObject, GodotObject, GodotObject>((_, __, ___, spineEvent) =>
			{
				try
				{
					if (!Sessions.TryGetValue(attacker, out MeleeSession? s) || s == null)
					{
						return;
					}
					if (s.LastRequestId != requestId)
					{
						return;
					}

					ChaosTeleportAttackProfile p = s.Profile ?? ChaosTeleportAttackProfiles.Default;
					string eventName = "";
					try
					{
						eventName = new MegaEvent(spineEvent).GetData().GetEventName() ?? "";
					}
					catch
					{
					}

					if (!s.StepPlayerMovePlayed && !string.IsNullOrWhiteSpace(p.StepPlayerMoveEvent) && string.Equals(eventName, p.StepPlayerMoveEvent, StringComparison.Ordinal))
					{
						if (TryGetRoomAndNode(attacker, out NCombatRoom room, out NCreature n))
						{
							float us = GetChaosEffUniformScale(n, p.UniformScaleMultiplier);
							Vector2 pos = s.HasOriginFoot ? s.OriginFootPos : GetFootPos(n);
							ChaosTeleportAttackVfxSet vfx = p.Vfx;
							PlayOneShotLayers(room, pos, vfx.StepPlayerMoveB, vfx.StepAnimBack, vfx.StepPlayerMoveF, vfx.StepAnimFront, us);
							s.StepPlayerMovePlayed = true;
						}
					}

					if (!s.DamageSignaled && !string.IsNullOrWhiteSpace(p.DamageEvent) && string.Equals(eventName, p.DamageEvent, StringComparison.Ordinal))
					{
						SignalDamage(attacker, requestId);
					}

					bool stepDone = string.IsNullOrWhiteSpace(p.StepPlayerMoveEvent) || s.StepPlayerMovePlayed;
					bool dmgDone = string.IsNullOrWhiteSpace(p.DamageEvent) || s.DamageSignaled;
					if (stepDone && dmgDone)
					{
						try
						{
							spine.DisconnectAnimationEvent(cb);
						}
						catch
						{
						}
						s.PlayEventConnected = false;
						s.PlayEventCb = default;
					}
				}
				catch
				{
				}
			});

			try
			{
				spine.ConnectAnimationEvent(cb);
				session.PlayEventConnected = true;
				session.PlayEventCb = cb;
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private static void TryPlayAttackSfx(Creature creature)
	{
		try
		{
			if (creature.IsPlayer && !creature.IsDead && creature.Player != null)
			{
				SfxCmd.Play(creature.Player.Character.AttackSfx);
			}
		}
		catch
		{
		}
	}

	private static bool TryGetRoomAndNode(Creature attacker, out NCombatRoom room, out NCreature attackerNode)
	{
		room = NCombatRoom.Instance!;
		attackerNode = null!;
		if (room == null)
		{
			return false;
		}

		NCreature? n = room.GetCreatureNode(attacker);
		if (n == null || !GodotObject.IsInstanceValid(n) || !n.HasSpineAnimation)
		{
			return false;
		}
		attackerNode = n;
		return true;
	}

	private static bool HasAnim(NCreature creatureNode, string name)
	{
		try
		{
			MegaSprite? sprite = creatureNode.Visuals.SpineBody;
			return sprite != null && sprite.HasAnimation(name);
		}
		catch
		{
			return false;
		}
	}

	private static bool HasCustomMeleeAnims(NCreature creatureNode, ChaosTeleportAttackProfile profile)
	{
		bool hasReady = HasAnim(creatureNode, profile.ReadyAnim);
		bool hasEnd = HasAnim(creatureNode, profile.EndAnim);
		bool hasPlay = HasAnim(creatureNode, profile.PlayAnim);
		bool hasAltPlay = !string.IsNullOrWhiteSpace(profile.AltPlayAnim) && HasAnim(creatureNode, profile.AltPlayAnim);
		return hasReady && hasEnd && (hasPlay || hasAltPlay);
	}

	private static void TryAddIdleLoop(NCreature creatureNode)
	{
		try
		{
			_ = creatureNode.SpineAnimation.AddAnimation("idle_loop", 0f, loop: true);
		}
		catch
		{
		}
	}

	private static void InitializePlayVariant(NCreature attackerNode, ChaosTeleportAttackProfile profile, MeleeSession session)
	{
		if (!string.IsNullOrWhiteSpace(session.ActivePlayAnim))
		{
			return;
		}

		string primary = profile.PlayAnim ?? "";
		string alt = profile.AltPlayAnim ?? "";

		bool hasPrimary = !string.IsNullOrWhiteSpace(primary) && HasAnim(attackerNode, primary);
		bool hasAlt = !string.IsNullOrWhiteSpace(alt) && HasAnim(attackerNode, alt);

		bool useAlt = false;
		if (hasAlt)
		{
			if (profile.RandomizePlayAnim && hasPrimary)
			{
				useAlt = Rng.Next(2) == 0;
			}
			else if (!hasPrimary)
			{
				useAlt = true;
			}
		}

		session.ActivePlayAnim = useAlt ? alt : primary;
		if (useAlt)
		{
			session.ActiveAttackPlayB = profile.AltAttackPlayB ?? "";
			session.ActiveAttackPlayF = profile.AltAttackPlayF ?? "";
		}
		else
		{
			session.ActiveAttackPlayB = "";
			session.ActiveAttackPlayF = "";
		}
	}

	private static string GetActivePlayAnim(NCreature attackerNode, ChaosTeleportAttackProfile profile, MeleeSession session)
	{
		InitializePlayVariant(attackerNode, profile, session);
		return string.IsNullOrWhiteSpace(session.ActivePlayAnim) ? (profile.PlayAnim ?? "") : session.ActivePlayAnim;
	}

	private static void PlayOneShotLayers(NCombatRoom room, Vector2 pos, string backScene, string backAnim, string frontScene, string frontAnim, float uniformScale)
	{
		Node? back = room.BackCombatVfxContainer;
		Node? front = room.CombatVfxContainer;
		if (back != null)
		{
			ChaosOneShotVfx.PlaySpineOneShot(backScene, backAnim, back, pos, zIndex: null, uniformScale: uniformScale);
		}
		if (front != null)
		{
			ChaosOneShotVfx.PlaySpineOneShot(frontScene, frontAnim, front, pos, zIndex: -1, uniformScale: uniformScale);
		}
	}

	private static void PlayAttackPlayVfx(NCombatRoom room, Vector2 pos, ChaosTeleportAttackProfile profile, MeleeSession session, float uniformScale)
	{
		ChaosTeleportAttackVfxSet vfx = GetActiveVfx(profile, session);
		PlayOneShotAutoLayer(room, pos, vfx.AttackPlayB, vfx.AttackPlayAnim, uniformScale);
		PlayOneShotAutoLayer(room, pos, vfx.AttackPlayF, vfx.AttackPlayAnim, uniformScale);
	}

	private static ChaosTeleportAttackVfxSet GetActiveVfx(ChaosTeleportAttackProfile profile, MeleeSession session)
	{
		ChaosTeleportAttackVfxSet vfx = profile.Vfx;
		if (!string.IsNullOrWhiteSpace(session.ActiveAttackPlayB))
		{
			vfx = vfx with { AttackPlayB = session.ActiveAttackPlayB };
		}
		if (!string.IsNullOrWhiteSpace(session.ActiveAttackPlayF))
		{
			vfx = vfx with { AttackPlayF = session.ActiveAttackPlayF };
		}
		return vfx;
	}

	private static void PlayReturnVfxAtPlayEnd(NCombatRoom room, Vector2 pos, ChaosTeleportAttackProfile profile, float uniformScale)
	{
		ChaosTeleportAttackVfxSet vfx = profile.Vfx;
		if (profile.ReturnVfxTiming == ChaosTeleportReturnVfxTiming.PlayerArriveAtPlayEnd)
		{
			PlayOneShotLayers(room, pos, vfx.StepPlayerArriveB, vfx.StepAnimBack, vfx.StepPlayerArriveF, vfx.StepAnimFront, uniformScale);
			return;
		}

		Node? back = room.BackCombatVfxContainer;
		if (back != null)
		{
			ChaosOneShotVfx.PlaySpineOneShot(vfx.StepTargetMove, vfx.StepAnimBack, back, pos, zIndex: null, uniformScale: uniformScale);
		}
	}

	private static void PlayReturnVfxAtOrigin(NCombatRoom room, Vector2 pos, ChaosTeleportAttackProfile profile, float uniformScale)
	{
		ChaosTeleportAttackVfxSet vfx = profile.Vfx;
		if (profile.ReturnVfxTiming == ChaosTeleportReturnVfxTiming.PlayerArriveAtPlayEnd)
		{
			Node? back = room.BackCombatVfxContainer;
			if (back != null)
			{
				ChaosOneShotVfx.PlaySpineOneShot(vfx.StepTargetMove, vfx.StepAnimBack, back, pos, zIndex: null, uniformScale: uniformScale);
			}
			return;
		}

		PlayOneShotLayers(room, pos, vfx.StepPlayerArriveB, vfx.StepAnimBack, vfx.StepPlayerArriveF, vfx.StepAnimFront, uniformScale);
	}

	private static void PlayOneShotAutoLayer(NCombatRoom room, Vector2 pos, string scenePath, string anim, float uniformScale)
	{
		if (scenePath.EndsWith("_b.tscn", StringComparison.Ordinal))
		{
			Node? back = room.BackCombatVfxContainer;
			if (back != null)
			{
				ChaosOneShotVfx.PlaySpineOneShot(scenePath, anim, back, pos, zIndex: null, uniformScale: uniformScale);
			}
			return;
		}

		if (scenePath.EndsWith("_f.tscn", StringComparison.Ordinal))
		{
			Node? front = room.CombatVfxContainer;
			if (front != null)
			{
				ChaosOneShotVfx.PlaySpineOneShot(scenePath, anim, front, pos, zIndex: -1, uniformScale: uniformScale);
			}
		}
	}

	private static async void StartVanillaTeleportWatcher(Creature attacker, int requestId, float waitTime)
	{
		try
		{
			ChaosTeleportAttackProfile profile;
			try
			{
				if (!Sessions.TryGetValue(attacker, out MeleeSession? s) || s?.Profile == null)
				{
					profile = ChaosTeleportAttackProfiles.Default;
				}
				else
				{
					profile = s.Profile.Value;
				}
			}
			catch
			{
				profile = ChaosTeleportAttackProfiles.Default;
			}

			float t = Mathf.Clamp(waitTime * profile.VanillaTeleportAtRatio, 0.05f, Mathf.Max(0.05f, waitTime - 0.05f));
			await Cmd.CustomScaledWait(t * 0.5f, t);

			if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
			{
				return;
			}

			if (session.LastRequestId != requestId || session.Teleported)
			{
				return;
			}

			if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out NCreature attackerNode))
			{
				return;
			}

			Vector2 originFoot = GetFootPos(attackerNode);
			float uniformScale = GetChaosEffUniformScale(attackerNode, profile.UniformScaleMultiplier);
			ChaosTeleportAttackVfxSet vfx = profile.Vfx;
			PlayOneShotLayers(room, originFoot, vfx.StepPlayerMoveB, vfx.StepAnimBack, vfx.StepPlayerMoveF, vfx.StepAnimFront, uniformScale);

			Vector2 pos = session.UseFootAnchor
				? ComputeDesiredGlobalPosByFoot(attackerNode, session.LatestTargetCenter, distance: profile.TeleportDistance, attacker.Side)
				: ComputeDesiredGlobalPos(attackerNode, session.LatestTargetCenter, distance: profile.TeleportDistance, attacker.Side);
			session.HasPlannedTeleport = true;
			session.PlannedTeleportGlobalPos = pos;
			attackerNode.GlobalPosition = pos;
			attackerNode.GlobalPosition = pos;
			Vector2 targetFoot = GetFootPos(attackerNode);
			PlayOneShotLayers(room, targetFoot, vfx.StepTargetArriveB, vfx.StepAnimBack, vfx.StepTargetArriveF, vfx.StepAnimFront, uniformScale);
				PlayAttackPlayVfx(room, targetFoot, profile, session, uniformScale);

			session.Teleported = true;
			StartPositionLock(attacker, requestId, pos, waitTime);
			StartVanillaEndWatcher(attacker, requestId, waitTime);
		}
		catch
		{
		}
	}

	private static async void StartVanillaEndWatcher(Creature attacker, int requestId, float waitTime)
	{
		try
		{
			float t = Mathf.Max(waitTime, 0.05f);
			await Cmd.CustomScaledWait(t * 0.5f, t);

			if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
			{
				return;
			}

			if (session.LastRequestId != requestId || !session.Teleported)
			{
				return;
			}

			if (!TryGetRoomAndNode(attacker, out NCombatRoom room, out NCreature attackerNode))
			{
				YukiVictoryAnimCoordinator.MarkTeleportEnd(attacker);
				return;
			}

			Vector2 playEndFoot = GetFootPos(attackerNode);
			ChaosTeleportAttackProfile profile = session.Profile ?? ChaosTeleportAttackProfiles.Default;
			float uniformScale = GetChaosEffUniformScale(attackerNode, profile.UniformScaleMultiplier);
			PlayReturnVfxAtPlayEnd(room, playEndFoot, profile, uniformScale);

			Vector2 origin = session.OriginGlobalPos;
			session.Teleported = false;
			session.HasOrigin = false;
			session.ReadyPlaying = false;
			session.TargetSignature = 0;
			session.UseFootAnchor = false;
			session.HasPlannedTeleport = false;
			session.PlannedTeleportGlobalPos = Vector2.Zero;
			session.PendingDeferredTeleportForRequestId = 0;
			session.PendingDeferredTeleportForRequestId = 0;

			attackerNode.GlobalPosition = origin;
			attackerNode.GlobalPosition = origin;
			Vector2 endFoot = GetFootPos(attackerNode);
			PlayReturnVfxAtOrigin(room, endFoot, profile, uniformScale);
			YukiVictoryAnimCoordinator.MarkTeleportEnd(attacker);
		}
		catch
		{
		}
	}

	private static void ConnectAnimationCompletedOnce(MegaSprite sprite, Action action)
	{
		Callable cb = default;
		cb = Callable.From<GodotObject, GodotObject, GodotObject>((_, __, ___) =>
		{
			try
			{
				sprite.DisconnectAnimationCompleted(cb);
			}
			catch
			{
			}

			action();
		});
		try
		{
			sprite.ConnectAnimationCompleted(cb);
		}
		catch
		{
		}
	}

	private static async void StartPositionLock(Creature attacker, int requestId, Vector2 plannedGlobalPos, float seconds)
	{
		try
		{
			float remaining = Mathf.Max(seconds, 0.01f);
			while (remaining > 0f)
			{
				if (CombatManager.Instance.IsEnding)
				{
					return;
				}
				if (!Sessions.TryGetValue(attacker, out MeleeSession? session) || session == null)
				{
					return;
				}
				if (session.LastRequestId != requestId || !session.Teleported)
				{
					return;
				}
				if (!TryGetRoomAndNode(attacker, out _, out NCreature attackerNode))
				{
					return;
				}

				attackerNode.GlobalPosition = plannedGlobalPos;
				float dt = await attackerNode.AwaitProcessFrame(CancellationToken.None);
				remaining -= Mathf.Max(dt, 0.0f);
			}
		}
		catch
		{
		}
	}

	private static Marker2D? GetChaosEffMarker(NCreature creatureNode)
	{
		try
		{
			Node2D? visualsRoot = creatureNode.Visuals;
			if (visualsRoot == null || !GodotObject.IsInstanceValid(visualsRoot))
			{
				return null;
			}

			Marker2D? marker = visualsRoot.GetNodeOrNull<Marker2D>("Visuals/ChaosEff");
			if (marker != null && GodotObject.IsInstanceValid(marker))
			{
				return marker;
			}

			marker = visualsRoot.GetNodeOrNull<Marker2D>("%ChaosEff");
			if (marker != null && GodotObject.IsInstanceValid(marker))
			{
				return marker;
			}
		}
		catch
		{
		}

		try
		{
			Marker2D? marker = creatureNode.GetNodeOrNull<Marker2D>("%ChaosEff");
			if (marker != null && GodotObject.IsInstanceValid(marker))
			{
				return marker;
			}
		}
		catch
		{
		}

		return null;
	}

	private static float GetChaosEffUniformScale(NCreature creatureNode, float multiplier)
	{
		float s = 1f;
		try
		{
			Marker2D? chaosEff = GetChaosEffMarker(creatureNode);
			if (chaosEff != null && GodotObject.IsInstanceValid(chaosEff))
			{
				s = Mathf.Abs(chaosEff.GlobalScale.X);
			}
			else if (creatureNode.Visuals != null && GodotObject.IsInstanceValid(creatureNode.Visuals))
			{
				s = Mathf.Abs(creatureNode.Visuals.GlobalScale.X);
			}
		}
		catch
		{
			s = 1f;
		}

		s = Mathf.Max(s, 0.01f);
		return multiplier * s;
	}

	private static List<Creature> GetAliveTargets(AttackCommand command)
	{
		try
		{
			var method = AccessTools.Method(command.GetType(), "GetPossibleTargets");
			if (method == null)
			{
				return new List<Creature>();
			}
			if (method.Invoke(command, null) is not IReadOnlyList<Creature> list)
			{
				return new List<Creature>();
			}
			return list.Where(c => c != null && c.IsAlive).ToList();
		}
		catch
		{
			return new List<Creature>();
		}
	}

	private static Vector2 GetTargetsCenter(NCombatRoom room, IReadOnlyList<Creature> targets)
	{
		Vector2 sum = Vector2.Zero;
		int count = 0;
		for (int i = 0; i < targets.Count; i++)
		{
			Creature t = targets[i];
			NCreature? node = room.GetCreatureNode(t);
			if (node == null || !GodotObject.IsInstanceValid(node))
			{
				continue;
			}
			sum += node.VfxSpawnPosition;
			count++;
		}
		if (count <= 0)
		{
			return Vector2.Zero;
		}
		return sum / count;
	}

	private static Vector2 GetLeftmostTargetFoot(NCombatRoom room, IReadOnlyList<Creature> targets)
	{
		Vector2 best = Vector2.Zero;
		bool has = false;
		for (int i = 0; i < targets.Count; i++)
		{
			Vector2 foot = GetSingleTargetFoot(room, targets[i]);
			if (foot == Vector2.Zero)
			{
				continue;
			}
			if (!has || foot.X < best.X)
			{
				best = foot;
				has = true;
			}
		}
		return best;
	}

	private static Vector2 GetSingleTargetFoot(NCombatRoom room, Creature target)
	{
		try
		{
			NCreature? node = room.GetCreatureNode(target);
			if (node == null || !GodotObject.IsInstanceValid(node))
			{
				return Vector2.Zero;
			}
			FootAnchorCache cache = TargetFootCache.GetOrCreateValue(target);
			float scale = GetVisualScaleX(node);
			if (!cache.Initialized)
			{
				Vector2 dynamicFoot = GetCreatureFootAnchorDynamic(node);
				cache.OffsetAtScale1 = (dynamicFoot - node.GlobalPosition) / scale;
				cache.Initialized = true;
			}
			return node.GlobalPosition + cache.OffsetAtScale1 * scale;
		}
		catch
		{
			return Vector2.Zero;
		}
	}

	private static int GetTargetSignature(IReadOnlyList<Creature> targets)
	{
		unchecked
		{
			int hash = 17;
			for (int i = 0; i < targets.Count; i++)
			{
				hash = hash * 31 + RuntimeHelpers.GetHashCode(targets[i]);
			}
			hash = hash * 31 + targets.Count;
			return hash;
		}
	}

	private static Vector2 ComputeDesiredGlobalPos(NCreature attackerNode, Vector2 targetCenter, float distance, CombatSide side)
	{
		Vector2 attackerCenter = attackerNode.VfxSpawnPosition;
		Vector2 dir = targetCenter - attackerCenter;
		if (dir.LengthSquared() < 0.001f)
		{
			dir = side == CombatSide.Player ? Vector2.Right : Vector2.Left;
		}
		else
		{
			dir = dir.Normalized();
		}

		Vector2 desiredCenter = targetCenter - dir * distance;
		Vector2 anchorOffset = attackerCenter - attackerNode.GlobalPosition;
		return Snap(desiredCenter - anchorOffset);
	}

	private static Vector2 ComputeDesiredGlobalPosByFoot(NCreature attackerNode, Vector2 targetFoot, float distance, CombatSide side)
	{
		Vector2 attackerAnchor = GetFootPos(attackerNode);
		Vector2 dir = targetFoot - attackerAnchor;
		if (dir.LengthSquared() < 0.001f)
		{
			dir = side == CombatSide.Player ? Vector2.Right : Vector2.Left;
		}
		else
		{
			dir = dir.Normalized();
		}

		Vector2 desiredAnchor = targetFoot - dir * distance;
		Vector2 anchorOffset = attackerAnchor - attackerNode.GlobalPosition;
		return Snap(desiredAnchor - anchorOffset);
	}

	private static float GetVisualScaleX(NCreature node)
	{
		try
		{
			Node2D visuals = node.Visuals;
			if (visuals != null && GodotObject.IsInstanceValid(visuals))
			{
				return Mathf.Max(Mathf.Abs(visuals.Scale.X), 0.01f);
			}
		}
		catch
		{
		}

		return 1f;
	}

	private static Vector2 GetCreatureFootAnchorDynamic(NCreature node)
	{
		try
		{
			Control hitbox = node.Hitbox;
			if (hitbox != null && GodotObject.IsInstanceValid(hitbox))
			{
				Rect2 rect = hitbox.GetGlobalRect();
				return new Vector2(rect.Position.X + rect.Size.X * 0.5f, rect.Position.Y + rect.Size.Y);
			}
		}
		catch
		{
		}

		try
		{
			return node.GlobalPosition;
		}
		catch
		{
			return Vector2.Zero;
		}
	}

	private static Vector2 Snap(Vector2 pos)
	{
		return new Vector2(Mathf.Round(pos.X), Mathf.Round(pos.Y));
	}

	private static Vector2 GetFootPos(NCreature creatureNode)
	{
		try
		{
			Marker2D? chaosEff = GetChaosEffMarker(creatureNode);
			if (chaosEff != null && GodotObject.IsInstanceValid(chaosEff))
			{
				return chaosEff.GlobalPosition;
			}

			Node2D? visuals = creatureNode.Visuals;
			if (visuals != null && GodotObject.IsInstanceValid(visuals))
			{
				return visuals.GlobalPosition;
			}
		}
		catch
		{
		}
		return creatureNode.GlobalPosition;
	}

	private static void WarnOnce(string key, string msg)
	{
		lock (Warned)
		{
			if (!Warned.Add(key))
			{
				return;
			}
		}
		Log.Warn(msg);
	}
}

