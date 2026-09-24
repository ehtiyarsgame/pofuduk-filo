# Screen fit — phones, tall phones, tablets

On 2026-09-24 the user asked for the game to be optimised for tablets and for large and small screens. The game is portrait only.

| Screen | Aspect (w/h) | Playfield | UI |
|---|---|---|---|
| Reference phone (1080×2400) | 0.45 | Orthographic size 10.8, width 9.72 u | Full safe area |
| Tall phone (e.g. 0.42) | < 0.45 | **Same width**, more height (size = 9.72 / (2·aspect)) | Full safe area |
| Wider phone (0.45 to 0.5625) | ≤ 9:16 | Size 10.8; the width grows up to 12.15 u | Full safe area |
| Tablet / foldable (e.g. 0.75) | > 9:16 | Centred **9:16 column**; plum bars on the sides (pillarbox camera) | The same centred column |

- **Formula:** `Formulas.FitCamera(aspect, 10.8)` returns the orthographic size and the viewport width fraction. `CameraFit` (execution order −1000) applies it before any system reads the camera bounds, and again whenever the resolution changes.
- **UI:** `UIFactory.SafeArea` intersects the device safe area with the same column. The CanvasScaler reference stays 1080×2400 with match 0.5, so text and buttons keep phone proportions on every size.
- **Tests:** FormulasTests (tall phone keeps the design width; tablet is pillarboxed; the reference phone is unchanged).
