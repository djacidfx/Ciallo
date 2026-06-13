shader_type canvas_item;
render_mode unshaded;

uniform vec4 OverridingColor : source_color;

void fragment() {
	float a = COLOR.a * OverridingColor.a;
	if(a < 0.001) discard;
	COLOR = vec4(OverridingColor.rgb * a, a);
}
