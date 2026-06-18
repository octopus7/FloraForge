# Bush Generator Request Notes

This document records the current art and implementation direction for the Unity bush generator.

## Shape Goal

- The bush should read as a rounded boulder-like mass, not a cone, teepee, cabbage stack, or flat wall.
- A user-provided volume mesh should define the outer bush volume.
- The default dummy volume should remain a rounded cube/loaf shape for testing.
- The top crown must not have a bald circular hole. Add explicit crown fill clusters when surface sampling leaves the top sparse.

## Leaf Placement

- Leaves must not be placed as independent random cards scattered over a surface.
- Leaves should form small natural sub-clusters of roughly 5-10 leaves.
- Sub-clusters should look like naturally splitting leaf growth, not a literal fan.
- Use phyllotaxis-style alternating divergence inside each cluster.
- Prefer golden-angle or similar spiral/alternate offsets for cluster members.
- Keep a shared branch/growth direction per cluster, but vary each leaf position and orientation enough to avoid a comb or fan shape.

## Layering

- Layer colors are not enough. Layer positions must differ in actual depth.
- Shadow/internal leaves should sit deeper, lower, and slightly farther back.
- Mid leaves should occupy the middle shell.
- Highlight/outer leaves should sit farther out, higher, and slightly toward the front.
- Debug tint mode may use strong spectrum colors and ignore texture so layer separation is obvious.

## Material Constraints

- Final output target remains one MeshRenderer and one Material.
- Use vertex colors for tint and depth data.
- Texture can be swapped later; debug mode may ignore texture completely.

