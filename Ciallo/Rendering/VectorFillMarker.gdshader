shader_type canvas_item;
render_mode unshaded;

void vertex() {
	vec2 canvas_zoom = vec2(length(CANVAS_MATRIX[0].xy), length(CANVAS_MATRIX[1].xy));
	VERTEX /= canvas_zoom;
}

void fragment() {
	COLOR = texture(TEXTURE, UV) * COLOR;
}
