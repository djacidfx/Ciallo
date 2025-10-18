// Clip a rect into dot
shader_type canvas_item;
render_mode unshaded;

uniform int radiusMode : hint_enum("World", "Canvas") = 0;
uniform float canvasZoomThreshold : hint_range(0.0, 10.0, 0.1) = 0;

void vertex(){
	float radius = INSTANCE_CUSTOM.r;
	if(radiusMode == 1){
		vec2 canvasZoom = vec2(length(CANVAS_MATRIX[0].xy), length(CANVAS_MATRIX[1].xy));
		vec2 modelScale = vec2(length(MODEL_MATRIX[0].xy), length(MODEL_MATRIX[1].xy));
		
		canvasZoom.x = max(canvasZoom.x, canvasZoomThreshold);
		canvasZoom.y = max(canvasZoom.y, canvasZoomThreshold);
		
		VERTEX /= canvasZoom * modelScale;
		VERTEX *= radius;
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