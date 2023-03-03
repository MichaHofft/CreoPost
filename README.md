# CreoPost

CreoPost is a free, very simple post processor for converting Creo CL to GRBL G-Code.
It does to pretend to be a full-fledged CAM post processor.

# Use case

* use a education version of PTC reo
* create a CAM setup
* generate one or multiple (N|A) CL files describing the machine operations
* transform these into G-code
* .. which can be understood by simple GRBL-based machines.

# Principles and references

* Grbl is the firmware for the CND router: https://github.com/grbl/grbl
* G-Code are the commands with are describing the machining. See: https://www.sainsmart.com/blogs/news/grbl-v1-1-quick-reference#Parameters or https://www.minimill.at/de/welcher-gcode-ist-grbl-kompatibel/ or https://marlinfw.org/docs/gcode/G000-G001.html
* G-Code is a winner. Started as https://en.wikipedia.org/wiki/G-code it is understood by a wide range of devices
* Modern successors are https://en.wikipedia.org/wiki/STEP-NC and all-famous https://en.wikipedia.org/wiki/ISO_10303, maintained by https://en.wikipedia.org/wiki/ISO/TC_184/SC_4
* Candle is a tool for sending G-Code to the device: https://github.com/Denvi/Candle. It can also previe the G-Code in 3D space.
* CAD tools such as PTC Creo (https://www.ptc.com/en/products/creo/parametric) do not produce directly G-Code, but they produce a intermediate language such as BCL/ACL: https://webstore.ansi.org/standards/sae/saeeia494b2015eia494b
* Using a post-processor, such intermediate language can be adopted to G-Code variants understood by many different machines. 
* Such post-processor is described here: http://bdml.stanford.edu/twiki/pub/Manufacturing/HaasReferenceInfo/V61_GPost_CD_Manual.pdf
* Such post-processor needs specific electronic machine descriptions to tailor the G-Code dialects in a way, that the machine can safely operate. For a list see https://www.ptc.com/en/support/article/CS54723

# Building

* Current source code is C# for .NET 6.0 windows
* Solution can be opened and compiled using Microsoft Visual Studio 2019 or 2022.
