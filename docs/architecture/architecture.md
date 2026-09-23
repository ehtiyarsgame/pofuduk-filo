# Teknik Mimari — Pofuduk Filo (Unity 6)

> Kapsam: Yüzlerce mermi ve düşmanın aynı anda 60 FPS'te çalıştığı mobil shmup için
> performans odaklı mimari ve yol haritası. İlgili kod: `Assets/_Project/Scripts/`.
>
> ⚠️ Unity 6 API'leri modelin eğitim verisinin ötesinde olabilir; motor sürümü
> `/setup-engine` ile sabitlenince `docs/engine-reference/unity/` ile çapraz kontrol edin.

## 1. Hedefler ve Bütçeler

| Metrik | Hedef (orta segment: Snapdragon 680 / Helio G85, 4 GB) |
|--------|--------------------------------------------------------|
| Kare hızı | Sabit **60 FPS** (16.6 ms); düşük cihazda 30 FPS kademesi |
| CPU oyun mantığı | ≤ 5 ms (ana iş parçacığı) |
| Render (CPU tarafı) | ≤ 4 ms, **≤ 60 draw call** |
| Mermiler (eşzamanlı) | Oyuncu 1.500 + düşman 800 |
| Düşmanlar | 250 aktif |
| XP gem'leri | 250 (sonrası birleşir) |
| Parçacıklar | ≤ 1.500 aktif (kademeye göre) |
| GC tahsisi (koşu sırasında) | **0 B/kare** |
| Bellek | ≤ 450 MB (düşük cihaz ≤ 300 MB) |
| Isınma | 15 dk koşuda termal kısma yok (Adaptive Performance) |

## 2. Temel Karar: Tam ECS değil, **hibrit veri odaklı** mimari

| Seçenek | Artı | Eksi | Karar |
|---------|------|------|-------|
| **Saf MonoBehaviour + GameObject pooling** | Basit, editör dostu, hızlı prototip | ~500+ mermide Transform güncelleme ve `Update()` maliyeti; fizik temasları pahalı | Prototip ve **"az sayılı"** nesneler için |
| **Tam DOTS/ECS (Entities 1.x)** | Maks. ölçeklenebilirlik | Entities Graphics 2D `SpriteRenderer`'ı desteklemez; öğrenme eğrisi, editör iş akışı, küçük ekip için yavaş iterasyon | **Reddedildi** (ADR yazılacak) |
| **Hibrit: GameObject'ler + Burst/Jobs ile "veri olarak mermi"** | Mermiler için ECS'e yakın performans, geri kalan her şey alışık olunan Unity iş akışı | Özel render ve çarpışma kodu gerekir | ✅ **Seçildi** |

**Kural:** *Sayısı çok ve davranışı basit* olan (mermiler, XP gem'leri, basit sürü düşmanları)
→ **veri (NativeArray + Burst job + instanced render)**. *Sayısı az ve davranışı zengin* olan
(oyuncu, boss, elitler, silah kontrolcüleri, UI) → **pooled GameObject + MonoBehaviour**.

## 3. Katmanlar ve Klasör Yapısı

```
Assets/_Project/
├── Scripts/
│   ├── Core/          GameLoop, Formulas (saf C#), servis erişimi
│   ├── Pooling/       PrefabPool<T> (UnityEngine.Pool tabanlı), IPoolable
│   ├── Bullets/       BulletSystem (Burst), BulletJobs, BulletTypeDefinition (SO)
│   ├── Enemies/       Enemy, EnemyManager (konum verisini NativeArray'e yazar)
│   ├── Player/        PlayerController (göreli sürükleme), PlayerHealth
│   ├── Weapons/       WeaponDefinition (SO), WeaponBehaviour, FeatherBlaster, WeaponInventory
│   ├── Progression/   XpSystem, UpgradeDraft (ağırlıklı kart çekimi)
│   ├── Meta/          MetaProgressionService, MetaUpgradeDefinition (SO), SaveService
│   └── Feel/          Juice (hitstop, sarsıntı, DOTween UI)
├── Data/              ScriptableObject örnekleri (silahlar, düşmanlar, dalgalar, meta)
├── Art/ Audio/ VFX/   Sprite Atlas'lar, ses, VFX
└── Tests/EditMode/    Formül ve taslak birim testleri
```

**Veri akışı (bir kare):**

```
Update (sıra -100)  PlayerController  → girdi, konum
Update (sıra -50)   EnemyManager      → düşman hareketi, NativeArray<float3> konum/yarıçap yaz
Update (sıra 0)     WeaponBehaviour'lar → BulletSystem.Spawn(...) (bekleyen tampona)
Update (sıra 100)   BulletSystem      → bekleyenleri ekle, uzamsal ızgarayı kur,
                                        [Burst] Hareket + Çarpışma job'larını planla
LateUpdate (100)    BulletSystem      → Complete(), isabetleri uygula (hasar, ölüm, VFX),
                                        ölü mermileri sıkıştır, RenderMeshInstanced ile çiz
```

## 4. Mermi Sistemi (en kritik parça)

### 4.1 Veri düzeni

Mermiler GameObject **değildir**. `NativeList<BulletData>` (struct, ~48 byte) içinde yaşarlar:
konum, hız, açısal yön, yarıçap, hasar, delme hakkı, ömür, tip indeksi. Oyuncu ve düşman
mermileri **ayrı listelerde** tutulur (farklı çarpışma hedefleri).

### 4.2 Hareket — `MoveBulletsJob : IJobParallelFor` [BurstCompile]

Konum += hız × dt, ömür azalır, ekran dışı kontrolü. 2.000 mermi Burst ile orta
cihazda ~0.1–0.2 ms (tahmini; Profiler ile doğrulanmalı).

### 4.3 Çarpışma — fizik motoru **yok**

`Physics2D` binlerce tetikleyicide pahalıdır (broadphase + callback + GC). Bunun yerine:

1. **Uzamsal hash ızgarası:** Düşman konumları `NativeParallelMultiHashMap<int,int>`'e
   hücre anahtarıyla yazılır (hücre = 1.0 birim; en büyük düşman yarıçapına göre ayarlanır).
2. **Oyuncu mermisi job'u:** Her mermi kendi hücresi + 8 komşuyu tarar, daire–daire testi,
   isabeti `NativeQueue<BulletHit>.ParallelWriter`'a yazar.
3. **Düşman mermisi job'u:** Tek hedef (oyuncu) → basit mesafe testi + **kıl payı (graze)**
   yarıçapı ayrı sayılır.
4. Ana iş parçacığı kuyruğu boşaltır → `Enemy.TakeDamage()` → ölüm → pool'a iade.

Delme (pierce): mermi başına bir sayaç; paralel job'da yalnızca kendi mermisine yazdığı
için yarış durumu yok. (Aynı karede aynı düşmana çift isabet kabul edilebilir; gerekirse
`lastHitEnemyId` alanı ile engellenir.)

### 4.4 Çizim — `Graphics.RenderMeshInstanced`

- Her mermi tipi = **1 quad mesh + 1 instancing açık materyal** (URP Unlit, sprite atlas'tan
  tek karelik doku). Tip başına 1023'lük gruplar halinde tek draw call.
- 2.300 mermi / ~8 tip ≈ **8–12 draw call**. GameObject yok → Transform maliyeti yok.
- Sıralama: düşman mermileri oyuncu mermilerinden **sonra** (üstte) çizilir (art-bible §2.2).
- İleri seviye (gerekirse): `BatchRendererGroup` veya `RenderMeshIndirect` + `GraphicsBuffer`
  ile tek çağrıda tüm mermiler; tip başına UV'yi instance verisinden okuyan özel shader.

## 5. Object Pooling

- **`UnityEngine.Pool.ObjectPool<T>`** (Unity yerleşik) sarmalanır → `PrefabPool<T>`.
- **Ön ısıtma (prewarm)** yükleme ekranında: düşman tipi başına 40–80, VFX başına 30,
  hasar sayısı 60. Koşu sırasında `Instantiate` = hata (Profiler marker ile yakalanır).
- `IPoolable.OnSpawned/OnDespawned` ile durum sıfırlama; **`SetActive` yerine** mümkünse
  renderer/collider kapatma + ekran dışına taşıma (SetActive hiyerarşi maliyeti yüksek).
- Havuz nesneleri sahne kökünde değil, bir "Pool" parent'ı altında (hierarchy dirty
  maliyetini azaltır); `Transform.SetParent` koşu sırasında çağrılmaz.
- Parçacıklar: `ParticleSystem` havuzlanır; kısa efektler için tek bir **paylaşılan**
  ParticleSystem'e `Emit(EmitParams)` ile yayım (yüzlerce patlama = 1 sistem, 1 draw call).

## 6. DOTween Kullanım Politikası

| ✅ Kullan | ❌ Kullanma |
|-----------|------------|
| UI animasyonları (kart düşüşü, buton punch, sayı sayacı) | Mermiler (veri; tween yok) |
| Nadir oyun olayları (evrim animasyonu, boss giriş, sandık) | Sürü düşmanlarının hareketi (Burst veya basit Update) |
| Kamera sarsıntısı (`DOShakePosition`) | Her kare yeni tween oluşturma |

Ayarlar:
- Başlangıçta `DOTween.Init(recycleAllByDefault: true, useSafeMode: false).SetCapacity(500, 50)`
  → çalışma anında kapasite büyümesi (GC) olmaz. *Safe mode* sadece geliştirmede açık.
- Tekrarlanan tween'ler **bir kez oluşturulur**: `SetAutoKill(false)` + `Pause()`, sonra
  `Restart()` (ör. buton punch).
- Havuza dönen nesnede **`transform.DOKill()`** zorunlu (sızan tween = hayalet hareket).
- Duraklatmadan etkilenmemesi gereken UI tween'leri `SetUpdate(true)` (unscaled time);
  hitstop sırasında UI akmaya devam eder.
- Kurulum: DOTween Utility Panel → **Create ASMDEF**, ardından `PofudukFilo.Runtime`
  asmdef'ine `DOTween.Modules` referansı ve Player Settings'e `DOTWEEN` scripting define'ı
  ekleyin (kod `#if DOTWEEN` ile korunur).

## 7. Render ve VFX Optimizasyonu

- **URP 2D Renderer**, HDR kapalı, MSAA kapalı (2D'de gereksiz), post-processing yalnızca
  hafif Bloom (düşük kademede kapalı). **2D ışıklar kullanılmıyor** (pastel sabit ışık).
- **SRP Batcher** açık (GameObject sprite'lar için), mermiler için instancing.
- **Sprite Atlas** (V2): bölüm başına bir düşman atlası, bir mermi atlası, bir UI atlası.
- **Overdraw** mobilde birincil darboğaz: şeffaf alanı küçük sprite'lar ("Tight" mesh
  tipi), büyük yarı saydam efektlerden kaçın, Frame Debugger + RenderDoc ile ölç.
- **VFX kalite kademeleri** (Adaptive Performance veya kendi FPS izleyicimiz):

| Kademe | Tetik | Parçacık bütçesi | Bloom | Arka plan paralaks | Hedef FPS |
|--------|-------|------------------|-------|--------------------|-----------|
| Yüksek | ort. kare < 14 ms | 1.500 | Açık | 3 katman | 60 |
| Orta | 14–16.5 ms | 800 | Kapalı | 2 katman | 60 |
| Düşük | > 16.5 ms (3 sn) | 400 | Kapalı | 1 katman | 60 → 30 |

- `Application.targetFrameRate = 60`, `QualitySettings.vSyncCount = 0`; pil tasarrufu modunda 30.

## 8. Oyun Mantığı Performans Kuralları (Kod İnceleme Listesi)

1. Oyun döngüsünde **LINQ, `foreach` on interface, lambda capture, string birleştirme yok**.
2. `GetComponent` / `Find` / `Camera.main` → yalnızca başlatmada, önbelleğe al.
3. Yüzlerce nesne için bireysel `Update()` yerine **yönetici döngüsü** (EnemyManager tek
   `Update` içinde tüm düşmanları gezer) — Unity'nin native→managed çağrı maliyeti.
4. Veriler `ScriptableObject`'te; çalışma anında SO'ya **yazılmaz** (kopya runtime durumu).
5. Rastgelelik: `Unity.Mathematics.Random` (job uyumlu, seed'lenebilir → tekrar oynatma/test).
6. Olaylar: C# `event`/`Action` (tahsissiz); UnityEvent sadece editör bağlamalarında.
7. `NativeContainer`'lar `Allocator.Persistent` ile koşu başında ayrılır, `OnDestroy`'da
   `Dispose` edilir. Kare içi geçiciler `Allocator.TempJob`.
8. Addressables ile bölüm varlıkları yüklenir/boşaltılır (bellek ≤ 450 MB).

## 9. Meta Veri ve Kayıt

- `SaveData` (plain C# sınıfı) → `JsonUtility` → `Application.persistentDataPath`.
- **Atomik yazma:** önce `.tmp`'ye yaz, sonra değiştir (çökmede bozuk kayıt yok).
- **HMAC-SHA256 imza** — basit kayıt düzenleme hilelerini caydırır (gerçek güvenlik değil;
  premium para birimi **sunucu tarafında** doğrulanmalı).
- Unity Gaming Services **Cloud Save** ile senkron; çakışmada "ilerleme puanı" yüksek olan kazanır.
- Koşu ortası anlık görüntüsü (snapshot) her dalga sonunda yazılır (OS uygulamayı öldürürse).

## 10. Yol Haritası

| Faz | Süre | Teslim | Performans kapısı |
|-----|------|--------|-------------------|
| **0 — Gri kutu prototip** | 2 hf | Sürükleme, 1 silah, 2 düşman, XP + kart taslağı. GameObject pooling ile | 300 mermi @ 60 FPS (hedef cihaz) |
| **1 — Veri odaklı mermiler** | 2 hf | `BulletSystem` (Burst + instancing + uzamsal ızgara) | 2.000 mermi, 0 B GC/kare |
| **2 — Dikey dilim** | 4 hf | 5 silah + evrimler, 1 bölüm, boss, art-bible'a uygun 1 tema, juice | Profiler oturumu: ana iş parçacığı ≤ 8 ms |
| **3 — Meta döngü** | 3 hf | Atölye, Hangar, kayıt, koşu sonu ekranı | Soğuk açılış ≤ 4 sn |
| **4 — İçerik + ayar** | 6 hf | 3 takımyıldız, dengeleme, telemetri | 15 dk soak test, termal kısma yok |
| **5 — Soft launch** | — | Tek ülke, KPI ölçümü (D1/D7) | Crash-free ≥ %99.5 |

**Profil alışkanlığı:** Her sprint sonunda gerçek cihazda (IL2CPP, Release, Development
Build) Unity Profiler + Memory Profiler; editör ölçümleri **geçersiz** kabul edilir.
Önerilen şablon skill'leri: `/perf-profile`, `/architecture-decision` (ECS reddi için ADR),
`/create-architecture`.

## 11. Paketler (Package Manager)

| Paket | Neden |
|-------|-------|
| `com.unity.burst`, `com.unity.collections`, `com.unity.mathematics` | Mermi job'ları |
| `com.unity.inputsystem` | Enhanced Touch (çoklu dokunma, düşük gecikme) |
| `com.unity.render-pipelines.universal` | URP 2D |
| `com.unity.2d.sprite`, `com.unity.2d.animation` (isteğe bağlı) | Sprite Atlas, iskelet anim |
| `com.unity.addressables` | Bölüm bazlı içerik yükleme |
| `com.unity.adaptiveperformance` (+ sağlayıcı) | Termal/performans kademeleri |
| `com.unity.services.cloudsave` | Bulut kayıt |
| `com.unity.test-framework` | EditMode/PlayMode testleri |
| DOTween (Asset Store / Demigiant) | UI ve juice tween'leri |
