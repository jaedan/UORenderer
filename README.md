# Ultima Online Style Render Demo, but 3D

This repository contains a demo for an alternate strategy for rendering a 2D, fake isometric game like Ultima Online. The map/terrain are rendered in full 3D, but the "statics" - decoration and such - are rendered as billboards.

## Build

You'll need Visual Studio with .net 9. It will probably work on Linux, but I'm only testing Windows at the moment.

* Clone this repository (with submodules, recursively).
~~~sh
git clone https://github.com/jaedan/uorenderer
cd uorenderer
git submodule update --init --recursive
~~~

* Open the solution file (UORenderer.sln)
* Press build

## Run

Edit src/Project.cs and change BasePath to point at your UO installation. I am currently using a 7.0.8 Stygian Abyss client for most testing.

Then just press run in Visual Studio and you'll show up at Brit Bank.

## Controls

* WASD to move around.
* Q/E to rotate
* Z/X or mouse wheel to zoom in/out
* Esc resets to standard view

