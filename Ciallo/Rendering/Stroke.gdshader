// Ciallo's articulated line shader!
shader_type canvas_item;
render_mode unshaded;

const int Vanilla = 0, Stamp = 1, Airbrush = 2;
uniform int strokeType : hint_enum("Vanilla", "Stamp", "Airbrush") = 0;
uniform vec4 materialColor : source_color = vec4(0.0, 0.0, 0.0, 1.0);
instance uniform vec4 overridingColor = vec4(0.0, 0.0, 0.0, -1.0);
// Canvas mode is used for drawing wireframe
uniform int radiusMode : hint_enum("World", "Canvas") = 0;
uniform float canvasZoomThreshold : hint_range(0.0, 10.0, 0.1) = 0;
// Dash
uniform float dashLength = -1.0; // length with unit "L/r"
uniform float gapLength = -1.0;
uniform float dashForwardSpeed = 0.0;

// ------stamp parameters-----------
//// Fail to compile:
// hint_range(intervalMin, 10.0, intervalStep) .
// const float intervalMin = 1.0/64.0;
// const float intervalStep = 1.0/128.0;
uniform float stampInterval : hint_range(0.015625, 10.0, 0.0078125) = 0.2;
// ---------------------------------

uniform sampler2D falloffCurve;

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
varying flat float sinTheta;
varying flat float cosTheta;
varying flat float tanTheta;

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
	r0 = max(rs.x, 1e-7);
	r1 = max(rs.y, 1e-7);
	vec2 ns = unpack2floats(COLOR.y);
	n0 = ns.x;
	n1 = ns.y;
	if(radiusMode > 0){
		float canvasZoom = length(CANVAS_MATRIX[0].xy); // Cannot support shear (or skew in godot)
		canvasZoom = canvasZoom > canvasZoomThreshold ? canvasZoom : canvasZoomThreshold;
		r0 /= canvasZoom;
		r1 /= canvasZoom;
		n0 *= canvasZoom;
		n1 *= canvasZoom;
	}
	segmentLength = distance(p1, p0);
	cosTheta = (r0 - r1)/distance(p0, p1);
	sinTheta = sqrt(1.0 - cosTheta * cosTheta);
	tanTheta = cosTheta < 1e-7 ? 1e30 : sinTheta/cosTheta;

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
	if(abs(cosTheta) < 1e-7) return x/r0;
	return -1.0/cosTheta * log(1.0 - cosTheta*x/r0);
}
float n2x(float n) {
	if(abs(cosTheta) < 1e-7) return n*r0;
	return r0/cosTheta * (1.0-exp(-cosTheta*n));
}

float sampleFalloffCurve(float t){
	return texture(falloffCurve, vec2(t, 0)).r;
}

void fragment() {
	vec2 pLocal = vec2(dot(p-p0, tangent), dot(p-p0, normal));
	float d0 = distance(p, p0);
    float d1 = distance(p, p1);
	float cosPhi0 = pLocal.x / d0;
    float cosPhi1 = (pLocal.x - segmentLength) / d1;
	/*----------------------------------------------------------*/
	if(strokeType == Vanilla){
		if(cosPhi0 < cosTheta && d0 > r0) discard;
    	if(cosPhi1 > cosTheta && d1 > r1) discard;

		COLOR = overridingColor.a >= 0.0 ? overridingColor : materialColor;
		//// Anti antialiasing
		const float aaStart = 0.97;
		float lambda;
		if (cosPhi0 < cosTheta) lambda = d0/r0;
		else if (cosPhi1 > cosTheta) lambda = d1/r1;
		else {
			vec2 p_r = pLocal - vec2(abs(pLocal.y)/tanTheta, 0);
			float r_p = r0 - cosTheta * p_r.x;
			lambda = abs(pLocal.y) / r_p * sinTheta;
		}

		if(lambda > aaStart){
			float t = (lambda - aaStart)/(1.0 - aaStart);
			COLOR.a = COLOR.a * smoothstep(1.0, 0.0, t);
		}
	}
	/*----------------------------------------------------------*/
 	if(strokeType == Stamp){
 		float x1 = pLocal.x - r;
 		float x2 = pLocal.x + r;
 		float x1Idx = x2n(x1) + n0;
 		float x2Idx = x2n(x2) + n1;
 		float startIdx = x1 < 0.0 ? n0 : x1Idx;
		startIdx /= stampInterval;
 		float endIdx = x2 > segmentLength ? n1 : x2Idx;
		endIdx /= stampInterval;

 		int i = 0;
 		int MAX_i = 128;
 		float currIdx = ceil(startIdx);
 		float currA = 1e-7;
 		while(i < MAX_i && currIdx < endIdx){
 			float currStampLocalX = n2x(currIdx * stampInterval - n0);
 			vec2 pToCurrStamp = pLocal - vec2(currStampLocalX, 0.0);
 			//vec2 uv = (pToCurrStamp/r + 1.0)/2.0;
			vec2 c = pToCurrStamp/r;
			//float sampledA = length(uv) > 1.0 ? 1e-7 : 0.5;
 			float sampledA = abs(c.x) > 1.0 || abs(c.y) > 1.0 ? 1e-7 : 0.5;
 			currA = sampledA + currA * (1.0 - sampledA);
			i++;
			currIdx += 1.0;
 		}
 		COLOR = vec4(materialColor.rgb, currA);
 	}
	/*----------------------------------------------------------*/
	if(strokeType == Airbrush){
		if(cosPhi0 < cosTheta && d0 > r0) discard;
    	if(cosPhi1 > cosTheta && d1 > r1) discard;

		// The Whispers Of the olD Ones
		vec2 p_r = pLocal - vec2(abs(pLocal.y)/tanTheta, 0);
		float flow = materialColor.a;
		float transparency0 = d0 > r0 ? 1.0:sqrt(1.0 - flow*sampleFalloffCurve(d0/r0));
        float transparency1 = d1 > r1 ? 1.0:sqrt(1.0 - flow*sampleFalloffCurve(d1/r1));
		float transparency;

		// Shen already forget about every details here, and he is too lazy to remind of.
		// A bunch of math derived with the continuous form of airbrush here.
        if(p_r.x <= 0.0){
            transparency = transparency0/transparency1;
        }
        if(p_r.x > 0.0 && p_r.x < segmentLength){
			float r_p = r0 - cosTheta * p_r.x;
            float lambda = abs(pLocal.y) / r_p * sinTheta;
            transparency = (1.0 - flow*sampleFalloffCurve(lambda))/transparency0/transparency1;
        }
        if(p_r.x >= segmentLength){
            transparency = transparency1/transparency0;
        }
		COLOR = vec4(materialColor.rgb, 1.0 - transparency);
	}

	// Dash
	if(dashLength > 0.0){
		float unitLength = dashLength + (gapLength > 0.0 ? gapLength : dashLength);
		float pixelN = x2n(pLocal.x) + n0;
		if(mod(pixelN + TIME * dashForwardSpeed, unitLength) > dashLength) discard;
	}

	// Be mindful with premultiply alpha
	// See: https://github.com/godotengine/godot/issues/99715
}