// Ciallo's articulated line shader!
shader_type canvas_item;
render_mode unshaded;
render_mode skip_vertex_transform;

const int Vanilla = 0, Stamp = 1, Airbrush = 2;

struct StampFlag {
    int StampTexture;
    int MaskTexture;
    int RotationNoise;
	int MaskDisk;
};
const StampFlag StampFlags = StampFlag(1 << 0, 1 << 1, 1 << 2, 1 << 3);

struct BrushFlag {
    int Pressure2FlowCurve;
    int Dash;
};
const BrushFlag BrushFlags = BrushFlag(1 << 0, 1 << 1);

uniform int StrokeType : hint_enum("Vanilla", "Stamp", "Airbrush") = 0;
uniform int ActiveBrushFlags = 0;
uniform vec4 MaterialColor : source_color = vec4(0.0, 0.0, 0.0, 1.0);
instance uniform vec4 OverridingColor = vec4(0.0, 0.0, 0.0, -1.0);
// Canvas mode is used for drawing wireframe
uniform int RadiusMode : hint_enum("World", "Canvas") = 0;
// Dash
uniform float DashLength = -1.0; // radius-normalized length
uniform float GapLength = -1.0;
uniform float DashForwardSpeed = 0.0;
// Pen pressure map curve
uniform sampler2D Pressure2FlowCurve;

// ------stamp parameters-----------
//// Fail to compile:
// hint_range(intervalMin, 10.0, intervalStep) .
// const float intervalMin = 1.0/64.0;
// const float intervalStep = 1.0/128.0;
uniform int ActiveStampFlags;
uniform float StampInterval : hint_range(0.015625, 10.0, 0.0078125) = 0.2;
uniform sampler2D StampTexture;
uniform mat2 CoordinateTransform;
uniform sampler2D DiskOpacityCurve;
uniform sampler2D MultiplyTexture;

uniform int RotationNoiseOctave;
uniform float RotationNoiseAmplitude = 0; // `= PI` don't compile
uniform float RotationNoiseFrequency;
// ---------------------------------
uniform sampler2D FalloffCurve;
uniform float AlphaDensity;

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
varying flat float scale;

vec2 unpack2floats(float f) {
    uint word = floatBitsToUint(f);
    // unpackHalf2x16 returns vec2( halfToFloat(high16), halfToFloat(low16) )
    return unpackHalf2x16(word);
}

void vertex() {
	mat3 canvas_linear = mat3(CANVAS_MATRIX);  // discard translation
    scale = length(canvas_linear * vec3(1.0, 0.0, 0.0));  // X-scale
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

	if(RadiusMode == 1){
		r0 /= scale;
		r1 /= scale;
		n0 *= scale;
		n1 *= scale;
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

float sampleCurve(sampler2D curve, float t){
	return texture(curve, vec2(t, 0)).r;
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
    float freq = RotationNoiseFrequency; // start at base frequency, double each octave (lacunarity = 2.0)
    float ampSum = 0.0;

    for (int i = 0; i < MAX_OCTAVES; ++i) {
        if (i >= RotationNoiseOctave) break;
        sum += perlinNoise1D(x * freq) * amp;
        ampSum += amp;

        freq *= 2.0;
        amp  *= 0.5;
    }

    // Normalize by cumulative amp so final amplitude parameter is consistent
    if (ampSum > 0.0) {
        sum /= ampSum;
    }

    return sum * RotationNoiseAmplitude;
}

vec2 solveTheQuadraticEq(vec2 pLocal){
	float a, b, c, delta;
    a = 1.0 - cosTheta * cosTheta;
    b = 2.0 * (r0 * cosTheta - pLocal.x);
    c = pLocal.x * pLocal.x + pLocal.y * pLocal.y - r0 * r0;
    delta = b * b - 4.0*a*c;
    if(delta <= 0.0) discard;

	float tempMathBlock = b + sign(b) * sqrt(delta);
    float x1 = -2.0 * c / tempMathBlock;
    float x2 = -tempMathBlock / (2.0*a);
    vec2 result = vec2(min(x1, x2), max(x1, x2));
	return result;
}

void fragment() {
	vec2 pLocal = vec2(dot(p-p0, tangent), dot(p-p0, normal));
	float d0 = distance(p, p0);
    float d1 = distance(p, p1);
	float cosPhi0 = pLocal.x / d0;
    float cosPhi1 = (pLocal.x - segmentLength) / d1;
	vec2 p_r = pLocal - vec2(abs(pLocal.y)/tanTheta, 0);
	float r_p = r0 - cosTheta * p_r.x;

	float t = mix(pp0, pp1, p_r.x/segmentLength);
	t = clamp(t, 0.0, 1.0);
	float flowRatio = ((ActiveBrushFlags & BrushFlags.Pressure2FlowCurve) != 0) ? sampleCurve(Pressure2FlowCurve, t) : 1.0;

	/*----------------------------------------------------------*/
	if(StrokeType == Vanilla){
		if(cosPhi0 < cosTheta && d0 > r0) discard;
    	if(cosPhi1 > cosTheta && d1 > r1) discard;

		COLOR = OverridingColor.a >= 0.0 ? OverridingColor : MaterialColor;
    	float targetFlow = COLOR.a * flowRatio;
		COLOR.a = COLOR.a * flowRatio;
		if(targetFlow < 0.999){ // Can avoid little white dot
			if(d0 < r0 && d1 < r1){
        		discard;
    		}
		}
		else{
			//// Anti antialiasing
			//// No configuration of screen space AA is satifying enough to draw the strokes
			//// Have to AA manually
			//// AA end point makes visible gaps with transparency.
			float aaEdge = 3.0 / scale;
			float aaStart = 1.0 - aaEdge/r_p;
			aaStart = clamp(aaStart, 0.0, 1.0);

			float lambda;
			if (cosPhi0 < cosTheta) lambda = d0/r0;
			else if (cosPhi1 > cosTheta) lambda = d1/r1;
			else lambda = abs(pLocal.y) / r_p * sinTheta;
			lambda = clamp(lambda, 0.0, 1.0);

			if(lambda > aaStart){
				float t = (lambda - aaStart)/(1.0 - aaStart);
				COLOR.a = COLOR.a * smoothstep(1.0, 0.5, t);
			}
		}

		if(d0 < r0 || d1 < r1){
        	COLOR.a = 1.0 - sqrt(1.0 - COLOR.a);
    	}
	}
	/*----------------------------------------------------------*/
  	if(StrokeType == Stamp){
 		// The quadratic equation
	    vec2 result = solveTheQuadraticEq(pLocal);
		float x1 = result.x;
		float x2 = result.y;

 		float startN = x1 < 0.0 ? n0 : x2n(x1) + n0;
		float startStampIdx = startN / StampInterval;
 		float endN = x2 > segmentLength ? n1 : x2n(x2) + n0;
		float endStampIdx = endN / StampInterval;

 		int i = 0;
 		int MAX_i = 128;
 		float currStampIdx = ceil(startStampIdx);
 		float currA = 1e-7;
		float flow = MaterialColor.a * flowRatio;
		// Main loop
 		while(i < MAX_i && currStampIdx < endStampIdx){
			float currN = currStampIdx * StampInterval;
 			float currStampLocalX = n2x(currN - n0);
 			vec2 pToCurrStamp = pLocal - vec2(currStampLocalX, 0.0);
			// Transform
			pToCurrStamp *= CoordinateTransform;
			// Rotation noise
			float rotationNoise = (ActiveStampFlags & StampFlags.RotationNoise) != 0 ? noise1D(currStampIdx) : 0.0;
			float cosRot = cos(rotationNoise);
			float sinRot = sin(rotationNoise);
			pToCurrStamp *= mat2(vec2(cosRot, sinRot), vec2(-sinRot, cosRot));

 			vec2 uv = (pToCurrStamp/r + 1.0)/2.0;
			float textureMask = ((ActiveStampFlags & StampFlags.MaskTexture) != 0) ? texture(MultiplyTexture, uv).r : 1.0;
			float nr = length(pToCurrStamp/r); // normalized r
			float dotMask = ((ActiveStampFlags & StampFlags.MaskDisk) != 0) ? sampleCurve(DiskOpacityCurve, nr) : 1.0;
			float stampSample = ((ActiveStampFlags & StampFlags.StampTexture) != 0) ? texture(StampTexture, uv).r : 1.0;
			bool isOutDisk = uv.x > 1.0 || uv.y > 1.0 || nr > 1.0;
			float sampledA = isOutDisk ? 1e-7 : textureMask * dotMask * flow * stampSample;

 			currA = sampledA + currA * (1.0 - sampledA);
			i++;
			currStampIdx += 1.0;
 		}
 		COLOR = vec4(MaterialColor.rgb, currA);
 	}
	/*----------------------------------------------------------*/
	if(StrokeType == Airbrush){
		if(cosPhi0 < cosTheta && d0 > r0) discard;
    	if(cosPhi1 > cosTheta && d1 > r1) discard;
		float flow = MaterialColor.a * flowRatio;
		vec2 result = solveTheQuadraticEq(pLocal);
		float x1 = result.x;
		float x2 = result.y;
		float startN = x1 < 0.0 ? 0.0 : x2n(x1);
		float endN = x2 > segmentLength ? (n1 - n0) : x2n(x2);
		float deltaN = endN - startN;

		float lambda = clamp(abs(pLocal.y) / r_p * sinTheta, 0.0, 1.0);
		float curveFall = sampleCurve(FalloffCurve, lambda);
		float A = 1.0 - exp(-deltaN * flow * curveFall * AlphaDensity);
		COLOR = vec4(MaterialColor.rgb, A);
	}

	// Dash
	if((ActiveBrushFlags & BrushFlags.Dash) != 0){
		float unitLength = DashLength + (GapLength > 0.0 ? GapLength : DashLength);
		float pixelN = x2n(pLocal.x) + n0;
		if(mod(pixelN + TIME * DashForwardSpeed, unitLength) > DashLength) discard;
	}

	// Be mindful with premultiply alpha
	// See: https://github.com/godotengine/godot/issues/99715
}
