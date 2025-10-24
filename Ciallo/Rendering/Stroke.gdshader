// Ciallo's articulated line shader!
shader_type canvas_item;
render_mode unshaded;
render_mode skip_vertex_transform;

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
uniform sampler2D stampTexture;
uniform sampler2D multiplyTexture;
uniform mat2 coordinateTransform;
uniform int rotationNoiseOctave;
uniform float rotationNoiseAmplitude = 0; // `= PI` don't compile
uniform float rotationNoiseFrequency;
// ---------------------------------

uniform sampler2D falloffCurve;

varying flat vec2 p0;
varying flat vec2 p1;
varying flat float r0;
varying flat float r1;
varying flat float n0;
varying flat float n1;
varying flat float pp0; // pen pressure
varying flat float pp1;
varying vec2 p;
varying float r;

varying flat vec2 tangent;
varying flat vec2 normal;
varying flat float segmentLength;
varying flat float sinTheta;
varying flat float cosTheta;
varying flat float tanTheta;
varying flat vec2 scale;

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
	vec2 pps = unpack2floats(COLOR.z);
	pp0 = pps.x;
	pp1 = pps.y;
	// Trying to be invariant to model scale.
	vec2 canvasZoom = vec2(length(CANVAS_MATRIX[0].xy), length(CANVAS_MATRIX[1].xy));
	vec2 modelScale = vec2(length(MODEL_MATRIX[0].xy), length(MODEL_MATRIX[1].xy));
	scale = canvasZoom * modelScale;

	if(radiusMode == 1){
		float zoom = max(scale.x, scale.y);
		r0 /= zoom;
		r1 /= zoom;
		n0 *= zoom;
		n1 *= zoom;
	}
	segmentLength = distance(p1, p0);
	cosTheta = (r0 - r1)/distance(p0, p1);
	sinTheta = sqrt(1.0 - cosTheta * cosTheta);
	tanTheta = abs(cosTheta) < 1e-5 ? 1e10 * sign(cosTheta) : sinTheta/cosTheta;
	if(abs(tanTheta) < 1e-5) tanTheta = 1e10;

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
		vec2 offset = offsetSign.x * r * tangent + offsetSign.y * r * normal * normalTanValue;
		p = position + offset;
	}

	VERTEX = (MODEL_MATRIX * vec4(p, 0.0, 1.0)).xy;
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

// ---------------------noise helper gen by copilot--------------------
const int MAX_OCTAVES = 16;
float fade(float t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}
float grad1(float i) {
    // Simple hash based on sine; deterministic without textures
    float h = fract(sin(i * 127.1) * 43758.5453123);
    return h * 2.0 - 1.0;
}
float perlinNoise1D(float x) {
    float i0 = floor(x);
    float i1 = i0 + 1.0;
    float t  = x - i0;

    float g0 = grad1(i0);
    float g1 = grad1(i1);

    float u = fade(t);

    return 2.0 * mix(g0 * t, g1 * (t - 1.0), u);
}

// Fractal Brownian Motion (FBM) over 1D Perlin
float noise1D(float x) {
	x = mod(x, 1024.0);
	x = abs(x - 512.0);
    float sum = 0.0;
    float amp = 1.0;      // start at 1.0, halve each octave (persistence = 0.5)
    float freq = rotationNoiseFrequency; // start at base frequency, double each octave (lacunarity = 2.0)
    float ampSum = 0.0;

    for (int i = 0; i < MAX_OCTAVES; ++i) {
        if (i >= rotationNoiseOctave) break;
        sum += perlinNoise1D(x * freq) * amp;
        ampSum += amp;

        freq *= 2.0;
        amp  *= 0.5;
    }

    // Normalize by cumulative amp so final amplitude parameter is consistent
    if (ampSum > 0.0) {
        sum /= ampSum;
    }

    return sum * rotationNoiseAmplitude;
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
		if(d0 < r0 && d1 < r1){
        	discard;
    	}
    	float flow = COLOR.a;
    	if(d0 < r0 || d1 < r1){
        	flow = 1.0 - sqrt(1.0 - COLOR.a);
    	}
		COLOR = vec4(COLOR.rgb, flow);

		//// Anti antialiasing
		float zoom = min(scale.x, scale.y);
		float aaEdge = 0.25 / zoom;
		float aaStart = 1.0 - aaEdge;
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
		float flow = materialColor.a;
		// Main loop
 		while(i < MAX_i && currIdx < endIdx){
			float currN = currIdx * stampInterval;
 			float currStampLocalX = n2x(currN - n0);
 			vec2 pToCurrStamp = pLocal - vec2(currStampLocalX, 0.0);
			// Transform
			pToCurrStamp *= coordinateTransform;
			// Rotation noise
			float rotationNoise = noise1D(currIdx);
			float cosRot = cos(rotationNoise);
			float sinRot = sin(rotationNoise);
			pToCurrStamp *= mat2(vec2(cosRot, sinRot), vec2(-sinRot, cosRot));

 			vec2 uv = (pToCurrStamp/r + 1.0)/2.0;
			float noise = texture(multiplyTexture, uv).r;
			bool isOutDisk = uv.x > 1.0 || uv.y > 1.0 || length(pToCurrStamp/r) > 1.0;
			float sampledA = isOutDisk ? 1e-7 : noise * flow * texture(stampTexture, uv).r;

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
            float lambda = abs(pLocal.y) / sinTheta / r_p;
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
