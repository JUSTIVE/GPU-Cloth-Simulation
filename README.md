# GPU Cloth Simulation

This repository is about GPU based Mass-Spring Simuation in Unity. This project is a sub project of [ReclothSimulation-master](), some of features are removed.  

- Mass Spring System Cloth
- ~~Inter Object Collision-Response~~
- ~~Self Collision-Response~~
- ~~Dynamic Collider Generation~~

Currently Confirmed working on following platforms:

- Unity Engine version 5.6.1f2 ~ 2018.2.0b10
- Windows(OpenGL Core)
- Android(OpenGL Core)

## How to use

Create a empty GameObject on hierarchy panel and append [ClothSimuation]("../Assets/GPUClothModule/script/ClothSimulation.cs") and [ClothRenderer]("../Assets/GPUClothModule/script/ClothRenderer.cs") scripts on it. Since this simulation calculates with its own system, you'll need special materials and dedicated shaders for rendering. There are two example materials(Face Normal Rendering, Diffuse Rendering).

### Attributes

#### Cloth Simulation

##### Compute Shaders

attributes|description
:---|:---
NodeUpdateComputeShader| a compute shader which updates forces, positions, velocities, etc.
TriCollisionResponseComputeShader| a compute shader which calculates collision with floor
NormalComputeShader| a compute shader which calculates all face normals of the cloth model

##### Attribute of Cloth

attributes|description
:---|:---
VertexColumn|numbers of nodes in a Column
VertexRow|numbers of nodes in a Row
ClothSize|Size of cloth object in Unity world scale
ClothMass|Mass of cloth object
Looper|amount of looping compute shaders per frame, larger number for precise, slower simulation

##### Constant for Cloth

attributes|description
---|---
SpringK|spring force from [Hooke's Law](https://en.wikipedia.org/wiki/Hooke%27s_law)
Damping|damping coefficient for cloth simulation
External Force|external forces such as wind, water etc. since it is public member, you can pass from external scripts

#### Cloth Renderer

### recommended parameters

Since the simulation is not pretty stable, certain values for the clothSimulation scripts are recommended:

attributes|values
---|---
vertexColumn|8 ~ 64
vertexRow|8 ~ 64
Looper|20 ~ 500
Damping|100~200

## Contact

you can contact Minsang Kim via e-mail(ben399399@gmail.com).

## License

This Project is licensed under the [MIT License](https://opensource.org/licenses/MIT)

Copyright (c) 2018 Minsang Kim

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.