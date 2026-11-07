# Manrope

Static Regular and SemiBold TrueType fonts from the official googlefonts/manrope repository, revision `468c0dbe38efa331b80bfe9448256abe27be44c3`.

- https://github.com/googlefonts/manrope/tree/468c0dbe38efa331b80bfe9448256abe27be44c3/fonts/ttf
- `manrope-regular.ttf` → `Manrope-Regular.ttf` (unchanged bytes)
- `manrope-semibold.ttf` → `Manrope-SemiBold.ttf` (unchanged bytes)
- SIL Open Font License 1.1, supplied alongside the fonts as OFL.txt.

USS directly references the imported Font asset. Unity 6 supports Font and FontAsset in `-unity-font-definition`; see the official StylePropertyReader.ReadFontDefinition implementation:
https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Modules/UIElements/Core/StyleSheets/StylePropertyReader.cs#L226

This keeps the existing baked JalanFont fallback intact; no font atlas is hand-authored or regenerated. Static fonts avoid variable-font importer differences. The preview continues using the standard text generator.
