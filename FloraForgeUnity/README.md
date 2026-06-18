# FloraForge Unity

Unity 6.3 LTS project for procedural fantasy vegetation.

## Open

Open this folder in Unity Hub:

`D:\github\FloraForge\FloraForgeUnity`

## Demo

Use one of these editor menu items:

- `Tools > FloraForge > Create Tavern Vegetation Demo Scene`
- `Tools > FloraForge > Add Generator To Current Scene`

The generator creates a small wooden facade with procedural climbing vines, hanging vines, shrubs, and wildflower clumps. Select `FloraForge Vegetation Generator` in the scene and adjust the parameters in the Inspector, then press `Regenerate`.

## First target

This version favors fast iteration over production rendering. The next production step is to replace generated leaf and flower cards with authored meshes/materials, then batch vegetation into combined meshes or GPU instancing.

## Bush Generator Notes

Current bush generator art and placement requirements are tracked in `..\docs\bush-generator-request.md`.
