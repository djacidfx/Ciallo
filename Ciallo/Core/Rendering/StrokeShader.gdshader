shader_type canvas_item;
//render_mode world_vertex_coords;
//render_mode skip_vertex_transform;
render_mode unshaded;

varying flat vec2 p0;
varying flat vec2 p1;
varying flat float r0;
varying flat float r1;
varying vec2 p;

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
	float radius = vec4(r0, r0, r1, r1)[VERTEX_ID];

	// Apply the half angle formula from cos(theta) to tan(theta/2)
    float tanHalfTheta = sqrt((1.0+cosTheta) / (1.0-cosTheta));
    float cotHalfTheta = 1.0 / tanHalfTheta;
    float normalTanValue = vec4(tanHalfTheta, tanHalfTheta, cotHalfTheta, cotHalfTheta)[VERTEX_ID];
    bool isIll = normalTanValue > 10.0 || normalTanValue < 0.1;
    if(!isIll)
	{
		p = position +
        offsetSign.x * radius * tangent +
        offsetSign.y * radius * normal * normalTanValue;
	};

	VERTEX = p;
}

void fragment() {
	vec2 pLocal = vec2(dot(p-p0, tangent), dot(p-p0, normal));
	float d0 = distance(p, p0);
    float d1 = distance(p, p1);
	float d0cos = pLocal.x / d0;
    float d1cos = (pLocal.x - len) / d1;

	// Perhaps someday avoid using `discard` to enable early z test
	if(d0cos < cosTheta && d0 > r0) discard;
    if(d1cos > cosTheta && d1 > r1) discard;

	// Premultiply alpha is necessary
	// See: https://github.com/godotengine/godot/issues/99715
    COLOR = vec4(1.0, 1.0, 1.0, 0.5);
}