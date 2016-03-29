# Volumetric Lights for Unity 5
[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/JPxLCYXB-8A/0.jpg)](https://www.youtube.com/watch?v=JPxLCYXB-8A)

Open source (FreeBSD) extension for builtin Unity lights. It uses ray marching in light's volume to compute volumetric fog. Light scattering is not physically correct. Scattering is approximated by light's attenuation. This technique is similar to the one used in Killzone ([GPU Pro 5](http://www.amazon.com/GPU-Pro-Advanced-Rendering-Techniques/dp/1482208636): Volumetric Light Effects in Killzone Shadow Fall by Nathan Vos)

[Unity Community Thread](http://forum.unity3d.com/threads/true-volumetric-lights-open-source-soon.390818/)

### Demo Project
I developed this technology for my hobby project. It was never meant for production and it is therefore little rough around the edges.

[Youtube](https://www.youtube.com/watch?v=JPxLCYXB-8A)

[Binary download](https://onedrive.live.com/redir?resid=D65A46249BFF9056!40295&authkey=!AAK3X7BJ_nr3IhE&ithint=file%2czip)
### Features
* Volumetric fog effect for multiple lights. Point and spot lights are fully supported. Directional light support is partial/experimental.
* Volumetric shadows
* Volumetric light cookies
* Volumetric noise implemented as animated 3D texture.

### Usage
* Add VolumetricLightRenderer script to your camera and set default cookie texture for spot light. Camera has to use HDR+Deferred combo.
* Add VolumetricLight script to every light that you want to have volumetric fog.

Volumetric lights will respect standard light's parameters like color, intensity, range, shadows and cookie. There are also additional parameters specific for volumetric lights:
* Sample count - number of raymarching samples (trade quality for performance)
* Noise - enable volumetric noise
* Noise Scale - noise scale
* Noise Speed - noise animation speed
* MieG - parameter that controls mie scattering (controls how light is reflected with respect to light's direction)

### Rendering resolution
Volumetric fog can be rendered in smaller resolution as an optimization. Set rendering resolution in VolumetricLightRenderer script.
* Full resolution - best quality, poor performance. Serves as a "ground truth".
* Half resolution - best quality/performance ratio.
* Quarter resolution - experimental. Worse quality. No real performance gain for one or twe lights. Try to use when you have many lights.

### Known Limitations
* Currenty requires HDR camera and deferred renderer
