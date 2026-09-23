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
| Systems | `BulletSystem` (mermi tiplerini ata), `EnemyManager`, `WaveDirector` (Run asset'i ata), `Juice` |
| Player | Sprite + `PlayerController`, `PlayerHealth`, `WeaponInventory` (başlangıç silahı), `XpSystem`, `UpgradeDraft` |
| Weapon prefab | `FeatherBlaster` → `WeaponDefinition.behaviourPrefab` alanına |
| Enemy prefab | Sprite + `Enemy` (SpriteRenderer referansı; `_FlashAmount` destekleyen shader) |
| Boss prefab | `Enemy` (veya alt sınıfı); **Formation Hold Seconds = 9999** ki slotundan ayrılmasın |

Mermi materyalleri: URP/Unlit, doku = mermi sprite'ı, **Enable GPU Instancing** açık, mesh = 1×1 quad.

## Sistem haritası

```
PlayerController ─► (konum) ─► BulletSystem ◄── WeaponBehaviour.Fire (SpawnPlayerBullet)
WaveDirector ─► EnemyManager.Spawn (bütçe × DDA, formasyonlar, boss fazları)
EnemyManager ─► NativeArray<EnemyProxy> ─► [Burst] grid + çarpışma ─► ApplyDamage ─► EnemyKilled
EnemyKilled ─► XpSystem.AddXp ─► LevelUpQueued ─► UpgradeDraft.Roll/Apply ─► WeaponInventory
MetaProgressionService (SaveService) ─► PlayerStats (koşu başında)
```

## Dalga yöneticisi (Run Definition)

*Create → Pofuduk Filo → Run Definition* ile bölüm başına bir asset oluşturun. GDD §3.2'deki
varsayılan zaman çizelgesi:

| Faz | Tür | Başlangıç (dk) | Sürü (threat cost / weight) | Formasyon | Bütçe ölçeği |
|-----|-----|----------------|-----------------------------|-----------|--------------|
| Dalga 1 | Waves | 0 | Civciv 1 / 10 | Grid 3×5, 20 sn | 1.0 |
| Dalga 2 | Waves | 1.5 | Civciv 1/10, Kurabiye Robot 3/4 | V 1×7, Arc 1×6 | 1.0 |
| Mini-boss 1 | MiniBoss | 3 | Civciv 1/10 | kapalı (0) | 0.3 |
| Dalga 3–4 | Waves | 3.2 | + Jöle Ayı 5/3, Sakız Balonu 4/2 | Grid, V, Arc, 16 sn | 1.0 |
| Mini-boss 2 | MiniBoss | 6 | Civciv 1/10 | kapalı | 0.3 |
| Dalga 5 "Kaos" | Waves | 6.2 | hepsi + Dondurma Kulesi 6/2 | 12 sn | 1.2 |
| Final Boss | FinalBoss | 8 | Civciv 1/6 | kapalı | 0.2 |

Kurallar: boss yaşarken zaman çizelgesi ilerlemez; boss ölünce `breatherSeconds` (9 sn) boyunca
spawn yok ve düşman mermileri temizlenir. Sürü bütçesi `Formulas.SpawnBudget × DDA × budgetScale`,
en fazla 3 saniyelik birikim. Olaylar: `PhaseStarted`, `FormationCleared` (XP yağmuru bonusu),
`BossSpawned`, `BossDefeated` (evrim sandığını burada aç), `RunCompleted`.

Henüz olmayanlar (sonraki adımlar): XP gem'leri (veri odaklı, mıknatıs), level-up kart UI'ı,
diğer 4 silah, boss saldırı desenleri, koşu sonu ekranı.
