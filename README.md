# Ships Carry Crew for Solar Expanse
Solar Expanse mod removing the need for crew modules to ship humans. Spacecraft now posssess a "Crew Capacity" variable that directly allows them to ship a certain amount of humans, free of modules, anywhere those spacecraft are present.

## Features
- Spacecraft now have a "Crew Capacity" metric, visible in the hover-over tooltip.
- When planning a mission, the "Crew Module" is now available in the "add modules" tab and is always visible for spacecraft with Life Support capacity. This  module can hold up to the spacecraft's Crew Capacity.
<img width="354" height="401" alt="image" src="https://github.com/user-attachments/assets/28258a35-2500-4136-9f45-a3d0d79d0f8c" />

- This mod does not remove the existing crew capsules; if you want to load your ship's cargo (and take the associated extra mass penalties), that is still an option.
- Values for Crew Capacity per ship (and, as needed, for custom ships) is editable via YAML.

## Installation
1. This plugin uses BepInEx 5.4 to inject code into the Solar Expanse exe. 
2. Once BepInEx is installed, run it once to generate ```Solar Expanse/BepInEx/Plugins```
3. Download the latest release of this mod.
4. Move the ```ships-carry-crew``` folder in the ZIP into the ```BepInEx/Plugins``` folder.

