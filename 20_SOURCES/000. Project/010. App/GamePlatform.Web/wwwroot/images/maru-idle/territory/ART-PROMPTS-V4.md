# World-map artwork v4

Generated with built-in image_gen (not CLI). Original v2 artwork preserved.
Project assets: terrain-v4.png, strongholds-v4.png, resources-v4.png.
The latter two are 3-column square-cell RGBA atlases consumed without raster editing.

Final expanded-world assets added afterward:
- `continent-v5.png`: continuous continent, replaces the repeated terrain-v4 background. Exact prompt and delivered resolution: [CONTINENT-PROMPT-V5.md](CONTINENT-PROMPT-V5.md).
- `resource-sites-v4.png`: farm, logging camp, quarry atlas for map locations, distinct from inventory resources. Exact prompt: [RESOURCE-SITES-PROMPT-V4.md](RESOURCE-SITES-PROMPT-V4.md).
- All files are saved beside this document and generated with the built-in image_gen tool, not CLI. Original generated files remain in the tool output directory.

## terrain

Use case: stylized-concept. Asset type: square strategy-game world terrain texture for a pannable and zoomable wuxia 4X game. Create a very detailed, premium painterly-realistic orthographic high-overhead landscape, 2048x2048 square. Entire square is usable terrain, no horizon, no perspective vanishing point. A broad connected verdant grassland basin dominates the central 70%, winding dirt roads and small streams cross it, dense bamboo forest patches left and lower left, gray jade cliffs and rock veins right, pale golden wheat fields lower right, dramatic rocky mountain ridges framing outer edges. Keep terrain readable at game-map scale, clear distinct biomes and excellent material detail. Soft bright afternoon sunlight, muted emerald and jade greens, warm ochre earth, silver-blue waters, restrained mist ONLY at distant edge ridges. No castles, no buildings, no characters, no symbols, no labels, no UI, no grid, no dark fog rectangles. Ground is background for separate interactive object sprites, therefore many clear landing spaces evenly distributed.

## strongholds

Use case: stylized-concept. Asset type: production game sprite atlas on genuinely transparent background, exactly THREE equal square cells in one horizontal row, wide 3:1 canvas. Each isolated object centered in its own cell with 12% clear transparent margin and same isometric 3/4 overhead camera angle. LEFT: magnificent friendly East Asian wuxia fortress with multi-tier jade-tiled roofs, carved bronze gilded details, pale stone defensive walls, central ornate pagoda citadel and teal banners. MIDDLE: smaller hostile military outpost, dark wooden palisade, stone gatehouse, tiled watchtowers and burgundy banners. RIGHT: imposing enemy imperial black citadel with layered black tile roofs, dark volcanic stone walls, dramatic red and gold central palace. Premium commercially shipped strategy-game rendering, crisp intricate architecture, readable silhouette, realistic materials, attractive warm directional lighting. Complete building bases fully visible, tiny soft contact shadows, no ground square or platform. Do not merge the three objects. No text, no letters, no panels, no borders, no background color. Preserve actual alpha transparency around sprites.

## resources

Use case: stylized-concept. Asset type: premium realistic game inventory RESOURCE ICON ATLAS, exactly THREE equal square cells in one horizontal row, 3:1 canvas on genuinely transparent background. LEFT cell: open burlap sack full of golden rice grains with a few wheat ears, food provision icon. MIDDLE cell: three stacked cut timber logs with bark and clearly visible pale growth-ring ends, bound with rough rope, wood resource icon. RIGHT cell: a compact stack of three quarried pale bluish-gray stone blocks and smaller uncut chunks, stone resource icon. Each centered with generous 15% clear transparent padding, complete silhouette no cropping, nothing crosses cell boundaries. All same three-quarter camera and coherent warm upper-left lighting, rich hand-painted photorealistic 3D game icon finish, readable at 32px, strong material contrast and rim highlights, suitable for a premium East Asian fantasy strategy game's jade-black UI. No emoji look, no flat vector, no text, no coins, no fantasy gems, no panels, no borders, no background scene, no opaque background. Genuine transparent alpha.
