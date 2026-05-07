@tool
extends Node

@export var AnimationName: String = "animation"
@export var Loop: bool = true
@export var FollowUpAnimationName: String = ""
@export var FollowUpLoop: bool = true
@export var FollowUpDelay: float = 0.0
@export var SkinName: String = ""
@export var ResetSlotsToSetupPose: bool = false
@export var ResetSlotsBeforeFollowUp: bool = false
@export var SlotFixProfile: String = ""

var _played := false
var _setup_pose_applied := false
var _waiting_for_follow_up := false
var _spine_sprite: Object
var _animation_state: Object


func _ready() -> void:
    _try_play()
    _apply_configured_slot_fixes()


func _process(_delta: float) -> void:
    if _played:
        _try_play_follow_up()
    else:
        _try_play()

    _apply_configured_slot_fixes()


func _try_play() -> void:
    if AnimationName.strip_edges() == "":
        return

    var spine_sprite := get_parent()
    if spine_sprite == null:
        return

    _spine_sprite = spine_sprite
    _apply_initial_pose(spine_sprite)

    var animation_state := _get_animation_state(spine_sprite)
    if animation_state == null:
        return

    _animation_state = animation_state
    if _try_set_animation(animation_state, AnimationName, Loop, 0):
        if FollowUpAnimationName.strip_edges() != "":
            if ResetSlotsBeforeFollowUp and not Loop:
                _waiting_for_follow_up = true
            else:
                _try_add_animation(animation_state, FollowUpAnimationName, FollowUpLoop, 0, FollowUpDelay)

        _played = true


func _try_play_follow_up() -> void:
    if not _waiting_for_follow_up or _spine_sprite == null or _animation_state == null:
        return

    if not _is_current_animation_complete(_animation_state, 0):
        return

    _apply_initial_pose(_spine_sprite, true)
    _try_set_animation(_animation_state, FollowUpAnimationName, FollowUpLoop, 0)
    _waiting_for_follow_up = false


func _apply_initial_pose(spine_sprite: Object, force: bool = false) -> void:
    if _setup_pose_applied and not force:
        return

    if SkinName.strip_edges() == "" and not ResetSlotsToSetupPose:
        _setup_pose_applied = true
        return

    var skeleton := _get_skeleton(spine_sprite)
    if skeleton == null:
        return

    if SkinName.strip_edges() != "":
        _try_call(skeleton, "set_skin_by_name", [SkinName]) or _try_call(skeleton, "SetSkinByName", [SkinName])

    if ResetSlotsToSetupPose:
        _try_call(skeleton, "set_slots_to_setup_pose", []) or _try_call(skeleton, "SetSlotsToSetupPose", [])

    _setup_pose_applied = true


func _apply_configured_slot_fixes() -> void:
    if _spine_sprite == null or SlotFixProfile.strip_edges() == "":
        return

    var skeleton := _get_skeleton(_spine_sprite)
    if skeleton == null:
        return

    match SlotFixProfile.strip_edges().to_lower():
        "attack_defense_unity":
            _hide_slot_attachment(skeleton, "face_shadow_1")
            _hide_slot_attachment(skeleton, "face_shadow_2")
        "huang_hun_de_ji_ban":
            _hide_slot_attachment(skeleton, "effect_yellow")


func _try_set_animation(animation_state: Object, animation: String, loop: bool, track: int) -> bool:
    return _try_call(animation_state, "set_animation", [animation, loop, track]) \
        or _try_call(animation_state, "SetAnimation", [animation, loop, track])


func _try_add_animation(animation_state: Object, animation: String, loop: bool, track: int, delay: float) -> bool:
    return _try_call(animation_state, "add_animation", [animation, delay, loop, track]) \
        or _try_call(animation_state, "AddAnimation", [animation, delay, loop, track])


func _get_animation_state(spine_sprite: Object) -> Object:
    var result = _try_call_with_result(spine_sprite, "get_animation_state", [])
    if result != null:
        return result

    return _try_call_with_result(spine_sprite, "GetAnimationState", [])


func _get_skeleton(spine_sprite: Object) -> Object:
    var result = _try_call_with_result(spine_sprite, "get_skeleton", [])
    if result != null:
        return result

    return _try_call_with_result(spine_sprite, "GetSkeleton", [])


func _find_slot(skeleton: Object, slot_name: String) -> Object:
    var result = _try_call_with_result(skeleton, "find_slot", [slot_name])
    if result != null:
        return result

    return _try_call_with_result(skeleton, "FindSlot", [slot_name])


func _hide_slot_attachment(skeleton: Object, slot_name: String) -> void:
    var slot := _find_slot(skeleton, slot_name)
    if slot == null:
        return

    _try_call(slot, "set_attachment", [null]) or _try_call(slot, "SetAttachment", [null])


func _is_current_animation_complete(animation_state: Object, track: int) -> bool:
    var track_entry = _try_call_with_result(animation_state, "get_current", [track])
    if track_entry == null:
        track_entry = _try_call_with_result(animation_state, "GetCurrent", [track])

    if track_entry == null:
        return false

    var snake_result = _try_call_with_result(track_entry, "is_complete", [])
    if snake_result != null:
        return bool(snake_result)

    var pascal_result = _try_call_with_result(track_entry, "IsComplete", [])
    if pascal_result != null:
        return bool(pascal_result)

    return false


func _try_call(obj: Object, method_name: String, args: Array) -> bool:
    if obj == null or not obj.has_method(method_name):
        return false

    obj.callv(method_name, args)
    return true


func _try_call_with_result(obj: Object, method_name: String, args: Array):
    if obj == null or not obj.has_method(method_name):
        return null

    return obj.callv(method_name, args)
