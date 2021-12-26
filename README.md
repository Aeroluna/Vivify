# Vivify
Bring your map to life.

#### If you use any of these features, you MUST add "Vivify" as a requirement for your map for them to function, you can go [Here](https://github.com/Kylemc1413/SongCore/blob/master/README.md) to see how adding suggestions/requirements to the info.dat works

This documentation assumes basic understanding of custom events and tracks.

## Event Types
- [`SetMaterialProperty`](#SetMaterialProperty)
- [`ApplyPostProcessing`](#ApplyPostProcessing)
- [`InstantiatePrefab`](#InstantiatePrefab)
- [`DeclareMask`](#DeclareMask)
- [`DeclareCullingMask`](#DeclareCullingMask)

# Creating an asset bundle
Firstly, make sure you have a Unity install (for maximum compatibility, Beat Saber uses version 2019.4.28) and create a project.

Visit https://learn.unity.com/tutorial/introduction-to-asset-bundles for a basic introduction to creating asset bundles.

(Optional) See https://docs.unity3d.com/Manual/AssetBundles-Browser.html. this tool allows you to browse the contents of a built asset bundle.

When building shaders, make sure you have `Virtual Reality Supported` enabled in your project and your stereo rendering mode is set to `Single Pass`. (Edit > Project Settings > Player > XR Settings > Deprecated Settings > Virtual Reality Supported)

After creating your asset bundle, place it in your song folder and call it `bundle` (no extension).
```
song folder
├── bundle
├── song.ogg
├── cover.jpg
├── ExpertPlusStandard.dat
└── info.dat
```

When referencing an asset's file path in an event, remember to write in all lower case.

# SetMaterialProperty
Allows setting material properties, e.g. Texture, Float, Color.

Here is an example of one being defined to animate [`_position`](#_position): (See: [AnimateTrack](#AnimateTrack))
```js
{
  "_time": float, // Time in beats.
  "_type": "SetMaterialProperty",
  "_data": {
    "_asset": string, // File path to the desired material.
    "_duration": float, // The length of the event in beats (defaults to 0).
    "_easing": string, // An easing for the animation to follow (defaults to easeLinear).
    "_properties": [{
      "_name": string, // Name of the property on the material.
      "_type": string, // Type of the property (Texture, Float, Color).
      "_value": ? // What to set the property to, type varies depending on property type.
    }]
  }
}
```
## Property types.
- Texture: Must be a string that is a direct path file to a texture.
- Float: May either be a direct value (`"_value": 10.4`) or a point definition (`"_value": [[0,0], [10, 1]]`).
- Color: May either be a RGBA array (`"_value": [0, 1, 0]`) or a point definition (`"_value": [1, 0, 0, 0, 0.2], [0, 0, 1, 0, 0.6]`)

```js
// Example
{
  "_time": 3.0,
  "_type": "SetMaterialProperty",
  "_data": {
    "_asset": "assets/screens/glitchmat.mat",
    "_duration": 8,
    "_properties": [{
      "_name": "_Juice",
      "_type": "Float",
      "_value": [
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
  "_time": float, // Time in beats.
  "_type": "ApplyPostProcessing",
  "_data": {
    "_asset": string, // File path to the desired material.
    "_pass": int, // Target pass (0 - 3), camera will render greatest targeted pass (defaults to 0).
    "_duration": float, // How long will this material be applied.
    "_easing": string, // See SetMaterialProperty.
    "_properties": ? // See SetMaterialProperty.
  }
}
```
`ApplyPostProcessing` will assign a material to the camera.

This event allows you to call a [SetMaterialProperty](#SetMaterialProperty) from within.

```js
// Example
{
  "_time": 73.0,
  "_type": "ApplyPostProcessing",
  "_data": {
    "_asset": "assets/shaders/tvdistortmat.mat",
    "_duration": 32,
    "_properties": [{
      "_name": "_Juice",
      "_type": "Float",
      "_value": 0.2
    }
  }
}
```

Note: Camera depth mode is always set to `DepthTextureMode.Depth` and can be grabbed through by declaring a sampler called `_CameraDepthTexture` (See https://docs.unity3d.com/Manual/SL-CameraDepthTexture.html for more info.)

## Writing shaders

There a few quirks to take note of when writing post processing shaders.

### Stereo UV

Dev note: add fucked screenshot for examples.

Beat Saber uses Single Pass Stereo renderering (See https://docs.unity3d.com/Manual/SinglePassStereoRendering.html for more info). Use the unity built-in function `UnityStereoScreenSpaceUVAdjust` to fix your shaders in vr.
```csharp
sampler2D _MainTex;
half4 _MainTex_ST;

fixed4 frag (v2f i) : SV_Target
{
  return tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
}
```

### Platform differences

Dev note: FIGURE THIS SHIT OUT WHY DOES IT DO THIS AAAAAAAAAAAAAAAA

If you are on a Direct3D-like platform (Which windows is) then you will have flipped UV in your shader. (See https://docs.unity3d.com/Manual/SL-PlatformDifferences.html for more info)

```csharp
v2f vert (appdata v)
{
  v2f o;
  o.vertex = UnityObjectToClipPos(v.vertex);
  o.uv = v.uv;

  // Add this to your vertex shader.
  // _ProjectionParams.x is -1 if the image is incorrectly flipped.
  if (_ProjectionParams.x < 0)
    o.uv.y = 1 - o.uv.y;

  return o;
}
```

## Passes

Dev note: this method of global texture is subject to change.

This event can target a specific "pass" to render to. After, this "pass" can be accessed from any Shader by the global texture `_Pass*` (0 - 3).
```csharp
// you do not declare these as a property!
sampler2D _Pass0;

fixed4 frag (v2f i) : SV_Target
{
    return tex2D(_Pass, i.uv);
}
```
For even more fun, you can sample whatever that pass rendered last frame by appending `_Previous`.
```csharp
sampler2D _Pass0_Previous;
```

# DeclareMask
```js
{
  "_time": float, // Time in beats.
  "_type": "DeclareMask",
  "_data": {
    "_name": string // Name of the mask, this is what you must name your sampler in your shader.
    "_track": string/string[] // Name(s) of your track(s). Everything on the track(s) will be added to this mask.
  }
}
```

Dev note: this method of declaring a mask is subject to change

This declares a mask of which vivify will automatically create a texture for you to sample from your shader.
Note: The resulting texture is a DEPTH TEXTURE.

```js
// Example
{
  "_time": 0.0,
  "_type": "DeclareMask",
  "_data": {
    "_name": "_NoteMask",
    "_track": "allnotes"
  }
}
```

```csharp
//Example for only getting mask thats not occluded
sampler2D _NoteMask;

fixed4 frag (v2f i) : SV_Target
{
  float camdepth = tex2D(_CameraDepthTexture, i.uv).r;
  camdepth = Linear01Depth(camdepth);

  float notedepth = tex2D(_NoteMask, i.uv).r;
  notedepth = Linear01Depth(notedepth);

  // 1 is the far clipping plane of the camera.
  if (notedepth < 1) {
    // We get the difference between the camera's depth texture and our note's depth texture
    float diff = saturate(notedepth - camdepth);
    // if there is no difference, we know there is nothing occluding the note.
    if (diff < 0.001)
      return float4(0, 1, 0, 1);
  }
  return tex2D(_NoteMask, i.uv);
}
```

# DeclareCullingMask
```js
{
  "_time": float, // Time in beats.
  "_type": "DeclareCullingMask",
  "_data": {
    "_name": string // Name of the culling mask, this is what you must name your sampler in your shader.
    "_track": string/string[] // Name(s) of your track(s). Everything on the track(s) will be added to this mask.
    "_whitelist": bool // Optional. When true, will cull everything but the selected tracks. By default is false.
  }
}
```

Dev note: this method of declaring a culling mask is subject to change. also need to find some way to return depth

This declares a culling mask where the selected tracks are culled (or if whitelist = true, only the selected tracks are rendererd) of which vivify will automatically create a texture for you to sample from your shader.

```js
// Example
{
  "_time": 0.0,
  "_type": "DeclareCullingMask",
  "_data": {
    "_name": "_NotesCulled",
    "_track": "allnotes"
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
    // Note: you dont need to flip the uv for the culling texture
    // Dev note: DONT ASK ME WHY CAUSE I DONT FUCKING KNOW AND I WISH I KNEW AND I WILL PROBABLY FIX THIS SO GET READY TO UPDATE ALL YOUR SHADERS SJDFKASHDF
    return tex2D(_NotesCulled, i.uv);
  }
  else {
    if (_ProjectionParams.x < 0)
      i.uv.y = 1 - i.uv.y;

    return tex2D(_MainTex, i.uv);
  }
}
```


# InstantiatePrefab (WIP)
```js
{
  "_time": float, // Time in beats.
  "_type": "InstantiatePrefab",
  "_data": {
    "_asset": string, // File path to the desired prefab.
    "_position": vector3, // Optionally, set position.
    //"_localPosition": vector3, // Optionally, set localPosition.
    "_rotation": vector3, // Optionally, set rotation (in euler angles).
    //"_localRotation": vector3. // Optionally, set localRotation (in euler angles).
    "_scale": vector3 // Optionally, set scale.
  }
}
```
`InstantiatePrefab` will instantiate a your prefab in the scene.