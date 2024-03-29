# Dual Contouring on a Uniform Grid

Computational Geometry final project by Vincent Nikolayev and Conner Moore. We implemented Dual Contouring on a Uniform Grid entirely on the GPU in the Unity game engine. We also visualized parts of the algorithm execution such as the volumetric field, surface point placement, and wireframe renders of the polygonal mesh. Our results include basic SDF surfaces, 3D noise surfaces, with triplanar texture mapping, flat shading, and shading of surface normals. 

The relevant compute shaders and csharp scripts are located in `./Assets/Isosurface/Scripts`.

![main-preview](./Images/main-preview.png)

## Installation

1. Unity game engine required to run.
2. Git clone the project and add it to Unity Hub.
3. Select the main scene and run.

## Report

The associated report is found [here](https://docs.google.com/document/d/1gRrFQ69Ng3mF5B550XXXuDtswovxLhK4cKRdbKfFYmY/edit?usp=sharing). We discuss other Isosurface Extraction techniques such as Marching Cubes and improving performance with Octrees.

## Presentation

The slides used in our in-class presentation are found [here](https://docs.google.com/presentation/d/14Gk8NlOuF4xucH_i8O3gszvqvE339LZGBSqDQ9dZmoo/edit?usp=sharing)

## Visualizations

![volume_surface-points](./Images/volume_surface-points.png)

![qef](./Images/qef.png)
