// Clip a rect into dot
shader_type canvas_item;
render_mode unshaded;

uniform int radiusMode : hint_enum("World", "Canvas") = 0;
uniform float canvasZoomThreshold : hint_range(0.0, 10.0, 0.1) = 0;

void vertex(){
	if(radiusMode == 1){
		float canvasZoom = length(CANVAS_MATRIX[0].xy); // Cannot support shear (or skew in godot)
		canvasZoom = canvasZoom > canvasZoomThreshold ? canvasZoom : canvasZoomThreshold;
		VERTEX /= canvasZoom;
	}
}

void fragment() {
	vec2 toMid = (UV-0.5) * 2.0;
	if(length(toMid) < 1.0){
		COLOR = COLOR;
	}
	else{
		COLOR = vec4(COLOR.rgb, 0.0);
	}
}