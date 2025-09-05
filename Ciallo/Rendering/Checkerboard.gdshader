shader_type canvas_item;

uniform float size : hint_range(1.0, 100.0) = 10;
uniform vec4 color1 : source_color = vec4(0.9, 0.9, 0.9, 1.0);
uniform vec4 color2 : source_color = vec4(0.6, 0.6, 0.6, 1.0);

void fragment() {
    vec2 pos = floor(FRAGCOORD.xy / size);
    float pattern_mask = mod(pos.x + mod(pos.y, 2.0), 2.0);
	vec4 checkerboard_color = mix(color1, color2, pattern_mask);
	vec4 target_color = COLOR;
	float a = target_color.a;
	vec3 color = target_color.rgb * a + checkerboard_color.rgb * (1.0-a);
    COLOR = vec4(color, 1.0);
}