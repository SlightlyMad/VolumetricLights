# Volumetric Lights for Unity 5
[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/JPxLCYXB-8A/0.jpg)](https://www.youtube.com/watch?v=JPxLCYXB-8A)

Open source (FreeBSD) extension for built-in Unity lights. It uses ray marching in light's volume to compute volumetric fog. Light scattering is not physically correct. It is approximated by light's attenuation. This technique is similar to the one used in Killzone ([GPU Pro 5](http://www.amazon.com/GPU-Pro-Advanced-Rendering-Techniques/dp/1482208636): Volumetric Light Effects in Killzone Shadow Fall by Nathan Vos)

Corresponding thread in Unity Forum can be found [here](http://forum.unity3d.com/threads/true-volumetric-lights-open-source-soon.390818/).

### Demo Project
I developed this technology for my hobby project. It was never meant for real use and it is therefore little rough around the edges.

You can see the demo on [Youtube](https://www.youtube.com/watch?v=JPxLCYXB-8A).
Or you can try it for yourself: [Binary download](https://onedrive.live.com/redir?resid=D65A46249BFF9056!40295&authkey=!AAK3X7BJ_nr3IhE&ithint=file%2czip)
### Features
* Volumetric fog effect for multiple lights. Point and spot lights are fully supported. Directional light support is partial/experimental.
* Volumetric shadows
* Volumetric light cookies
* Volumetric noise implemented as animated 3D texture.

### Usage
* Add VolumetricLightRenderer script to your camera and set default cookie texture for spot light. Camera has to use HDR+Deferred combo.
* Add VolumetricLight script to every light that you want to have volumetric fog.

Volumetric lights will respect standard light's parameters like color, intensity, range, shadows and cookie. There are also additional parameters specific for volumetric lights. For example:
* Sample count - number of raymarching samples (trade quality for performance)
* Noise - enable volumetric noise
* Noise Scale - noise scale
* Noise Speed - noise animation speed
* MieG - parameter that controls mie scattering (controls how light is reflected with respect to light's direction)

Sample scene called "example" is part of this project.

### Rendering resolution
Volumetric fog can be rendered in smaller resolution as an optimization. Set rendering resolution in VolumetricLightRenderer script.
* Full resolution - best quality, poor performance. Serves as a "ground truth".
* Half resolution - best quality/performance ratio.
* Quarter resolution - experimental. Worse quality. No real performance gain for one or two lights. Try to use it when you have many lights.

### Known Limitations
* Currently requires HDR camera and deferred renderer
* Currently requires DirectX 11
* Doesn't handle transparent geometry correctly (cutout is ok)
* 3d noise texture is hard coded. VolumetricLightRenderer has custom dds file loader that loads one specific 3d texture (Unity doesn't support 3d textures loaded from file). File "NoiseVolume.bytes" has to be in Resources folder.
* Directional light isn't fully supported
* Shadow fading and shadow strength currently isn't supported
* Parameters and thresholds for bilateral blur and upscale are hard-coded in shaders. 

### Technique overview
* Create render target for volumetric light (volume light buffer)
* Use CameraEvent.BeforeLighting to clear volume light buffer
* For every volumetric light render light's volume geometry (sphere, cone, full-screen quad)
  * Use LightEvent.AfterShadowMap for shadow casting lights​
  * Use CameraEvent.BeforeLighting for non-shadow casting lights​
  * Perform raymarching in light's volume​
    * Dithering is used to offset ray origin for neighbouring pixels​
* Use CameraEvent.AfterLighting to perform depth aware gaussian blur on volume light buffer
* Add volume light buffer to Unity's built-in light buffer

### Possible improvements
* Light's volume geometry is unnecessarily high poly
* Ray marching is performed in view space and every sample is then transformed to shadow space. It would be better to perform ray marching in both spaces simultaneously.
* Add temporal filter to improve image quality and stability in motion. Change sampling pattern every frame to get more ray marching steps at no additional cost (works only with temporal filter)
* Bilateral blur and upscale can be improved
* I didn't try to optimize the shaders, there is likely room for improvement.
