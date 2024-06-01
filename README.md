# XR phosphene simulations with Unity game engine.

## About
This repository contains a collection of Unity-based phosphene simulations in mixed reality (XR) across different platforms. Note that this code is mainly intended for simple demonstrations of our visual prosthetics research. The simulations in this repository are relatively basic and are missing some of the biologically realistic features that were developed in other projects [on cortical stimulation parameters](https://github.com/neuralcodinglab/dynaphos) or [eye movements](https://github.com/neuralcodinglab/SPVGaze). 

## Contents
This repository contains both some compiled applications, as well as the Unity source code, to serve different needs.

### Build application
If you just want to use the application without adjustments, you can use the compiled applications in the `build` directory. This means that you don't have to install Unity or do any coding. 
- The most basic android application can be found [here](build/Android/demo_NEMO_v2021_july.apk). The installation requires downloading the `.apk` file and granting your phone all permissions to install third-party applications. The application is best viewed using phose-based VR-goggles (e.g., cartboard glasses). 
- There is also a Windows version which uses a webcam for the simulations. (Useful for presentations). You can run the desktop application by cloning or downloading this repository and executing the `.exe` file found in `build/Windows/` directory. 

### Unity Packages (source code)
If you want to use the source code to create your own simulations, it is easiest to import the code as a package. All `.UnityPackage` files are found in the `pkg` directory. Simply create an empty new project in Unity and import the package by clicking `Assets>Import package > import custom package`in the menu of the Unity editor. Some notes: 
- Originally the simulator made use of the OpenCVForUnity asset, which need to be bought in the asset store. This asset is useful for image processing (e.g., Gaussian Blurring, Canny edge detection.)
- If you don't want to pay for this utility, there is a version included where these dependencies are removed. Here the simulator makes use of a Sobel Edge detection shader. Look for the file that ends with `..._removed_dependencies.unitypackage`.

### Unity Project Folder & Miscellaneous Scripts
Importing the source code as package is recommended. Nevertheless the raw project files are also included in the `src` directory for version tracking and for an optional manual installation. 
- To manually install, clone this repository and add the folder `src/Xarpos-UnityProject` as a new project in the Unity Hub. 
- Again, the current version of the simulator is relatively basic (for demo purposes only). If you want to improve the simulator in this repository, you can make use of the miscellaneous scripts included under `src/_misc`. See also these projects for more advanced simulations: [dynaphos](https://github.com/neuralcodinglab/dynaphos) or [SPVGaze](https://github.com/neuralcodinglab/SPVGaze). 



### Remarks

- VR development: the currently included versions use the webcam/phonecam as input for the simulations. If you want to adapt the simulation for virtual environments, you can replace the WebCamTexture with a RenderTexture that receives input from a virtual camera.
