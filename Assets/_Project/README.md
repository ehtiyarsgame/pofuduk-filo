# Pofuduk Filo — Unity 6 Kod İskeleti

Tasarım: `design/gdd/` · Sanat: `design/art-bible.md` · Mimari: `docs/architecture/architecture.md`

## Kurulum (tek tık)

1. Unity Hub → **Unity 6.3 LTS** → yeni proje, şablon **Universal 2D**, konum = bu deponun kökü
   (`Assets/_Project` korunur).
2. Package Manager: **Burst**, **Collections**, **Mathematics**, **Input System**
   (Unity 6 2D şablonunda UGUI ve Test Framework hazır gelir). Input System "Both / New" sorusuna **Yes**.
3. Menü: **Pofuduk Filo ▸ Oynanabilir Sahneyi Kur**. Araç şunları üretir (`Assets/_Project/Generated/`):
   sevimli yer tutucu sprite'lar, instanced materyaller, 8 mermi tipi, 8 pasif, 5 silah + 5 evrim,
   6 düşman + 3 boss, 3 bölüm (Run), 10 Atölye yükseltmesi ve `Assets/_Project/Scenes/Main.unity`.
   Tekrar çalıştırmak güvenlidir; varlıkları yerinde günceller.
4. Game görünümünü **9:19.5 portre** (ör. 1080×2340) yapıp **Play**. Fare sürükleme = parmak.
5. İsteğe bağlı: DOTween (Utility Panel → Create ASMDEF → `PofudukFilo.Runtime`'a `DOTween.Modules`
   referansı + Scripting Define `DOTWEEN`). Olmadan da oyun çalışır; sadece UI zıplamaları ve kamera
   sarsıntısı kapalı kalır.
6. Testler: *Window → General → Test Runner → EditMode → Run All*. Smoke:
   `Unity -batchmode -quit -projectPath . -executeMethod PofudukFilo.EditorTools.SmokeCheck.Run`.

Buluttan (Unity'siz) derleme + saf mantık testleri: `tools/verify/run.sh`.

Yer tutucu sanat önizlemesi: `design/placeholder-art-preview.png`. Gerçek çizimler geldiğinde
`Generated/Art/*.png` dosyalarını değiştirmeniz yeterli.

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

## Oynanış döngüsü (hepsi bağlı)

Ana menü (bölüm seçimi, cüzdan, Atölye) → koşu (dalgalar, formasyonlar, mini-boss'lar, final boss) →
XP taşları / altın / mıknatıs / kalp / bomba → seviye atlama kartları (yeniden çek, yasakla) →
boss/elit sandığı ile evrim → ölüm (diriliş / isteğe bağlı reklam) → koşu sonu (sayılan altın,
"sonraki yükseltme" hedefi) → Atölye.

Kapsam dışı / sonraki adımlar: füzyon silahları, karakter Hangar'ı ve Takımyıldız tahtası
(meta-economy.md §3.3 B–E), ses, gerçek reklam SDK'sı, bulut kayıt, yerelleştirme, gerçek sanat.
