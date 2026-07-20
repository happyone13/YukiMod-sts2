@tool
extends Control

const NORMAL_TEXTURE_PATH := "res://YukiMod/images/cards/card_effects/card_normal_0.png"
const GREEN_TEXTURE_PATH := "res://YukiMod/images/cards/card_effects/card_green_0.png"
const RED_TEXTURE_PATH := "res://YukiMod/images/cards/card_effects/card_red_0.png"
const NORMAL_FNT_PATH := "res://YukiMod/images/cards/card_effects/card_normal.fnt"
const GREEN_FNT_PATH := "res://YukiMod/images/cards/card_effects/card_green.fnt"
const RED_FNT_PATH := "res://YukiMod/images/cards/card_effects/card_red.fnt"
const COST_TEXT_PATH := "CardContainer/CostText"
const COST_PREVIEW_PATH := "CardContainer/CostTextAtlasPreview"
const DIGIT_PREFIX := "PreviewDigit"

var _digit_region_cache: Dictionary = {}

@export var preview_text := "2":
	set(value):
		preview_text = value
		_refresh_preview()

@export_enum("FontLabel", "AtlasNormal", "AtlasGreen", "AtlasRed") var preview_mode := 1:
	set(value):
		preview_mode = value
		_refresh_preview()

func _ready() -> void:
	_refresh_preview()

func _notification(what: int) -> void:
	if what == NOTIFICATION_ENTER_TREE:
		_refresh_preview()

func _refresh_preview() -> void:
	if not is_inside_tree():
		return

	var cost_label: Label = get_node_or_null(COST_TEXT_PATH)
	var preview: Control = get_node_or_null(COST_PREVIEW_PATH)
	if cost_label == null or preview == null:
		return

	if preview_mode == 0:
		cost_label.show()
		preview.hide()
		_clear_digits(preview)
		return

	cost_label.hide()
	preview.show()
	_render_digits(preview)

func _render_digits(preview: Control) -> void:
	_clear_digits(preview)
	if preview_text.is_empty():
		return

	var texture := _load_texture_from_png(_get_texture_path())
	if texture == null:
		return

	var digit_regions := _get_digit_regions(_get_fnt_path())
	if digit_regions.is_empty():
		return

	var visible_digits: Array[String] = []
	var total_source_width := 0.0
	var max_source_height := 0.0
	for c in preview_text:
		if digit_regions.has(c):
			visible_digits.append(c)
			var region: Rect2 = digit_regions[c]
			total_source_width += region.size.x
			max_source_height = maxf(max_source_height, region.size.y)

	if visible_digits.is_empty():
		return

	var scale := minf(
		preview.size.y / max_source_height,
		preview.size.x / total_source_width
	)
	if scale <= 0.0:
		return

	var start_x := (preview.size.x - total_source_width * scale) * 0.5
	var start_y := (preview.size.y - max_source_height * scale) * 0.5
	var cursor_x := start_x

	for i in visible_digits.size():
		var region: Rect2 = digit_regions[visible_digits[i]]
		var atlas := AtlasTexture.new()
		atlas.atlas = texture
		atlas.region = region

		var rect := TextureRect.new()
		rect.name = "%s%d" % [DIGIT_PREFIX, i]
		rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
		rect.texture = atlas
		rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		rect.stretch_mode = TextureRect.STRETCH_SCALE
		rect.position = Vector2(cursor_x, start_y)
		rect.size = region.size * scale
		preview.add_child(rect)
		if Engine.is_editor_hint():
			rect.owner = owner
		cursor_x += region.size.x * scale

func _clear_digits(preview: Control) -> void:
	for child in preview.get_children():
		if child is Node and String(child.name).begins_with(DIGIT_PREFIX):
			child.queue_free()

func _get_texture_path() -> String:
	match preview_mode:
		1:
			return NORMAL_TEXTURE_PATH
		2:
			return GREEN_TEXTURE_PATH
		3:
			return RED_TEXTURE_PATH
		_:
			return NORMAL_TEXTURE_PATH

func _get_fnt_path() -> String:
	match preview_mode:
		1:
			return NORMAL_FNT_PATH
		2:
			return GREEN_FNT_PATH
		3:
			return RED_FNT_PATH
		_:
			return NORMAL_FNT_PATH

func _get_digit_regions(fnt_path: String) -> Dictionary:
	if _digit_region_cache.has(fnt_path):
		return _digit_region_cache[fnt_path]

	var result: Dictionary = {}
	var contents := FileAccess.get_file_as_string(fnt_path)
	for raw_line in contents.split("\n", false):
		var line := raw_line.strip_edges()
		if not line.begins_with("char id="):
			continue

		var fields := {}
		for token in line.split(" ", false):
			if not token.contains("="):
				continue
			var parts := token.split("=", false, 1)
			if parts.size() == 2:
				fields[parts[0]] = parts[1]

		var id := int(fields.get("id", "-1"))
		var glyph := ""
		if id >= 48 and id <= 57:
			glyph = "%d" % [id - 48]
		elif id == 88 or id == 120:
			glyph = "X"
		else:
			continue

		result[glyph] = Rect2(
			float(fields.get("x", "0")),
			float(fields.get("y", "0")),
			float(fields.get("width", "0")),
			float(fields.get("height", "0"))
		)

	_digit_region_cache[fnt_path] = result
	return result

func _load_texture_from_png(resource_path: String) -> Texture2D:
	var image := Image.load_from_file(ProjectSettings.globalize_path(resource_path))
	if image == null or image.is_empty():
		return null

	return ImageTexture.create_from_image(image)
