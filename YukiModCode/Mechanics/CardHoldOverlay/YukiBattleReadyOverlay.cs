using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using YukiMod.YukiModCode.Mechanics.Settings;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YukiMod.YukiModCode.Mechanics.CardHoldOverlay;

public static class YukiBattleReadyOverlay
{
	private const float OutDelaySeconds = 0.3f;
	private const float CancelOutDelaySeconds = 0.8f;
	private static string ScenePath => YukiBattleReadyProfile.BattleReadyScenePath;
	private const string AnimIn = "b_in";
	private const string AnimIdle = "b_idle";
	private const string AnimOut = "b_out";
	private const string AnimCardAttack = "card_attack";
	private static readonly string[] AnimCardNonAttackCandidates = new[] { "card_casting" };

	private static PackedScene? _cachedScene;
	private static bool _sceneLoadAttempted;
	private static bool _sceneMissingWarned;

	private static Node? _node;
	private static MegaSprite? _sprite;
	private static bool _busy;

	private static bool _isHovered;
	private static bool _isUiFocused;
	private static ulong _focusToken;
	private static bool _outScheduled;

	private static bool _outPlaying;
	private static bool _cardUsePlaying;
	private static readonly Queue<string> _cardAnimQueue = new Queue<string>();

	private static bool _baseCaptured;
	private static Vector2 _basePos;
	private static Vector2 _baseScale = Vector2.One;

	private static bool _hasAnimIn;
	private static bool _hasAnimIdle;
	private static bool _hasAnimOut;
	private static bool _hasCardAttack;
	private static string? _cardNonAttackAnim;

	private static string? _lastFirst;
	private static string? _lastNextLoop;

	private static readonly HashSet<string> _missingAnimsWarned = new HashSet<string>(StringComparer.Ordinal);
	private static ulong _watchToken;
	private static long _createDisabledUntil;
	private static string? _createDisabledReason;
	private static int _createErrorLogged;
	private const int CreateDisableMs = 30000;

	private static bool IsFocused => _isHovered || _isUiFocused;
	private static bool IsFocusedEffective => IsFocused || _outScheduled;

	public static void Preload()
	{
		if (!YukiModSharedSettings.BattleReadyOverlayEnabled)
		{
			return;
		}
		_ = GetScene();
	}

	public static void NotifyCombatEnded()
	{
		_isHovered = false;
		_isUiFocused = false;
		_outScheduled = false;
		Cleanup();
	}

	private static PackedScene? GetScene()
	{
		if (_cachedScene != null)
		{
			return _cachedScene;
		}

		if (_sceneLoadAttempted)
		{
			return null;
		}

		_sceneLoadAttempted = true;
		try
		{
			_cachedScene = ResourceLoader.Load<PackedScene>(ScenePath);
			return _cachedScene;
		}
		catch
		{
			return null;
		}
	}

	private static void EnsureCreated(bool playIntro)
	{
		if (!YukiModSharedSettings.BattleReadyOverlayEnabled)
		{
			Cleanup();
			return;
		}

		long now = System.Environment.TickCount64;
		if (now < _createDisabledUntil)
		{
			return;
		}

		if (_node != null && GodotObject.IsInstanceValid(_node) && _sprite != null)
		{
			return;
		}

		try
		{
			Cleanup();

			NCombatRoom? room = NCombatRoom.Instance;
			if (room == null)
			{
				return;
			}

			PackedScene? scene = GetScene();
			if (scene == null)
			{
				if (!_sceneMissingWarned)
				{
					_sceneMissingWarned = true;
					Log.Warn("[YukiMod] YukiBattleReadyOverlay: missing scene " + ScenePath);
				}
				return;
			}

			Node instance = scene.Instantiate();
			_node = instance;
			_sprite = new MegaSprite(instance);
			InitAnimCache(_sprite);
			_busy = true;
			_outPlaying = false;
			_cardUsePlaying = false;
			_cardAnimQueue.Clear();

			ulong watchToken = ++_watchToken;
			TaskHelper.RunSafely(IdleWatchLoop(instance, watchToken));

			_sprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>((_, __, ___) =>
			{
				if (_node != instance)
				{
					return;
				}

				if (_cardUsePlaying)
				{
					if (TryPlayNextQueuedCardAnim(currentCompleted: true))
					{
						return;
					}

					_cardUsePlaying = false;
					if (IsFocused)
					{
						PlaySequence(AnimIdle, AnimIdle);
					}
					else
					{
						StartOut();
					}
					return;
				}

				if (_outPlaying)
				{
					_outPlaying = false;
					if (IsFocused)
					{
						PlaySequence(AnimIn, AnimIdle);
					}
					else
					{
						Cleanup();
					}
				}

				if (string.Equals(_lastFirst, AnimIn, StringComparison.Ordinal) && string.Equals(_lastNextLoop, AnimIdle, StringComparison.Ordinal))
				{
					_lastFirst = AnimIdle;
					_lastNextLoop = null;
				}
			}));

			room.CombatVfxContainer.AddChildSafely(instance);
			if (instance is CanvasItem canvasItem)
			{
				canvasItem.ZIndex = 0;
			}
			CaptureBaseTransform(instance);
			ApplyTransformFromSettings();

			if (playIntro)
			{
				PlaySequence(AnimIn, AnimIdle);
			}
			else
			{
				PlaySequence(AnimIdle, AnimIdle);
			}
		}
		catch (Exception ex)
		{
			_createDisabledReason = ex.Message;
			_createDisabledUntil = System.Environment.TickCount64 + CreateDisableMs;
			if (System.Threading.Interlocked.Exchange(ref _createErrorLogged, 1) == 0)
			{
				Log.Warn("[YukiMod] YukiBattleReadyOverlay create failed: " + ex);
			}
			Cleanup();
		}
	}

	public static void ApplyTransformFromSettings()
	{
		Node? node = _node;
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		try
		{
			ApplyTransform(node);
		}
		catch
		{
		}
	}

	private static void CaptureBaseTransform(Node instance)
	{
		if (_baseCaptured)
		{
			return;
		}
		if (instance is Node2D node2d)
		{
			_baseCaptured = true;
			_basePos = node2d.Position;
			_baseScale = node2d.Scale;
			return;
		}
		if (instance is Control control)
		{
			_baseCaptured = true;
			_basePos = control.Position;
			_baseScale = control.Scale;
		}
	}

	private static void ApplyTransform(Node instance)
	{
		float scale = YukiModSharedSettings.BattleReadyScale;
		float offsetX = YukiModSharedSettings.BattleReadyOffsetX;
		float offsetY = YukiModSharedSettings.BattleReadyOffsetY;
		if (instance is Node2D node2d)
		{
			node2d.Scale = _baseScale * new Vector2(scale, scale);
			node2d.Position = _basePos + new Vector2(offsetX, -offsetY);
			return;
		}
		if (instance is Control control)
		{
			control.Scale = _baseScale * new Vector2(scale, scale);
			control.Position = _basePos + new Vector2(offsetX, -offsetY);
		}
	}

	private static void Cleanup()
	{
		_busy = false;
		_outPlaying = false;
		_cardUsePlaying = false;
		_cardAnimQueue.Clear();
		_lastFirst = null;
		_lastNextLoop = null;
		_baseCaptured = false;
		_basePos = Vector2.Zero;
		_baseScale = Vector2.One;

		Node? node = _node;
		_node = null;
		_sprite = null;

		if (node != null && GodotObject.IsInstanceValid(node))
		{
			try
			{
				node.QueueFree();
			}
			catch
			{
			}
		}
	}

	public static void NotifyHovered(CardModel card, bool hovered)
	{
		if (!YukiModSharedSettings.BattleReadyOverlayEnabled)
		{
			Cleanup();
			return;
		}

		if (!YukiTarget.IsMineTargetCard(card))
		{
			return;
		}

		bool wasFocused = IsFocusedEffective;
		_isHovered = hovered;
		_focusToken++;

		if (hovered)
		{
			_outScheduled = false;
			if (!_busy)
			{
				EnsureCreated(playIntro: true);
				return;
			}
			if (wasFocused)
			{
				return;
			}
			if (_outPlaying || _cardUsePlaying)
			{
				return;
			}
			PlaySequence(AnimIn, AnimIdle);
			return;
		}

		if (IsFocused)
		{
			return;
		}

		_outScheduled = true;
		ulong token = _focusToken;
		TaskHelper.RunSafely(DelayedOutIfStillUnfocused(token, OutDelaySeconds));
	}

	public static void NotifyUiFocused(CardModel card, bool focused)
	{
		if (!YukiModSharedSettings.BattleReadyOverlayEnabled)
		{
			Cleanup();
			return;
		}

		if (!YukiTarget.IsMineTargetCard(card))
		{
			return;
		}

		bool wasFocused = IsFocusedEffective;
		_isUiFocused = focused;
		_focusToken++;

		if (focused)
		{
			_outScheduled = false;
			if (!_busy)
			{
				EnsureCreated(playIntro: true);
				return;
			}
			if (wasFocused)
			{
				return;
			}
			if (_outPlaying || _cardUsePlaying)
			{
				return;
			}
			PlaySequence(AnimIn, AnimIdle);
			return;
		}

		if (IsFocused)
		{
			return;
		}

		_outScheduled = true;
		ulong token = _focusToken;
		TaskHelper.RunSafely(DelayedOutIfStillUnfocused(token, OutDelaySeconds));
	}

	private static async Task DelayedOutIfStillUnfocused(ulong token, float delaySeconds)
	{
		await WaitSeconds(delaySeconds);
		if (token != _focusToken)
		{
			return;
		}

		if (IsFocused || !_busy)
		{
			return;
		}

		_outScheduled = false;
		if (_cardUsePlaying)
		{
			return;
		}

		StartOut();
	}

	private static async Task IdleWatchLoop(Node instance, ulong watchToken)
	{
		while (watchToken == _watchToken)
		{
			await WaitSeconds(1f);
			if (watchToken != _watchToken)
			{
				return;
			}

			if (!_busy || _node != instance || !GodotObject.IsInstanceValid(instance) || _sprite == null)
			{
				return;
			}

			if (_cardUsePlaying || _cardAnimQueue.Count > 0 || _outPlaying || _outScheduled)
			{
				continue;
			}

			if (IsFocused)
			{
				continue;
			}

			if (!string.Equals(_lastFirst, AnimIdle, StringComparison.Ordinal) || _lastNextLoop != null)
			{
				continue;
			}

			StartOut();
		}
	}

	private static async Task WaitSeconds(float seconds)
	{
		if (seconds <= 0f)
		{
			return;
		}

		try
		{
			NCombatRoom? room = NCombatRoom.Instance;
			SceneTree? tree = room?.GetTree();
			if (room != null && tree != null)
			{
				SceneTreeTimer timer = tree.CreateTimer(seconds);
				await room.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
				return;
			}
		}
		catch
		{
		}

		await Cmd.CustomScaledWait(seconds, seconds);
	}

	public static void NotifyBeforeCardPlayed(CardPlay cardPlay)
	{
		if (!YukiModSharedSettings.BattleReadyOverlayEnabled)
		{
			Cleanup();
			return;
		}

		CardModel? card = cardPlay.Card;
		if (!YukiTarget.IsMineTargetCard(card))
		{
			return;
		}

		_focusToken++;
		_isHovered = false;
		_outScheduled = false;
		EnsureCreated(playIntro: false);

		string? anim = GetCardUseAnim(card!);
		if (anim == null)
		{
			return;
		}

		if (_outPlaying)
		{
			_outPlaying = false;
		}

		_cardAnimQueue.Enqueue(anim);
		TryPlayNextQueuedCardAnim();
	}

	public static void NotifyCanceled(CardModel card)
	{
		if (!YukiModSharedSettings.BattleReadyOverlayEnabled)
		{
			Cleanup();
			return;
		}

		if (!YukiTarget.IsMineTargetCard(card))
		{
			return;
		}

		if (!_busy)
		{
			return;
		}

		_isHovered = false;
		_isUiFocused = false;
		_outScheduled = true;
		ulong token = ++_focusToken;

		if (_cardUsePlaying || _cardAnimQueue.Count > 0 || _outPlaying)
		{
			return;
		}

		TaskHelper.RunSafely(DelayedOutIfStillUnfocused(token, CancelOutDelaySeconds));
	}

	private static void StartOut()
	{
		if (_cardUsePlaying || _cardAnimQueue.Count > 0)
		{
			return;
		}
		if (_outPlaying)
		{
			return;
		}
		if (!_hasAnimOut)
		{
			Cleanup();
			return;
		}

		_outScheduled = false;
		_outPlaying = true;
		if (!PlaySingle(AnimOut))
		{
			Cleanup();
		}
	}

	private static bool TryPlayNextQueuedCardAnim(bool currentCompleted = false)
	{
		if (_cardUsePlaying && !currentCompleted)
		{
			return true;
		}

		if (_cardAnimQueue.Count == 0)
		{
			return false;
		}

		while (_cardAnimQueue.Count > 0)
		{
			string anim = _cardAnimQueue.Dequeue();
			_cardUsePlaying = true;
			_outScheduled = false;
			_outPlaying = false;

			if (PlaySingle(anim, restartIfSame: true))
			{
				return true;
			}

			_cardUsePlaying = false;
		}

		if (IsFocused)
		{
			PlaySequence(AnimIdle, AnimIdle);
		}
		else
		{
			StartOut();
		}

		return false;
	}

	private static void PlaySequence(string first, string nextLoop)
	{
		MegaSprite? sprite = _sprite;
		if (sprite == null)
		{
			return;
		}

		MegaAnimationState state = sprite.GetAnimationState();
		if (!HasAnim(sprite, first))
		{
			LogMissingAnimOnce(first);
			return;
		}

		if (string.Equals(first, nextLoop, StringComparison.Ordinal))
		{
			if (string.Equals(_lastFirst, first, StringComparison.Ordinal) && _lastNextLoop == null)
			{
				return;
			}

			state.SetAnimation(first, loop: true);
			_lastFirst = first;
			_lastNextLoop = null;
			return;
		}

		if (string.Equals(_lastFirst, first, StringComparison.Ordinal) && string.Equals(_lastNextLoop, nextLoop, StringComparison.Ordinal))
		{
			return;
		}

		state.SetAnimation(first, loop: false);
		if (HasAnim(sprite, nextLoop))
		{
			state.AddAnimation(nextLoop, 0f, loop: true);
			_lastFirst = first;
			_lastNextLoop = nextLoop;
		}
		else
		{
			_lastFirst = first;
			_lastNextLoop = null;
		}
	}

	private static bool PlaySingle(string anim, bool restartIfSame = false)
	{
		MegaSprite? sprite = _sprite;
		if (sprite == null)
		{
			return false;
		}

		if (!HasAnim(sprite, anim))
		{
			LogMissingAnimOnce(anim);
			return false;
		}

		if (!restartIfSame && string.Equals(_lastFirst, anim, StringComparison.Ordinal) && _lastNextLoop == null)
		{
			return true;
		}

		sprite.GetAnimationState().SetAnimation(anim, loop: false);
		_lastFirst = anim;
		_lastNextLoop = null;
		return true;
	}

	private static string? GetCardUseAnim(CardModel card)
	{
		if (card.Type == CardType.Attack)
		{
			return _hasCardAttack ? AnimCardAttack : null;
		}

		return _cardNonAttackAnim;
	}

	private static void InitAnimCache(MegaSprite sprite)
	{
		_hasAnimIn = sprite.HasAnimation(AnimIn);
		_hasAnimIdle = sprite.HasAnimation(AnimIdle);
		_hasAnimOut = sprite.HasAnimation(AnimOut);
		_hasCardAttack = sprite.HasAnimation(AnimCardAttack);

		_cardNonAttackAnim = null;
		for (int i = 0; i < AnimCardNonAttackCandidates.Length; i++)
		{
			string candidate = AnimCardNonAttackCandidates[i];
			if (sprite.HasAnimation(candidate))
			{
				_cardNonAttackAnim = candidate;
				break;
			}
		}

		_lastFirst = null;
		_lastNextLoop = null;
	}

	private static bool HasAnim(MegaSprite sprite, string anim)
	{
		if (string.Equals(anim, AnimIn, StringComparison.Ordinal))
		{
			return _hasAnimIn;
		}
		if (string.Equals(anim, AnimIdle, StringComparison.Ordinal))
		{
			return _hasAnimIdle;
		}
		if (string.Equals(anim, AnimOut, StringComparison.Ordinal))
		{
			return _hasAnimOut;
		}
		if (string.Equals(anim, AnimCardAttack, StringComparison.Ordinal))
		{
			return _hasCardAttack;
		}
		return sprite.HasAnimation(anim);
	}

	private static void LogMissingAnimOnce(string anim)
	{
		if (_missingAnimsWarned.Add(anim))
		{
			Log.Warn("[YukiMod] YukiBattleReadyOverlay missing animation: " + anim);
		}
	}
}
