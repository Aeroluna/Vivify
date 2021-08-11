# Vivify
Bring your map to life.

#### If you use any of these features, you MUST add "Vivify" as a requirement for your map for them to function, you can go [Here](https://github.com/Kylemc1413/SongCore/blob/master/README.md) to see how adding suggestions/requirements to the info.dat works

This documentation assumes basic understanding of custom events and tracks.

## Event Types
- [`SetMaterialProperty`](#SetMaterialProperty)
- [`ApplyPostProcessing`](#ApplyPostProcessing)
- [`InstantiatePrefab`](#InstantiatePrefab)

# Creating an asset bundle
Firstly, make sure you have a Unity install (for maximum compatibility, Beat Saber uses version 2019.4.18f1) and create a project.

Visit https://learn.unity.com/tutorial/introduction-to-asset-bundles for a basic introduction to creating asset bundles.

(Optional) See https://docs.unity3d.com/Manual/AssetBundles-Browser.html. this tool allows you to browse the contents of a built asset bundle.

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
- Texture: Must be a string, can either be "Pass*" (See: [ApplyPostProcessing](#ApplyPostProcessing)) or a direct path file to a texture.
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

## Passes
This event can target a specific "pass" to render to. After, this "pass" can be accessed from any Shader by the global texture `_Pass*` (0 - 3).
```csharp
sampler2D _Pass0;

fixed4 frag (v2f i) : SV_Target
{
    return tex2D(_Pass, i.uv);
}
```
For even more fun, you can sample whatever that pass rendered last frame by appending `_Previous`.
```
sampler2D _Pass0_Previous;
```

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
      "_value": [[0.2, 0]]
    }
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
    "_localPosition": vector3, // Optionally, set localPosition.
    "_rotation": vector3, // Optionally, set rotation (in euler angles).
    "_localRotation": vector3. // Optionally, set localRotation (in euler angles).
    "_scale": vector3 // Optionally, set scale.
  }
}
```
`InstantiatePrefab` will instantiate a your prefab in the scene.