# Volumetric Lights for Unity 5
[![IMAGE ALT TEXT HERE](https://bqu2ya-dm2305.files.1drv.com/y3mSxIn4D7Zx9td_2NWn3yZu6024l3GC9-8fKVrnivLnjVDxyiZ9SgNVjycxd1AX_bjaVZnWVqwu0TonTrdLqbi1RrIX_8eiTQx_7u38GzTOYj9zHKnEZ2Fz97TykfO2OAdQv_nndYI0_lwAAWwFwO08pnGCRgzjOYEBCAStMnf9UQ?width=1167&height=653&cropmode=none)](https://www.youtube.com/watch?v=JPxLCYXB-8A) [![IMAGE ALT TEXT HERE](https://agu0ya-dm2305.files.1drv.com/y3mnqQ4pzhZdF4k3Z7Fv_QApkSe2XWNtBUwVwrinyrbIuJt6Stv3XubFLqom7tLWehG9MCapT3z6njzfQeZbobiilFRe_2qJcE8f0gpENxg3_ccxGOMjV4Zi3GcwKhaf1iVdpq1d9p4I9QhflIlj2TdlEWWNcaklBpPJ8A5IZmCtcs?width=1167&height=650&cropmode=none)](https://www.youtube.com/watch?v=ElaPJyzR504)

Open source (BSD) extension for built-in Unity lights. It uses ray marching in light's volume to compute volumetric fog. This technique is similar to the one used in Killzone ([GPU Pro 5](http://www.amazon.com/GPU-Pro-Advanced-Rendering-Techniques/dp/1482208636): Volumetric Light Effects in Killzone Shadow Fall by Nathan Vos)

Corresponding thread in Unity Forum can be found [here](http://forum.unity3d.com/threads/true-volumetric-lights-open-source-soon.390818/).

### Demo Project
I developed this technology for my hobby project. It was never meant for real use and it is therefore little rough around the edges.

You can see the demo on [Youtube](https://www.youtube.com/watch?v=JPxLCYXB-8A).
Or you can try it for yourself: [Binary download](https://onedrive.live.com/redir?resid=D65A46249BFF9056!40295&authkey=!AAK3X7BJ_nr3IhE&ithint=file%2czip)
### Features
* Volumetric fog effect for multiple lights. Point, spot  and directional lights are fully supported.
* Volumetric shadows
* Volumetric light cookies
* Volumetric noise implemented as animated 3D texture.

### Usage
* Add VolumetricLightRenderer script to your camera and set default cookie texture for spot light.
* Add VolumetricLight script to every light that you want to have volumetric fog.

Volumetric lights will respect standard light's parameters like color, intensity, range, shadows and cookie. There are also additional parameters specific for volumetric lights. For example:
* Sample count - number of raymarching samples (trade quality for performance)
* Scattering Coef - scattering coefficient controls amount of light that is reflected towards camera. Makes the effect stronger.
* Extinction Coef - controls amount of light absorbed or out-scattered with distance. Makes the effect weaker with increasing distance. It also attenuates existing scene color when used with directional lights.
* Skybox Extinction Coef - Only affects directional light. It controls how much skybox is affected by Extinction coefficient. This technique ignores small air particles and decreasing particle density with altitude. That often makes skybox too "foggy". Skybox extinction coefficient can help with it.
* MieG - controls mie scattering (controls how light is reflected with respect to light's direction)
* Height Fog - toggle exponential height fog
* Height Fog Scale - scale height fog
* Noise - enable volumetric noise
* Noise Scale - noise scale
* Noise Speed - noise animation speed

Several sample scenes are part of this project.

#### Building a standalone player
**_IMPORTANT_** - Standalone player will work only if all shaders are included in build! All shaders must be added into "Always Included Shaders" in ProjectSettings/Graphics.

#### Example scene with different parameters
* Low/High Scattering and Extinction parameters
![alt tag](https://agu1ya-dm2305.files.1drv.com/y3mgo5ud5huq-SUjw4z8gGjB9JBoWBhIerh46Oh18e6GVoy7lR6vffSZeK50e7FnTINV04B20jmSGiyRrodTTVgYGkZ00goIWjvKMaxMQS9eygkKSKansmWCHR0lzJ-v0Rag8-_h4-iJZjD304lRqSmgHT7KAZpNJIeRnihNJ4Y03k?width=2338&height=650&cropmode=none)
* Low/High Mie G parameter
![alt tag](https://agupya-dm2305.files.1drv.com/y3mobk9viWO3q53gQlSGj7Libj929UoUR4WlQBu5aY5K57aBg3dly30hnHi7sLZwO1_OTPqvXb9Ifa3L46HFuuNZ_AEMijAK3hFJagQ-uxUHUCI5E5fSVFIpYGgeF_yS7AQwi-sloHyxFwdg4vD46aujZDYxlRCFG006cU-f_y4BnE?width=2338&height=650&cropmode=none)
* High/Low Shadow Strength. This technique uses single scattering model. That makes areas in shadow unnaturally dark. Use Shadow Strength to "simulate" multiple scattering.
![alt tag](https://aguqya-dm2305.files.1drv.com/y3mQZxpA5UbrBUPIMo44IfWChUXznaOaxBLbyqcCL-SN2y_o7mz8CJudvjWR_PdfwqPJ2i7VXIqO917oUBGhYkj-KS1pFXPHJk-GYQ_HPLmWKtDvwcqKDKPYwJy3JzQQE9IP38OIA6yQViA4olICpZkxmPpqhmYZbHemwdc5vEyz0s?width=2338&height=650&cropmode=none)
* Height fog with different scale
![alt tag](https://bqu4ya-dm2305.files.1drv.com/y3mmmE_KTmAE9MRLeoYFM3wkPDDjcUJyDA89Z1yorEND1GWkp4pW3Xo6iEdSQa0r8Ciz1hXT6XFufQQGO1id3vfybIgX2vw9hhJpwLGm4SMQ33CKdtnVkTTct_tYN0tW_g5cfXyNxFBEPshRUuP_-idZf5Hg4qaU5zrc8m6kIeDbZg?width=2338&height=650&cropmode=none)
* High/Low Skybox Extinction parameter
![alt tag](https://bqu3ya-dm2305.files.1drv.com/y3mxWdkqeJLzQKmg9OI8Xd2P0cZ1YAs6g5n0CihQLpcyfigapIjFUTYvVo4-vikHtMfMwyOoQMsprqydKNaDgEHwckqNx6ATTqcuNEanUQ9256-D4l3iSConnDO12nk9y_lIm_Ztu7Fuib7G7mW1rgF4Rc5tJOiwLIN7l08_4fTZAc?width=2338&height=650&cropmode=none)

### Rendering resolution
Volumetric fog can be rendered in smaller resolution as an optimization. Set rendering resolution in VolumetricLightRenderer script.
* Full resolution - best quality, poor performance. Serves as a "ground truth".
* Half resolution - best quality/performance ratio.
* Quarter resolution - experimental. Worse quality, best performance.

### Requirements
* Unity 5 (tested on 5.3.4, 5.4 and 5.5)
* DirectX 10/11 or OpenGL 4.1 and above
* Tested on Windows and Mac but it should work on other platforms as well
* VR isn't supported
* Mobile devices aren't supported

### Known Limitations
* ~~Currently requires HDR camera and deferred renderer~~
* ~~Currently requires DirectX 11~~
* Doesn't handle transparent geometry correctly (cutout is ok)
* 3d noise texture is hard coded. VolumetricLightRenderer has custom dds file loader that loads one specific 3d texture (Unity doesn't support 3d textures loaded from file). File "NoiseVolume.bytes" has to be in Resources folder.
* Shadow fading is not implemented
* Parameters and thresholds for bilateral blur and upscale are hard-coded in shaders. Different projects may need different constants. Look into BilateralBlur.shader. There are several constants at the beginning that control downsampling, blur and upsampling. 

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
 
### Donations
I've been asked about donation button several times now. I don't feel very comfortable about it. I don't need the money. I have a well paid job. It could also give the impression that I would use the money for further development. But that is certainly not the case. 

But if you really like it and you want to brighten my day then you can always buy me a little gift. Send me [Amazon](https://www.amazon.com/Amazon-Amazon-com-eGift-Cards/dp/BT00DC6QU4) or [Steam](https://www.paypal-gifts.com/uk/brands/steam-digital-wallet-code.html) gift card and I'll buy myself something shiny. You can find my email in my profile. 
