shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform vec4 OverridingColor : source_color;

void fragment() {
	vec4 c = textureLod(screen_texture, SCREEN_UV, 0.0);

 	vec3 RGB = vec3(0.0);
	if (c.a > 0.001) {
		RGB = OverridingColor.rgb / c.a;
	}
	float A = c.a * OverridingColor.a;

	COLOR = vec4(RGB, A);
}