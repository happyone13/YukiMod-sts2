using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Logging;
using YukiCharacterModel = YukiMod.YukiModCode.Character.YukiMod;

namespace YukiMod.YukiModCode.Mechanics.Animation;

[HarmonyPatch]
public static class YukiRandomAttackCastAnimPatch
{
	private sealed class VariantConfig
	{
		public string[] AttackAnims = Array.Empty<string>();
		public string[] CastAnims = Array.Empty<string>();
		public bool LoggedEnabled;
		public bool Initialized;
	}

	private static readonly ConditionalWeakTable<CreatureAnimator, VariantConfig> _configs = new ConditionalWeakTable<CreatureAnimator, VariantConfig>();
	private static readonly FieldInfo? _spineControllerField = AccessTools.Field(typeof(CreatureAnimator), "_spineController");
	private static readonly MethodInfo? _setNextStateMethod = AccessTools.Method(typeof(CreatureAnimator), "SetNextState");

	private static MethodBase? TargetMethod()
	{
		return AccessTools.Method(typeof(YukiCharacterModel), "GenerateAnimator");
	}

	[HarmonyPostfix]
	public static void Postfix(MegaSprite controller, ref CreatureAnimator __result)
	{
		if (controller == null || __result == null)
		{
			return;
		}

		if (!IsYukiController(controller))
		{
			return;
		}

		VariantConfig cfg = _configs.GetOrCreateValue(__result);
		if (cfg.Initialized)
		{
			return;
		}
		cfg.Initialized = true;

		cfg.AttackAnims = BuildAnimList(controller, "attack", "attack_2", "attack_3");
		cfg.CastAnims = BuildAnimList(controller, "cast", "cast_2", "cast_3");
		LogOnceIfEnabled(cfg);
	}

	private static bool IsYukiController(MegaSprite controller)
	{
		return Has(controller, "attack_2")
		       || Has(controller, "attack_3")
		       || Has(controller, "cast_2")
		       || Has(controller, "cast_3")
		       || Has(controller, "attack_ready")
		       || Has(controller, "attack_play")
		       || Has(controller, "attack_end")
		       || Has(controller, "u3_attack_ready")
		       || Has(controller, "u3_attack_play")
		       || Has(controller, "u3_attack_end");
	}

	private static bool Has(MegaSprite controller, string anim)
	{
		try
		{
			return controller.HasAnimation(anim);
		}
		catch
		{
			return false;
		}
	}

	private static string[] BuildAnimList(MegaSprite controller, string a0, string a1, string a2)
	{
		if (!Has(controller, a0))
		{
			return Array.Empty<string>();
		}
		if (Has(controller, a2))
		{
			return Has(controller, a1) ? new[] { a0, a1, a2 } : new[] { a0, a2 };
		}
		if (Has(controller, a1))
		{
			return new[] { a0, a1 };
		}
		return new[] { a0 };
	}

	private static void LogOnceIfEnabled(VariantConfig cfg)
	{
		if (cfg.LoggedEnabled)
		{
			return;
		}
		if (cfg.AttackAnims.Length <= 1 && cfg.CastAnims.Length <= 1)
		{
			return;
		}
		cfg.LoggedEnabled = true;
		Log.Info("[YukiMod] Random anim enabled: attackVariants=" + cfg.AttackAnims.Length + " castVariants=" + cfg.CastAnims.Length);
	}

	[HarmonyPatch(typeof(CreatureAnimator), nameof(CreatureAnimator.SetTrigger))]
	private static class CreatureAnimator_SetTrigger_Roll
	{
		[HarmonyPrefix]
		private static bool Prefix(CreatureAnimator __instance, string trigger)
		{
			if (trigger == null || __instance == null)
			{
				return true;
			}

			if (!_configs.TryGetValue(__instance, out VariantConfig? cfg) || cfg == null)
			{
				return true;
			}

			if (!string.Equals(trigger, "Attack", StringComparison.Ordinal) && !string.Equals(trigger, "Cast", StringComparison.Ordinal))
			{
				return true;
			}

			MegaSprite? sprite = null;
			if (_spineControllerField != null)
			{
				try
				{
					object? spriteObj = _spineControllerField.GetValue(__instance);
					sprite = spriteObj as MegaSprite;
				}
				catch
				{
				}
			}

			if (sprite != null)
			{
				if (string.Equals(trigger, "Attack", StringComparison.Ordinal))
				{
					if (cfg.AttackAnims.Length <= 1)
					{
						string[] rebuilt = BuildAnimList(sprite, "attack", "attack_2", "attack_3");
						if (rebuilt.Length > 0)
						{
							cfg.AttackAnims = rebuilt;
							LogOnceIfEnabled(cfg);
						}
					}
				}
				else
				{
					if (cfg.CastAnims.Length <= 1)
					{
						string[] rebuilt = BuildAnimList(sprite, "cast", "cast_2", "cast_3");
						if (rebuilt.Length > 0)
						{
							cfg.CastAnims = rebuilt;
							LogOnceIfEnabled(cfg);
						}
					}
				}
			}

			if (string.Equals(trigger, "Attack", StringComparison.Ordinal))
			{
				if (cfg.AttackAnims.Length <= 1)
				{
					return true;
				}
				int idx = Random.Shared.Next(cfg.AttackAnims.Length);
				string anim = cfg.AttackAnims[idx];
				return TryForceState(__instance, sprite, anim);
			}

			if (cfg.CastAnims.Length <= 1)
			{
				return true;
			}
			int idxCast = Random.Shared.Next(cfg.CastAnims.Length);
			string animCast = cfg.CastAnims[idxCast];
			return TryForceState(__instance, sprite, animCast);
		}

		private static bool TryForceState(CreatureAnimator animator, MegaSprite? sprite, string animId)
		{
			if (sprite == null)
			{
				return true;
			}
			try
			{
				if (!sprite.HasAnimation(animId))
				{
					return true;
				}
			}
			catch
			{
				return true;
			}

			try
			{
				if (_setNextStateMethod != null)
				{
					AnimState s = new AnimState(animId);
					s.NextState = new AnimState("idle_loop", isLooping: true);
					_setNextStateMethod.Invoke(animator, new object[] { s });
					return false;
				}
			}
			catch
			{
			}

			try
			{
				MegaAnimationState st = sprite.GetAnimationState();
				st.SetAnimation(animId, loop: false);
				if (sprite.HasAnimation("idle_loop"))
				{
					st.AddAnimation("idle_loop", 0f, loop: true);
				}
				return false;
			}
			catch
			{
				return true;
			}
		}
	}
}

