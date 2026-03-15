extends TextureRect

var parent: Control
var parent_rect: Rect2
var screen_rect: Rect2
var rect_ratio: Vector2

func _ready() -> void:
	parent = get_parent()
	if parent:
		parent_rect = parent.get_rect()
		screen_rect = get_viewport_rect()
		parent.item_rect_changed.connect(_on_item_rect_changed)
		rect_ratio = Vector2(parent_rect.size.x / screen_rect.size.x, parent_rect.size.y / screen_rect.size.y)

func _on_item_rect_changed() -> void:
	screen_rect = get_viewport_rect()
	rect_ratio = Vector2(parent_rect.size.x / screen_rect.size.x, parent_rect.size.y / screen_rect.size.y)

func _process(_delta: float) -> void:
	var mouse_pos := parent.get_global_mouse_position()
	var relative_mouse_pos := (parent.global_position + parent.size / 2) - Vector2(mouse_pos)
	var transformed_relative_offset := Vector2(relative_mouse_pos.x * rect_ratio.x, relative_mouse_pos.y * rect_ratio.y)
	position = -transformed_relative_offset * (size.x / 300.0) # This is 0.2 at the inventory size
