# Brand — Ehtiyars Game studio intro, launcher icon, credit

## 1. Overview

The user asked on 2026-09-24: *"logo ve ehtiyars game başlangıcı vs da olsun, tam takır olsun"* (add a logo and an Ehtiyars Game opening, so it feels complete). The app now opens with its own studio intro instead of the engine splash, installs with a proper launcher icon, and credits the studio on the menu.

## 3. Detailed Rules

- **Studio emblem** (`ArtRecipes.StudioEmblem`, 512²). A round badge: honey rim with twelve small stars, plum field. Inside is a cheerful chibi *ehtiyar* ("old man", the studio's name): flat cap (kasket), round glasses, happy closed eyes, big white moustache and beard. It is procedural like every other sprite and previewed with `tools/verify/art`.
- **Studio intro** (`UI/StudioIntro`), played on the first scene load of each launch:
  - It sits on an overlay canvas at sort order 1000, on a plum backdrop that blocks taps to the menu underneath.
  - Timing, in unscaled time: the emblem pops in over 0.5 s (ease-out-back, LevelUp chime); **EHTIYARS GAME** (honey) and **sunar** / *presents* (lilac) rise in; the screen holds for 1.3 s; the content fades in 0.35 s and the backdrop in 0.4 s. About 2.7 s in total.
  - A tap skips straight to the fade.
  - A language switch reloads the scene, but the intro does not replay (static once-per-process flag).
- **Engine splash off:** `PlayerSettings.SplashScreen.show = false` and `showUnityLogo = false`.
- **Launcher icon** (`ArtRecipes.AppIcon`, 512²). The bunny starfighter on a rounded candy-sky tile (plum to pink) with sparkles. It is set as the default icon, which Android's legacy and round icons use.
- **Company name:** "Ehtiyars Game". The package id `com.ehtiyarsgame.pofudukfilo` is unchanged, so existing installs and saves carry over.
- **Menu credit:** "© EHTIYARS GAME" at the bottom, in lilac at 70 % opacity.

## 5. Edge Cases

- **UI font missing:** the intro falls back to the built-in font, as the rest of the UI does.
- **App backgrounded during the intro:** unscaled time keeps running, so it simply finishes.

## 8. Acceptance Criteria

1. A cold start shows the plum screen with the emblem, EHTIYARS GAME and "sunar" (in English: "presents"), then the menu after about 3 s. The QA screenshot `00_intro.png` is kept.
2. Tapping during the intro reaches the menu within 0.8 s.
3. No "Made with Unity" splash appears.
4. The launcher shows the bunny starfighter icon, not the Unity default.
5. The menu footer reads "© EHTIYARS GAME".
