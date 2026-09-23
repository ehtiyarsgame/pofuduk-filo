# Pofuduk Filo — Unity 6 Kod İskeleti

Tasarım: `design/gdd/` · Sanat: `design/art-bible.md` · Mimari: `docs/architecture/architecture.md`

## Kurulum

1. Unity 6 ile **2D (URP)** şablonundan proje oluşturup bu deponun köküne yerleştirin
   (`Assets/_Project` korunur).
2. Package Manager: Burst, Collections, Mathematics, Input System, Test Framework
   (tam liste: architecture.md §11). Player Settings → *Active Input Handling* = Input System.
3. DOTween (isteğe bağlı ama önerilen): içe aktar → Utility Panel → **Create ASMDEF** →
   `PofudukFilo.Runtime.asmdef`'e `DOTween.Modules` referansı ekle → Scripting Define `DOTWEEN`.
4. Test: *Window → General → Test Runner → EditMode → Run All*.

## Sahne kurulumu (minimum oynanabilir)

| GameObject | Bileşenler |
|------------|-----------|
| Main Camera | Orthographic, size ≈ 9.75 (portre 9:19.5, 10 birim genişlik) |
| Systems | `BulletSystem` (mermi tiplerini ata), `EnemyManager`, `Juice` |
| Player | Sprite + `PlayerController`, `PlayerHealth`, `WeaponInventory` (başlangıç silahı), `XpSystem`, `UpgradeDraft` |
| Weapon prefab | `FeatherBlaster` → `WeaponDefinition.behaviourPrefab` alanına |
| Enemy prefab | Sprite + `Enemy` (SpriteRenderer referansı; `_FlashAmount` destekleyen shader) |

Mermi materyalleri: URP/Unlit, doku = mermi sprite'ı, **Enable GPU Instancing** açık, mesh = 1×1 quad.

## Sistem haritası

```
PlayerController ─► (konum) ─► BulletSystem ◄── WeaponBehaviour.Fire (SpawnPlayerBullet)
EnemyManager ─► NativeArray<EnemyProxy> ─► [Burst] grid + çarpışma ─► ApplyDamage ─► EnemyKilled
EnemyKilled ─► XpSystem.AddXp ─► LevelUpQueued ─► UpgradeDraft.Roll/Apply ─► WeaponInventory
MetaProgressionService (SaveService) ─► PlayerStats (koşu başında)
```

Henüz olmayanlar (sonraki adımlar): dalga yöneticisi (spawn bütçesi `Formulas.SpawnBudget`),
XP gem'leri (veri odaklı, mıknatıs), level-up kart UI'ı, diğer 4 silah, boss'lar, koşu sonu ekranı.
