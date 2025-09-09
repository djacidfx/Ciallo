shader_type canvas_item;
render_mode world_vertex_coords;
//render_mode skip_vertex_transform;
render_mode unshaded;

uniform int strokeType : hint_enum("Vanilla", "Stamp") = 0;
uniform vec4 strokeColor : source_color = vec4(0.0, 0.0, 0.0, 1.0);

// ------stamp parameters-----------
//// Fail to compile:
// hint_range(intervalMin, 10.0, intervalStep) .
// const float intervalMin = 1.0/64.0;
// const float intervalStep = 1.0/128.0;
uniform float stampInterval : hint_range(0.015625, 10.0, 0.0078125) = 0.2;
// ---------------------------------

varying flat vec2 p0;
varying flat vec2 p1;
varying flat float r0;
varying flat float r1;
varying flat float n0;
varying flat float n1;
varying vec2 p;
varying float r;

varying flat vec2 tangent;
varying flat vec2 normal;
varying flat float segmentLength;
varying flat float cosTheta;
varying flat float tanTheta;
varying flat float sinTheta;

vec2 unpack2floats(float f) {
    uint word = floatBitsToUint(f);
    // unpackHalf2x16 returns vec2( halfToFloat(high16), halfToFloat(low16) )
    return unpackHalf2x16(word);
}

void vertex() {
	// Canvas item not 
	p0 = INSTANCE_CUSTOM.xy;
	p1 = INSTANCE_CUSTOM.zw;
	vec2 rs = unpack2floats(COLOR.x);
	r0 = rs.x;
	r1 = rs.y;
	vec2 ns = unpack2floats(COLOR.y);
	n0 = ns.x/stampInterval;
	n1 = ns.y/stampInterval;
	segmentLength = distance(p1, p0);
	cosTheta = (r0 - r1)/distance(p0, p1);
	sinTheta = sqrt(1.0 - pow(cosTheta, 2.0));
	tanTheta = abs(cosTheta) > 1e-10 ? sinTheta/cosTheta : 1e30;

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

	float tanHalfTheta = sqrt((1.0+cosTheta) / (1.0-cosTheta));
    float cotHalfTheta = 1.0 / tanHalfTheta;
    float normalTanValue = vec4(tanHalfTheta, tanHalfTheta, cotHalfTheta, cotHalfTheta)[VERTEX_ID];
    bool isIll = normalTanValue > 10.0 || normalTanValue < 0.1;

	if(!isIll)
	{
		p = position +
        offsetSign.x * r * tangent +
        offsetSign.y * r * normal * normalTanValue;
	}

	VERTEX = p;
	VERTEX = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float x2n(float x) {
	if(abs(cosTheta) < 1e-10) return x/(stampInterval * (r0 + r1) * 0.5);
	return -1.0/cosTheta/stampInterval * log(1.0 - cosTheta*x/r0);
}
float n2x(float n) {
	if(abs(cosTheta) < 1e-10) return n * stampInterval * (r0 + r1) * 0.5;
	return r0/cosTheta * (1.0-exp(-stampInterval*cosTheta*n));
}

void fragment() {
	vec2 pLocal = vec2(dot(p-p0, tangent), dot(p-p0, normal));
	float d0 = distance(p, p0);
    float d1 = distance(p, p1);
	float d0cos = pLocal.x / d0;
    float d1cos = (pLocal.x - segmentLength) / d1;

	if(strokeType == 0){
		if(d0cos < cosTheta && d0 > r0) discard;
    	if(d1cos > cosTheta && d1 > r1) discard;
		
		COLOR = strokeColor;
		//// Anti antialiasing
		const float aaStart = 0.95;
		float dMidAxis;
		if (d0cos < cosTheta) dMidAxis = d0/r0;
		else if (d1cos > cosTheta) dMidAxis = d1/r1;
		else {
			vec2 rp = pLocal - vec2(pLocal.y/tanTheta, 0);
			float pr = r0 - cosTheta * rp.x;
			dMidAxis = abs(pLocal.y) / pr * sinTheta;
		}
		
		if(dMidAxis > aaStart){
			float t = (dMidAxis - aaStart)/(1.0 - aaStart);
			COLOR.a = COLOR.a * smoothstep(1.0, 0.0, t);
		}
	}
 	if(strokeType == 1){
 		float x1 = pLocal.x - r;
 		float x2 = pLocal.x + r;
 		float x1Idx = x2n(x1) + n0;
 		float x2Idx = x2n(x2) + n1;
 		float startIdx = x1 < 0.0 ? n0 : x1Idx;
 		float endIdx = x2 > segmentLength ? n1 : x2Idx;

 		int i = 0;
 		int MAX_i = 128;
 		float currIdx = ceil(startIdx);
 		float currA = 1e-10;
 		while(i < MAX_i && currIdx < endIdx){
 			float currStampLocalX = n2x(currIdx - n0);
 			vec2 pToCurrStamp = pLocal - vec2(currStampLocalX, 0.0);
 			//vec2 uv = (pToCurrStamp/r + 1.0)/2.0;
			vec2 uv = pToCurrStamp/r;
			//float sampledA = length(uv) > 1.0 ? 1e-7 : 0.5;
 			float sampledA = abs(uv.x) > 1.0 || abs(uv.y) > 1.0 ? 1e-7 : 0.5;
 			currA = sampledA + currA * (1.0 - sampledA);
			i++;
			currIdx += 1.0;
 		}
 		COLOR = vec4(strokeColor.rgb, currA);
 	}
	// Be mindful with premultiply alpha
	// See: https://github.com/godotengine/godot/issues/99715
}