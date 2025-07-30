shader_type canvas_item;
//render_mode world_vertex_coords;
//render_mode skip_vertex_transform;
render_mode unshaded;

uniform int strokeType : hint_enum("Solid", "Stamp") = 0;
uniform vec4 strokeColor : source_color = vec4(0.0, 0.0, 0.0, 1.0);

// ------stamp parameters-----------
//// hint_range(intervalMin, 10.0, intervalStep) not compile.
//const float intervalMin = 1.0/64.0;
//const float intervalStep = 1.0/128.0;
uniform float stampInterval : hint_range(0.015625, 10.0, 0.0078125) = 0.2;
// ---------------------------------

varying flat vec2 p0;
varying flat vec2 p1;
varying flat float r0;
varying flat float r1;
varying vec2 p;
varying float r;

varying flat vec2 tangent;
varying flat vec2 normal;
varying flat float len;
varying flat float cosTheta;

void vertex() {
	p0 = INSTANCE_CUSTOM.xy;
	p1 = INSTANCE_CUSTOM.zw;
	// Have to use COLOR to store the radius and stamp count values since the
	// INSTANCE_CUSTOM is a vec4 value, and is fully ouccupied for storing position.
	r0 = COLOR.x;
	r1 = COLOR.y;
	len = distance(p1, p0);
	cosTheta = (r0 - r1)/distance(p0, p1);

	tangent = normalize(p1 - p0);
	normal = vec2(-tangent.y, tangent.x);

	vec2 position = vec2[](p0, p0, p1, p1)[VERTEX_ID];
	vec2 offsetSign = vec2[](
        vec2(-1.0, -1.0),
        vec2(-1.0, 1.0),
        vec2(1.0, -1.0),
        vec2(1.0, 1.0)
    )[VERTEX_ID];
	r = vec4(r0, r0, r1, r1)[VERTEX_ID];

	p = position +
    offsetSign.x * r * tangent +
    offsetSign.y * r * normal;

	VERTEX = p;
}

void fragment() {
	vec2 pLocal = vec2(dot(p-p0, tangent), dot(p-p0, normal));
	float d0 = distance(p, p0);
    float d1 = distance(p, p1);
	float d0cos = pLocal.x / d0;
    float d1cos = (pLocal.x - len) / d1;

	if(pLocal.x < 0.0 && d0 > r) discard;
    if(pLocal.x > len && d1 > r) discard;

	// Premultiply alpha is necessary
	// See: https://github.com/godotengine/godot/issues/99715
    COLOR = strokeColor;
}