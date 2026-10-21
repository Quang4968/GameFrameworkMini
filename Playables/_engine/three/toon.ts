// A port of a stepped toon shader of the kind Unity games use for low-poly models: only the main light
// direction counts, `light = floor(saturate(NdotL / lightCut) * steps) / steps`, colour = albedo *
// mix(shadowColor, 1, light); plus an inverted-hull outline. Supports clipping planes (masking a model to a
// board rect). Values come from the game's material through the art map.
import {
  BackSide,
  Color,
  SRGBColorSpace,
  type Material,
  Mesh,
  type Object3D,
  type Plane,
  ShaderMaterial,
  type Texture,
  type Vector3,
} from 'three';

/** A colour as a CSS hex string or Unity's sRGB floats [r, g, b(, a)]. */
export type ToonColor = string | number[];

export interface ToonOptions {
  map?: Texture;
  color?: ToonColor;
  /** _UseColorMap: the map is an R/G/B mask, each channel recoloured (one model, many paints). */
  channels?: [ToonColor, ToonColor, ToonColor];
  steps: number;
  lightCut: number;
  shadowColor: string;
  /** Direction towards the light, world space. */
  toLight: Vector3;
  clippingPlanes?: Plane[];
}

const TOON_VERT = /* glsl */ `
#include <common>
#include <clipping_planes_pars_vertex>
varying vec2 vUv;
varying vec3 vNormalW;
void main() {
  vUv = uv;
  vNormalW = normalize(mat3(modelMatrix) * normal);
  vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
  gl_Position = projectionMatrix * mvPosition;
  #include <clipping_planes_vertex>
}`;

const TOON_FRAG = /* glsl */ `
#include <common>
#include <clipping_planes_pars_fragment>
uniform sampler2D map;
uniform bool useMap;
uniform bool useChannels;
uniform vec3 channelR;
uniform vec3 channelG;
uniform vec3 channelB;
uniform vec3 color;
uniform vec3 shadowColor;
uniform vec3 toLight;
uniform float steps;
uniform float lightCut;
varying vec2 vUv;
varying vec3 vNormalW;
void main() {
  #include <clipping_planes_fragment>
  vec4 albedo = vec4(color, 1.0);
  if (useMap) {
    vec4 tex = texture2D(map, vUv);
    albedo.rgb *= useChannels ? tex.r * channelR + tex.g * channelG + tex.b * channelB : tex.rgb;
  }
  float ndl = dot(normalize(vNormalW), normalize(toLight));
  float light = floor(clamp(ndl / lightCut, 0.0, 1.0) * steps) / steps;
  gl_FragColor = vec4(albedo.rgb * mix(shadowColor, vec3(1.0), light), albedo.a);
  #include <colorspace_fragment>
}`;

const OUTLINE_VERT = /* glsl */ `
#include <common>
#include <clipping_planes_pars_vertex>
uniform float width;
void main() {
  vec4 world = modelMatrix * vec4(position, 1.0);
  world.xyz += normalize(mat3(modelMatrix) * normal) * width;
  vec4 mvPosition = viewMatrix * world;
  gl_Position = projectionMatrix * mvPosition;
  #include <clipping_planes_vertex>
}`;

const OUTLINE_FRAG = /* glsl */ `
#include <common>
#include <clipping_planes_pars_fragment>
uniform vec3 color;
void main() {
  #include <clipping_planes_fragment>
  gl_FragColor = vec4(color, 1.0);
  #include <colorspace_fragment>
}`;

const linear = (c: ToonColor) => (typeof c === 'string' ? new Color(c) : new Color().setRGB(c[0], c[1], c[2], SRGBColorSpace));

export function toonMaterial(o: ToonOptions): ShaderMaterial {
  return new ShaderMaterial({
    // Plain uniforms, not UniformsUtils.merge: merging clones the texture before its image has loaded.
    uniforms: {
      map: { value: o.map ?? null },
      useMap: { value: !!o.map },
      useChannels: { value: !!o.channels },
      channelR: { value: linear(o.channels?.[0] ?? '#ffffff') },
      channelG: { value: linear(o.channels?.[1] ?? '#ffffff') },
      channelB: { value: linear(o.channels?.[2] ?? '#ffffff') },
      color: { value: linear(o.color ?? '#ffffff') },
      shadowColor: { value: linear(o.shadowColor) },
      toLight: { value: o.toLight.clone().normalize() },
      steps: { value: o.steps },
      lightCut: { value: o.lightCut },
    },
    vertexShader: TOON_VERT,
    fragmentShader: TOON_FRAG,
    // In the transparent pass with depth on, so renderOrder can put models above flat quads that skip depth.
    transparent: true,
    clipping: !!o.clippingPlanes,
    ...(o.clippingPlanes ? { clippingPlanes: o.clippingPlanes } : {}),
  });
}

export function outlineMaterial(width: number, color: ToonColor = '#000000', clippingPlanes?: Plane[]): ShaderMaterial {
  return new ShaderMaterial({
    uniforms: { width: { value: width }, color: { value: linear(color) } },
    vertexShader: OUTLINE_VERT,
    fragmentShader: OUTLINE_FRAG,
    side: BackSide,
    transparent: true,
    clipping: !!clippingPlanes,
    ...(clippingPlanes ? { clippingPlanes } : {}),
  });
}

/** Gives every mesh under `root` the toon material and an outline hull (world-space `width`). */
export function applyToon(root: Object3D, material: Material, outline?: ShaderMaterial, renderOrder = 20_000): void {
  const meshes: Mesh[] = [];
  root.traverse((o) => {
    if ((o as Mesh).isMesh) meshes.push(o as Mesh);
  });
  for (const mesh of meshes) {
    mesh.material = material;
    mesh.renderOrder = renderOrder + 1;
    if (outline) {
      const hull = new Mesh(mesh.geometry, outline);
      hull.renderOrder = renderOrder;
      mesh.add(hull);
    }
  }
}
