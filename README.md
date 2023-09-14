---
hide:
  - navigation
---

# Vivify
Bring your map to life.

If you use any of these features, you MUST add "Vivify" as a requirement for your map for them to function, you can go [Here](https://github.com/Kylemc1413/SongCore/blob/master/README.md) to see how adding suggestions/requirements to the info.dat works. Also, Vivify will only load on v3 maps.

This documentation assumes basic understanding of custom events and tracks.

### Event Types
- [`SetMaterialProperty`](#setmaterialproperty)
- [`SetGlobalProperty`](#setglobalproperty)
- [`Blit`](#blit)
- [`DeclareCullingTexture`](#declarecullingtexture)
- [`DeclareRenderTexture`](#declarerendertexture)
- [`DestroyTexture`](#destroytexture)
- [`InstantiatePrefab`](#instantiateprefab)
- [`DestroyPrefab`](#destroyprefab)
- [`SetAnimatorProperty`](#setanimatorproperty)
- [`SetCameraProperty`](#setcameraproperty)
- [`AssignTrackPrefab`](#assigntrackprefab)

## Setting up Unity
First, you should to download the Unity Hub at https://unity3d.com/get-unity/download and follow the on-screen instructions until you get to the projects page, while skipping the recommended Unity Editor. Install a version of Unity (Installs > Install Editor) and for maximum compatibility, Beat Saber uses version 2021.3.16f1 found in the [archive](https://unity3d.com/get-unity/download/archive).

Now, you can create your own Unity project or use a specially made template for Vivify (coming soon).

If you want to test your shaders, make sure you enable `XR Plugin Management` enabled in your project. (Edit > Project Settings > XR Plugin Management > Install XR Plugin Management) After that installs, select your approriate Plug-in Provider. Enabling the Unity Mock HMD allows you to preview in vr without a hmd.

## Writing VR shaders

Beat Saber uses Single Pass Instanced rendering. Any incompatible shaders will only appear in the left eye. To make your shader compatible with this vr rendering method, add instancing support to your shader. See https://docs.unity3d.com/Manual/SinglePassInstancing.html for how to add instancing support. Look under "Post-Processing shaders" to see how to sample a screen space texture.

A tip for writing shaders, there are many commonly used structs/functions in UnityCG.cginc. As a few examples, `appdata_base`, `appdata_img`, `appdata_full`, and `v2f_img` can usually be used instead of writing your own structs and since most image effect shaders use the same vertex function, the include file has a `vert_img` that can be used with `#pragma vertex vert_img`.

### Creating an asset bundle
Visit https://learn.unity.com/tutorial/introduction-to-asset-bundles for a basic introduction to creating asset bundles. Even if you are using a template it is still useful to at least read through these instructions.

(Optional) See https://docs.unity3d.com/Manual/AssetBundles-Browser.html. this tool allows you to browse the contents of a built asset bundle.

After creating your asset bundle, place it in your map folder and call it `bundle` (no extension).
```
Map Folder
├── bundle
├── song.ogg
├── cover.jpg
├── ExpertPlusStandard.dat
└── info.dat
```
When referencing an asset's file path in an event, remember to write in all lower case. You can use the above Asset Bundle Browser tool to see the path of specific assets.

By default when Vivify will check against a checksum when loading an asset bundle, but this checksum check can be disabled by enabling debug mode using the "-aerolunaisthebestmodder" launch parameter. You can add the checksum to the map by using the `"_assetBundle"` field in the info.dat.
```js
  ...
  "_environmentName": "DefaultEnvironment",
  "_allDirectionsEnvironmentName": "GlassDesertEnvironment",
  "_customData": {
    "_assetBundle": 1414251160
  },
  "_difficultyBeatmapSets": [
    {
  ...
```

## Quality Settings
Want realtime reflection probes? Here you go. See https://docs.unity3d.com/ScriptReference/QualitySettings.html for better descriptions of each setting.
```js
  ...
  "_difficulty": "ExpertPlus",
  "_difficultyRank": 9,
  "_beatmapFilename": "ExpertPlusStandard.dat",
  "_noteJumpMovementSpeed": 19,
  "_customData": {
    "_qualitySettings": {
      "_realtimeReflectionProbes": true,
      "_shadows": 2
    }
  }
  ...
```

Currently provided settings:
- `"_anisotropicFiltering"`: (0 - 2) Disable, Enable, ForceEnable.
- `"_antiAliasing"`: (0, 2, 4, 8)
- `"_pixelLightCount"`: (int)
- `"_realtimeReflectionProbes"`: (bool)
- `"_shadowCascades"`: (0, 2, 4)
- `"_shadowDistance"`: (float)
- `"_shadowResolution"`: (0 - 3) Low, Medium, High, VeryHigh.
- `"_shadows"`: (0 - 2) Disable, HardOnly, All.
- `"_softParticles"`: (bool)

## SetMaterialProperty
```js
{
  "b": float, // Time in beats.
  "t": "SetMaterialProperty",
  "d": {
    "asset": string, // File path to the desired material.
    "duration": float, // The length of the event in beats (defaults to 0).
    "easing": string, // An easing for the animation to follow (defaults to easeLinear).
    "properties": [{
      "name": string, // Name of the property on the material.
      "type": string, // Type of the property (Texture, Float, Color).
      "value": ? // What to set the property to, type varies depending on property type.
    }]
  }
}
```

Allows setting material properties, e.g. Texture, Float, Color.

## SetGlobalProperty
```js
{
  "b": float, // Time in beats.
  "t": "SetGlobalProperty",
  "d": {
    "duration": float, // The length of the event in beats (defaults to 0).
    "easing": string, // An easing for the animation to follow (defaults to easeLinear).
    "properties": [{
      "name": string, // Name of the property.
      "type": string, // Type of the property (Texture, Float, Color).
      "value": ? // What to set the property to, type varies depending on property type.
    }]
  }
}
```

Allows setting global properties, e.g. Texture, Float, Color. These will persist even after the map ends, do not rely on their default value.

### Property types
- Texture: Must be a string that is a direct path file to a texture.
- Float: May either be a direct value (`"value": 10.4`) or a point definition (`"value": [[0,0], [10, 1]]`).
- Color: May either be a RGBA array (`"value": [0, 1, 0]`) or a point definition (`"value": [1, 0, 0, 0, 0.2], [0, 0, 1, 0, 0.6]`)
- Vector: May either be an array (`"value": [0, 1, 0]`) or a point definition (`"value": [1, 0, 0, 0, 0.2], [0, 0, 1, 0, 0.6]`)

```js
// Example
{
  "b": 3.0,
  "t": "SetMaterialProperty",
  "d": {
    "asset": "assets/screens/glitchmat.mat",
    "duration": 8,
    "properties": [{
      "name": "_Juice",
      "type": "Float",
      "value": [
        [0.02, 0],
        [0.04, 0.1875, "easeStep"],
        [0.06, 0.375, "easeStep"],
        [0.08, 0.5, "easeStep"],
        [0.1, 0.625, "easeStep"],
        [0.12, 0.75, "easeStep"]
      ]
    }]
  }
}
```

## Blit
```js
{
  "b": float, // Time in beats.
  "t": "Blit",
  "d": {
    "asset": string, // (Optional) File path to the desired material. If missing, will just copy from source to destination without anything special.
    "priority": int, // (Optional) Which order to run current active post processing effects. Higher priority will run first. Default = 0
    "pass": int, // (Optional) Which pass in the shader to use. Will use all passes if not defined.
    "source": string, // (Optional) Which texture to pass to the shader as "_MainTex". "_Main" is reserved for the camera. Default = "_Main"
    "destination": string, // (Optional) Which render texture to save to. Can be an array. "_Main" is reserved for the camera. Default = "_Main"
    "duration": float, // (Optional) How long will this material be applied. Default = 0
    "easing": string, // (Optional) See SetMaterialProperty.
    "properties": ? // (Optional) See SetMaterialProperty.
  }
}
```

Assigns a material to the camera. A duration of 0 will run for exactly one frame. If a destination is the same as source, a temporary render texture will be created as a buffer.

This event allows you to call a [SetMaterialProperty](#SetMaterialProperty) from within.

```js
// Example
{
  "b": 73.0,
  "t": "Blit",
  "d": {
    "asset": "assets/shaders/tvdistortmat.mat",
    "duration": 32,
    "properties": [
      {
        "name": "_Juice",
        "type": "Float",
        "value": 0.2
      }
    ]
  }
}
```

## DeclareCullingTexture
```js
{
  "b": float, // Time in beats.
  "t": "DeclareCullingTexture",
  "d": {
    "name": string // Name of the culling mask, this is what you must name your sampler in your shader.
    "track": string/string[] // Name(s) of your track(s). Everything on the track(s) will be added to this mask.
    "whitelist": bool // (Optional) When true, will cull everything but the selected tracks. Default = false.
    "depthTexture": bool // (Optional) When true, write depth texture to "'name'_Depth". Default = false.
  }
}
```

Declares a culling mask where the selected tracks are culled (or if whitelist = true, only the selected tracks are rendered) of which Vivify will automatically create a texture for you to sample from your shader. If the named field is `_Main` then the culling will apply to the main camera.

```js
// Example
{
  "b": 0.0,
  "t": "DeclareCullingTexture",
  "d": {
    "name": "_NotesCulled",
    "track": "allnotes"
  }
}
```

```csharp
//Example where notes are not rendered on the right side of the screen
sampler2D _NotesCulled;

fixed4 frag(v2f i) : SV_Target
{
  if (i.uv.x > 0.5)
  {
    return tex2D(_NotesCulled, i.uv);
  }
  else {
    return tex2D(_MainTex, i.uv);
  }
}
```

## DeclareRenderTexture
```js
{
  "b": float, // Time in beats.
  "t": "DeclareRenderTexture",
  "d": {
    "name": string, // Name of the texture
    "xRatio": float, // (Optional) Number to divide width by, i.e. on a 1920x1080 screen, an xRatio of 2 will give you a 960x1080 texture.
    "yRatio": float, // (Optional) Number to divide height by.
    "width": int, // (Optional) Exact width for the texture.
    "height": int, // (Optional) Exact height for the texture.
    "colorFormat": string, // (Optional) https://docs.unity3d.com/ScriptReference/RenderTextureFormat.html
    "filterMode": string // (Optional) https://docs.unity3d.com/ScriptReference/FilterMode.html
  }
}
```

Declares a RenderTexture to be used anywhere. They are set as a global variable and can be accessed by declaring a sampler named what you put in "name".

```js
// Example
// Here we declare a texture called "snapshot", capture a single frame at 78.0, then store it in our new render texture.
// Lastly we destroy the texture (See below) after we are done with it to free up any memory it was taking.
// (Realistically, won't provide noticable boost to performance, but it can't hurt.)
{
  "b": 70.0,
  "t": "DeclareRenderTexture",
  "d": {
    "name": "snapshot"
  }
},
{
  "b": 78.0,
  "t": "Blit",
  "d": {
    "destination": "snapshot"
  }
},
{
  "b": 120.0,
  "t": "DestroyTexture",
  "d": {
    "name": "snapshot"
  }
}
```

## DestroyTexture
```js
{
  "b": float, // Time in beats.
  "t": "DestroyTexture",
  "d": {
    "name": string or string[], // Names(s) of textures to destroy.
  }
}
```

Destroys a texture. It is important to destroy any textures created through `DeclareCullingTexture` because the scene will have to be rendered again for each active culling texture. This can also be used for textures created through `DeclareRenderTexture` to free up memory.

## InstantiatePrefab
```js
{
  "b": float, // Time in beats.
  "t": "InstantiatePrefab",
  "d": {
    "asset": string, // File path to the desired prefab.
    "id": string, // (Optional) Unique id for referencing prefab later. Random id will be given by default.
    "track": string, // (Optional) Track to animate prefab transform.
    "position": vector3, // (Optional) Set position.
    "localPosition": vector3, // (Optional) Set localPosition.
    "rotation": vector3, // (Optional) Set rotation (in euler angles).
    "localRotation": vector3. // (Optional) Set localRotation (in euler angles).
    "scale": vector3 //(Optional) Set scale.
  }
}
```
Instantiates a prefab in the scene. If left-handed option is enabled, then the position, rotation, and scale will be mirrored.

## DestroyPrefab
```js
{
  "b": float, // Time in beats.
  "t": "DestroyPrefab",
  "d": {
    "id": string or string[], // Id(s) of prefab to destroy.
  }
}
```
Destroys a prefab in the scene.

## SetAnimatorProperty
```js
{
  "b": float, // Time in beats.
  "t": "SetAnimatorProperty",
  "d": {
    "id": string, // Id assigned to prefab.
    "duration": float, // (Optional) The length of the event in beats. Defaults to 0.
    "easing": string, // (Optional) An easing for the animation to follow. Defaults to "easeLinear".
    "properties": [{
      "name": string, // Name of the property.
      "type": string, // Type of the property (Bool, Float, Trigger).
      "value": ? // What to set the property to, type varies depending on property type.
    }]
  }
}
```

Allows setting animator properties. This will search the prefab for all Animator components.

### Property types
- Bool: May either be a direct value (`"value": true`) or a point definition (`"value": [[0,0], [1, 1]]`). Any value greater than or equal to 1 is true.
- Float: May either be a direct value (`"value": 10.4`) or a point definition (`"value": [[0,0], [10, 1]]`).
- Integer: May either be a direct value (`"value": 10`) or a point definition (`"value": [[0,0], [10, 1]]`). Value will be rounded.
- Trigger: Must be `true` to set trigger or `false` to reset trigger.

## SetCameraProperty
```js
{
  "b": float, // Time in beats.
  "t": "SetCameraProperty",
  "d": {
    "depthTextureMode": [] // Sets the depth texture mode on the camera. Can be [Depth, DepthNormals, MotionVectors].
  }
}
```

Remember to clear the `depthTextureMode` to `[]` after you are done using it as rendering a depth texture can impact performance. See https://docs.unity3d.com/Manual/SL-CameraDepthTexture.html for more info. Note: if the player has the Smoke option enabled, the `depthTextureMode` will always have `Depth`.

## AssignTrackPrefab
```js
{
  "b": float, // Time in beats.
  "t": "AssignTrackPrefab",
  "d": {
    "track": string, // Only objects on this track will be affected.
    "note": string // File path to the desired prefab to replace notes.
  }
}
```

Replaces all objects on the track with the assigned prefab.
