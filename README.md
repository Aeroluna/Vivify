# Vivify
Bring your map to life.

#### If you use any of these features, you MUST add "Vivify" as a requirement for your map for them to function, you can go [Here](https://github.com/Kylemc1413/SongCore/blob/master/README.md) to see how adding suggestions/requirements to the info.dat works. Also, Vivify will only load on v3 maps.

This documentation assumes basic understanding of custom events and tracks.

## Event Types
- [`SetMaterialProperty`](#SetMaterialProperty)
- [`SetGlobalProperty`](#SetGlobalProperty)
- [`ApplyPostProcessing`](#ApplyPostProcessing)
- [`DeclareCullingMask`](#DeclareCullingMask)
- [`DestroyTexture`](#DestroyTexture)
- [`DeclareRenderTexture`](#DeclareRenderTexture)
- [`InstantiatePrefab`](#InstantiatePrefab)
- [`DestroyPrefab`](#DestroyPrefab)
- [`SetAnimatorProperty`](#SetAnimatorProperty)
- [`SetCameraProperty`](#SetCameraProperty)

# Setting up Unity
First, you should to download the Unity Hub at https://unity3d.com/get-unity/download and follow the on-screen instructions until you get to the projects page, while skipping the recommended Unity Editor. Install a version of Unity (Installs > Install Editor) and for maximum compatibility, Beat Saber uses version 2019.4.28f1 found in the [archive](https://unity3d.com/get-unity/download/archive).

Now, you can create your own Unity project or use a specially made template for Vivify (coming soon).

If you're using screen space shaders, make sure you have `Virtual Reality Supported` enabled in your project and your stereo rendering mode is set to `Single Pass`. (Edit > Project Settings > Player > XR Settings > Deprecated Settings > Virtual Reality Supported) If you're using Oculus, select the `Oculus` SDK. If you have anything else, use the `OpenVR` SDK.

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

# Writing screen space shaders

There a few quirks to take note of when writing screen space shaders.

### Stereo UV

> Dev note: add fucked screenshot for examples.

Beat Saber uses Single Pass Stereo renderering (See https://docs.unity3d.com/Manual/SinglePassStereoRendering.html for more info). Use the unity built-in function `UnityStereoScreenSpaceUVAdjust` to fix your shaders in vr.
```csharp
sampler2D _MainTex;
half4 _MainTex_ST;

fixed4 frag (v2f i) : SV_Target
{
  return tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
}
```

# SetMaterialProperty
Allows setting material properties, e.g. Texture, Float, Color.

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

# SetGlobalProperty
Allows setting global properties, e.g. Texture, Float, Color. These will persist even after the map ends, do not rely on their default value.

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

## Property types
- Texture: Must be a string that is a direct path file to a texture.
- Float: May either be a direct value (`"value": 10.4`) or a point definition (`"value": [[0,0], [10, 1]]`).
- Color: May either be a RGBA array (`"value": [0, 1, 0]`) or a point definition (`"value": [1, 0, 0, 0, 0.2], [0, 0, 1, 0, 0.6]`)

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

# ApplyPostProcessing
```js
{
  "b": float, // Time in beats.
  "t": "ApplyPostProcessing",
  "d": {
    "asset": string, // File path to the desired material.
    "priority": int, // (Optional) Which order to run current active post processing effects. Higher priority will run first. Default = 0
    "pass": int, // (Optional) Which pass in the shader to use. Will use all passes if not defined.
    "source": string, // (Optional) Which texture to pass to the shader as "_MainTex". "_Main" is reserved for the camera. Default = "_Main"
    "destination": string[], // (Optional) Which render texture to save to. "_Main" is reserved for the camera. Default = "_Main"
    "duration": float, // (Optional) How long will this material be applied. Default = 0
    "easing": string, // See SetMaterialProperty.
    "properties": ? // See SetMaterialProperty.
  }
}
```
`ApplyPostProcessing` will assign a material to the camera. A duration of 0 will run for exactly one frame.

This event allows you to call a [SetMaterialProperty](#SetMaterialProperty) from within.

```js
// Example
{
  "b": 73.0,
  "t": "ApplyPostProcessing",
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

# DeclareCullingMask
```js
{
  "b": float, // Time in beats.
  "t": "DeclareCullingMask",
  "d": {
    "name": string // Name of the culling mask, this is what you must name your sampler in your shader.
    "track": string/string[] // Name(s) of your track(s). Everything on the track(s) will be added to this mask.
    "whitelist": bool // (Optional) When true, will cull everything but the selected tracks. Default = false.
    "depthTexture": bool // (Optional) When true, write depth texture to "'name'_Depth". Default = false.
  }
}
```

This declares a culling mask where the selected tracks are culled (or if whitelist = true, only the selected tracks are rendered) of which vivify will automatically create a texture for you to sample from your shader.

```js
// Example
{
  "b": 0.0,
  "t": "DeclareCullingMask",
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

# DestroyTexture
```js
{
  "b": float, // Time in beats.
  "t": "DestroyTexture",
  "d": {
    "name": string or string[], // Names(s) of textures to destroy.
  }
}
```
`DestroyTexture` will destroy a texture. It is important to destroy any textures created through `DeclareCullingMask` because the scene will have to be rendered again for each active culling mask.

# DeclareRenderTexture
```js
{
  "b": float, // Time in beats.
  "t": "DeclareRenderTexture",
  "d": {
    "name": string, // Name of the depth texture
    "xRatio": float, // (Optional) Number to divide width by, i.e. on a 1920x1080 screen, an xRatio of 2 will give you a 960x1080 texture
    "yRatio": float, // (Optional) Number to divide height by
    "width": int, // (Optional) Exact width for the texture.
    "height": int, // (Optional) Exact height for the texture.
    "colorFormat": string, // (Optional) https://docs.unity3d.com/ScriptReference/RenderTextureFormat.html
    "filterMode": string // (Optional) https://docs.unity3d.com/ScriptReference/FilterMode.html
  }
}
```
`DeclareRenderTexture` declare a RenderTexture to be used anywhere. They are set as a global variable and can be accessed by declaring a sampler named what you put in "name".

# InstantiatePrefab
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
`InstantiatePrefab` will instantiate your prefab in the scene.

# DestroyPrefab
```js
{
  "b": float, // Time in beats.
  "t": "DestroyPrefab",
  "d": {
    "id": string or string[], // Id(s) of prefab to destroy.
  }
}
```
`DestroyPrefab` will destroy a your prefab in the scene.

# SetAnimatorProperty
Allows setting animator properties. This will search the prefab for all Animator components.

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

## Property types
- Bool: May either be a direct value (`"value": true`) or a point definition (`"value": [[0,0], [1, 1]]`). Any value greater than or equal to 1 is true.
- Float: May either be a direct value (`"value": 10.4`) or a point definition (`"value": [[0,0], [10, 1]]`).
- Integer: May either be a direct value (`"value": 10`) or a point definition (`"value": [[0,0], [10, 1]]`). Value will be rounded.
- Trigger: Must be `true` to set trigger or `false` to reset trigger.

# SetCameraProperty
Allows setting animator properties. This will search the prefab for all Animator components.

```js
{
  "b": float, // Time in beats.
  "t": "SetCameraProperty",
  "d": {
    "depthTextureMode": [] // Sets the depth texture mode on the camera. Can be [Depth, DepthNormals, MotionVectors].
  }
}
```

Rememeber to clear the `depthTextureMode` to `[]` after you are done using it as rendering a depth texture can impact performance. See https://docs.unity3d.com/Manual/SL-CameraDepthTexture.html for more info. Note: if the player has the Smoke option enabled, the `depthTextureMode` will always have `Depth`.