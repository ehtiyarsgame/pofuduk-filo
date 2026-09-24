# Pofuduk Filo — Oyun Konsepti ve Çekirdek Döngü GDD

> **Çalışma adı:** Pofuduk Filo (*Fluffy Fleet*)
> **Tür:** Dikey kaydırmalı shoot 'em up + rogue-lite yetenek taslağı (draft)
> **Platform:** iOS / Android (portre, tek parmak)
> **Motor:** Unity 6 (URP 2D) — mimari için bkz. `docs/architecture/architecture.md`
> **İlgili dokümanlar:** `design/gdd/weapon-system.md`, `design/gdd/meta-economy.md`, `design/art-bible.md`

---

## 1. Overview (Genel Bakış)

Oyuncu, şeker gezegeni **Pamukşeker**'i atıştırmalık düşkünü sevimli uzay yaratıklarından
(civcivler, jöle ayıcıklar, kurabiye robotlar) korumaya çalışan tavşan pilot **Pıtır**'ı
yönetir. Gemi otomatik ateş eder; oyuncunun tek işi parmağıyla gemiyi sürüklemek, mermi
yağmurundan sıyrılmak ve her seviye atlayışta sunulan **3 kart** arasından doğru olanı
seçmektir. Bir koşu (run) **8–10 dakika** sürer; 5 dalga bölümü, 2 mini-boss ve 1 final
boss içerir. Koşu sonunda kazanılan altın ve Yıldız Tozu ile ana menüde kalıcı
geliştirmeler yapılır.

**Tasarım sütunları (pillars):**

| # | Sütun | Anlamı | Karar testi |
|---|-------|--------|-------------|
| P1 | **Parmağın ucunda ustalık** | Hareket tek beceri girdisidir; ölüm her zaman oyuncunun "görebildiği" bir hatadan olmalı | Okunamayan mermi/ani spawn → reddet |
| P2 | **Her kart bir hikâye** | Her seviye atlama anlamlı bir karar; 3 seçenekten en az biri "vay!" dedirtmeli | Tek başına +%3 gibi sönük kart → reddet |
| P3 | **Tatlı kaos** | Ekran kalabalık ama göz yormaz; patlamalar konfeti, ölümler "pıt" sesi | Kan, sert kontrast, agresif flaş → reddet |
| P4 | **Hep bir adım kaldı** | Her koşu sonunda oyuncu bir sonraki kilide/yükseltmeye yakın hissetmeli | Boş biten koşu ekranı → reddet |

---

## 2. Player Fantasy (Oyuncu Fantezisi)

*"Minicik bir tavşanla başlıyorum, 8 dakika sonra ekranı gökkuşağı lazerleri, dönen sakız
gezegenleri ve şimşek kedilerle dolduran bir kıyamet makinesiyim — ama hâlâ çok tatlıyım."*

- **İlk 60 sn:** Güçsüzlük + merak (tek düz atış, hızlı ilk level-up'lar).
- **2–5. dk:** Build kimliği oluşur ("ben bumerang+şimşek build'iyim").
- **5–8. dk:** Evrim anı — silah dönüşür, ekran patlar, güç fantezisi zirvede.
- **Boss:** Beceri sınavı; build'in işe yarayıp yaramadığı ortaya çıkar.
- **Koşu sonu:** "Bir el daha" — kaybetsem bile ilerledim, bir sonraki kilit çok yakın.

---

## 3. Detailed Rules (Detaylı Kurallar)

### 3.1 Kontrol ve kamera

- **Portre 9:19.5** hedef, güvenli alan (safe area) destekli. Oyun alanı sabit genişlikte
  (10 dünya birimi), arka plan paralaks ile sürekli aşağı kayar (hissedilen hız 2 birim/sn).
- **Göreli sürükleme (relative drag):** Parmak ekranın herhangi bir yerine konur; gemi,
  parmağın *hareket farkı* kadar hareket eder (`hassasiyet = 1.25`). Gemi parmağın altında
  kalmaz → oyuncu gemiyi her zaman görür.
- Gemi, ekranın alt %65'lik bölümüyle sınırlıdır; üst %35 düşman bölgesidir.
- **Hitbox:** Gemi sprite'ı ~1.0 birim, gerçek hitbox **0.18 birim yarıçaplı daire**
  (ortada parlayan kalp ile görselleştirilir). Mermiler de görselinden %30 küçük hitbox'a sahip.
- **Otomatik ateş** sürekli açıktır. Parmak kaldırıldığında oyun **duraklamaz** ama
  zaman **%30'a yavaşlar** (bullet-time yok; sadece telefonu tutuşu değiştirmek için nefes
  payı). Duraklatma: sağ üst buton.

### 3.2 Koşu (run) yapısı — 1 bölüm

| Zaman | Olay | Amaç |
|-------|------|------|
| 0:00–1:30 | Dalga 1 (civciv sürüleri, Chicken Invaders formasyonları) | Isınma, 3–4 hızlı level-up |
| 1:30–3:00 | Dalga 2 + **Altın Yumurta Sandığı** | İlk pasif, ilk sinerji |
| 3:00 | **Mini-boss 1** (Dev Jöle Ayı) | Sandık: garanti silah seviyesi |
| 3:00–6:00 | Dalga 3–4 (karışık formasyon + serbest dolaşan sürü) | Build derinleşir |
| 6:00 | **Mini-boss 2** | Sandık: **evrim** şansı (koşul sağlanıyorsa garanti) |
| 6:00–8:00 | Dalga 5 "Kaos" (yoğunluk zirvesi) | Güç fantezisi |
| 8:00+ | **Final Boss** (3 fazlı) | Beceri sınavı |
| Boss sonrası | İsteğe bağlı **"Sonsuz Mod"** (+%50 ödül çarpanı, ölene kadar) | Risk/ödül |

Dünya haritası: **Takımyıldızlar** (bölümler) → her takımyıldızda 5 bölüm; 5. bölüm "Kraliçe"
boss'u. Her tamamlanan bölüm bir sonrakini ve "Zor" versiyonunu açar.

### 3.3 Düşmanlar ve formasyonlar

İki spawn katmanı birlikte çalışır:

1. **Formasyon katmanı (Chicken Invaders):** Ekranın üstünde ızgara formasyonları
   (V, dalga, spiral, kutu). Belirli ritimde yumurta/mermi bırakırlar. Tamamı temizlenince
   "Formasyon Temizlendi!" bonusu (XP yağmuru + ses).
2. **Sürü katmanı (Vampire Survivors):** Kenarlardan ve yukarıdan sürekli akan, oyuncuya
   yönelen küçük düşmanlar. Yoğunluk zamanla artar; build'in alan temizleme gücünü test eder.

Düşman arketipleri (tamamı chibi): **Civciv** (düz iner), **Jöle Ayı** (yavaş, tank, ikiye
bölünür), **Kurabiye Robot** (nişanlı atış), **Dondurma Kulesi** (mermi spiralleri), **Sakız
Balonu** (patlayınca halka mermi), **Elit** (altın taçlı versiyonlar; sandık düşürür).

### 3.4 Oyun içi gelişim (rogue-lite)

- Öldürülen düşman **Tecrübe Şekeri** (XP gem) düşürür: mavi = 1, yeşil = 5, pembe = 25.
  Ekrandaki gem sayısı 250'yi aşarsa yeni gem'ler en yakınıyla birleşir (performans + okunabilirlik).
- Gem'ler mıknatıs yarıçapında (`1.5` birim, pasif ile artar) gemiye uçar.
- **Seviye atlama:** Oyun durur, **3 kart** sunulur (silah / pasif / altın-can). Oyuncu birini
  seçer. **Yeniden çek (reroll)** ve **yasakla (banish)** hakları meta ilerlemeden gelir.
- **Envanter:** Maks. **4 silah + 4 pasif** (Vampire Survivors'ın 6+6'sından az: mobil ekran
  okunabilirliği ve daha net build kimliği için).
- **Evrim:** Silah Sv.5 + eşleşen pasif envanterde → bir sonraki boss/elit sandığında silah
  evrime uğrar. Detay: `design/gdd/weapon-system.md`.
- **Eşya düşüşleri:** Mıknatıs (tüm gem'leri çeker), Kalp (+%20 can), Bomba (ekranı temizler,
  boss'a %5), Altın kese.

### 3.5 Can, ölüm ve ikinci şans

- Can puanı (HP) sistemi: temel 100 HP; düşman mermisi 10–25 hasar. Hasar sonrası **1.2 sn
  dokunulmazlık** (gemi yanıp söner).
- Ölüm → "Devam et?" ekranı: meta'dan kazanılan **Diriliş** hakkı (0–2) veya ödüllü reklam
  (koşu başına 1). Reklam asla zorunlu değildir.
- Ölümde toplanan altının **%100'ü** korunur (kayıp korkusunu değil, ilerleme hissini ödüllendir);
  bölüm tamamlama bonusu ve Yıldız Tozu yalnızca boss yenilince verilir.

---

## 4. Oyun Döngüsü ve Psikoloji — "Bir El Daha" Tasarımı

### 4.1 İç içe üç döngü

```
┌──────────────────────── META DÖNGÜ (günler/haftalar) ────────────────────────┐
│  Altın/Yıldız Tozu harca → kalıcı güç + yeni silah/karakter kilidi →         │
│  daha derin bölümler → yeni düşman/boss → yeni kilit hedefleri               │
│   ┌──────────────── KOŞU DÖNGÜSÜ (8–10 dk) ────────────────┐                 │
│   │  Dalga → mini-boss → build kur → evrim → boss → ödül   │                 │
│   │   ┌──────── MİKRO DÖNGÜ (5–20 sn) ────────┐             │                 │
│   │   │ Kaç → vur → gem topla → level-up →    │             │                 │
│   │   │ 3 kart seç → daha güçlü vur           │             │                 │
│   │   └───────────────────────────────────────┘             │                 │
│   └─────────────────────────────────────────────────────────┘                │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Psikolojik kaldıraçlar (ve nasıl uyguladığımız)

| Kaldıraç | Uygulama | Neden işe yarar |
|----------|----------|-----------------|
| **Değişken oranlı ödül** | Kart çekilişleri, elit sandıkları (sandık açılışında slot-makinesi animasyonu; 1/3/5 ödül) | Tahmin edilemeyen ödül dopamin beklentisini canlı tutar |
| **Sık ve hızlı ilk ödüller** | İlk 60 sn'de 3–4 level-up | İlk dakikada "yetkinlik" hissi; erken bırakmayı önler |
| **Yetkinlik (competence)** | Kıl payı kaçış (graze) → "Kıl Payı!" yazısı + küçük XP bonusu | Beceriyi görünür kılar, risk almaya teşvik eder |
| **Özerklik (autonomy)** | Her kartta anlamlı seçim; reroll/banish; build arketipleri | Kendi stratejim → sahiplenme |
| **Tamamlanmamış iş (Zeigarnik)** | Koşu sonu ekranı: "Tüy Blaster Ustalığı 80/100", "Yeni karaktere 120 altın" ilerleme çubukları | Açık kalan hedefler geri dönmeye iter |
| **Yakın kayıp (near-miss)** | Boss %10 canla kaldıysa ekranda "Çok az kaldı! Boss %8 canla kaldı" | "Bir dahakine kesin" hissi |
| **Güç fantezisi zirvesi** | Evrim anı: 0.6 sn zaman durur, silah dönüşüm animasyonu, ekran konfeti | Koşunun unutulmaz anı; paylaşılabilir |
| **Kayıptan korunma** | Ölümde altın korunur | Ceza değil ilerleme; hayal kırıklığını azaltır |
| **Kısa oturum** | 8–10 dk koşu, her an güvenli çıkış (arka plana alınca otomatik duraklat + koşu kaydı) | Mobil "otobüs durağı" oturumu |

> **Etik çizgi:** Bağımlılık yapıcılığı *anlamlı eğlence* ile sağlıyoruz. Loot box'ta gerçek
> para yok, zamanlı enerji sistemi yok, ödüllü reklam hep isteğe bağlı, çocuklara yönelik
> pazarlama kurallarına (COPPA / GDPR-K) uyum.

### 4.3 Zorluk eğrisi — "Testere dişi" (sawtooth)

Koşu içi yoğunluk düz artmaz; **gerilim → rahatlama** dalgaları halinde yükselir:

```
Yoğunluk
  ▲                                                   ████ Final Boss
  │                                   ▲ MB2      ▄▄▄███
  │                   ▲ MB1         ▄█▌  ▄▄▄▄████
  │                 ▄█▌   ▄▄▄▄▄▄████  ▌ ▀ (sandık/nefes)
  │      ▄▄▄▄▄████▀  ▌ ▀ (sandık)
  │ ▄▄███
  └────────────────────────────────────────────────────────▶ Zaman
   0:00        3:00                  6:00          8:00
```

- Her mini-boss sonrası **8–10 sn "nefes" penceresi**: spawn yok, gem yağmuru, sandık.
- Oyuncu gücü (build DPS) ile düşman HP'si arasındaki oran, koşunun ilk %70'inde oyuncu
  lehine **yavaşça açılır** (güç fantezisi), son %30'da boss ile **kapanır** (sınav).

### 4.4 Dinamik zorluk ayarı (DDA) — görünmez yardım

- Oyuncu son 60 sn'de 2+ kez hasar aldıysa: spawn yoğunluğu **−%10** (en fazla −%25).
- Oyuncu 90 sn hasar almadıysa: yoğunluk **+%5** (en fazla +%15).
- Bir bölümde **3 ardışık başarısızlık** → sonraki denemede "Pıtır'ın Tılsımı" (+%10 hasar)
  önerilir (reddedilebilir). *Açıkça gösterilir* — gizli yardım güveni zedeler.

### 4.5 Koşu sonu ekranı (en kritik ekran)

Sıralama bilinçlidir — önce his, sonra ödül, sonra hedef:

1. **Öne çıkan an:** En çok hasar veren silah + evrim animasyonunun tekrarı (1 sn).
2. **Ödül sayacı:** Altın ve Yıldız Tozu "şıngırdayarak" sayılır (DOTween sayı tween'i).
3. **İlerleme çubukları:** Silah ustalıkları, günlük görev, sonraki kilit.
4. **Tek büyük CTA:** "Tekrar Oyna" (büyük, pembe, zıplayan) + küçük "Geliştir" butonu.
   Eğer oyuncu bir yükseltmeyi karşılayabiliyorsa → "Geliştir" butonunda parlayan **"!"**.

---

## 5. Formulas (Formüller)

**XP eşiği** — seviye `n`'den `n+1`'e geçmek için gereken XP:

```
XP(n) = floor( 5 + 6·n + 0.9·n^1.7 )
```

| n | 1 | 2 | 3 | 5 | 10 | 15 | 20 | 25 |
|---|---|---|---|---|----|----|----|----|
| XP | 11 | 19 | 28 | 48 | 110 | 184 | 271 | 369 |
| Kümülatif | 11 | 30 | 58 | 144 | 563 | 1330 | 2508 | 4152 |

İlk 60 sn'de ~60 civciv (1 XP) → seviye 4. Seviye 25 için ~4150 XP gerekir; bu da 8 dakikada
ortalama **~8.6 XP/sn** demektir. Gem değerleri ve spawn bütçesi bu hedefe göre ayarlanır.

Hedef: 8 dakikalık tipik koşuda seviye **24–28** (≈ 25 kart seçimi; 8 slot × maks 5 seviye
+ evrimler için yeterli ama "her şeyi alamazsın" gerginliği korunur).

**Düşman HP ölçeklemesi** (koşu içi, `t` = dakika, `c` = bölüm indeksi 0'dan):

```
HP(t, c) = HP_base · (1 + 0.25·t + 0.035·t²) · (1.22)^c
```

**Spawn bütçesi** (saniye başına "tehdit puanı"):

```
Budget(t) = 1.5 + 0.9·t + 0.08·t² ,  DDA ile ×[0.75 … 1.15]
```

> 2026-09-24 ayarı (QA koşuları 12–15 ve cihaz testi): bütçe 2.0 → 1.5 (açılış çok ölümcüldü);
> HP ölçeği 0.18·t + 0.012·t² → 0.25·t + 0.035·t² (geç oyunda düşmanlar ekrana girmeden eriyordu).
> Ekran dışındaki düşmanlar vurulamaz ve ateş edemez.

**Kart ağırlığı:** Her kart için `w = rarityWeight × (sahipse 1.6 : 1.0) × (evrim yolundaysa 1.4 : 1.0)`.
Nadirlik ağırlıkları: Yaygın 60, Nadir 28, Epik 10, Efsane 2. Aynı teklifte aynı kart iki kez
çıkamaz. Sahip olunan silahın ağırlık artışı, build'in odaklanmasına yardım eder.

**Evrim acıma sayacı (pity):** Evrim koşulu sağlandıktan sonra açılan **ilk** boss/elit sandığı
evrimi garanti eder.

---

## 6. Edge Cases (Uç Durumlar)

| Durum | Çözüm |
|-------|-------|
| Envanter tamamen dolu ve maks seviye | Kartlar "Altın +50" / "Can +30" / "Kalıcı +%1 hasar (bu koşu)" ile doldurulur |
| Aynı karede birden fazla level-up | Kuyruk; her biri ayrı taslak ekranı, arada 0.3 sn nefes. "Hepsini otomatik seç" ayarı (meta'dan açılır) |
| Level-up ekranı açıkken arka plana alma | Oyun zaten duraklı; koşu durumu kaydedilir |
| Uygulama öldürülürse (OS) | Her dalga sonunda koşu anlık görüntüsü (snapshot) diske yazılır; geri dönüşte "Devam et" |
| Parmak ekrandan çıkarsa/kenar hareketi | Göreli sürükleme; OS kenar jestleri için alt 24 px ölü bölge |
| Aynı anda 2 evrime hak kazanma | Sandık ikisini de verir (nadir, "JACKPOT" animasyonu) |
| Ekranda 250+ gem | Birleştirme (merge) kuralı; toplam XP korunur |
| Boss ölürken oyuncu ölürse | Oyuncu lehine: boss ölümü önceliklidir |
| Düşük FPS cihaz | VFX kalite kademesi otomatik düşer (bkz. mimari §7) |

## 7. Dependencies (Bağımlılıklar)

- **Silah Sistemi** (`weapon-system.md`) — kart havuzu, evrim koşulları
- **Meta Ekonomi** (`meta-economy.md`) — başlangıç statları, reroll/banish/diriliş hakları, kilitler
- **Sanat Yönetimi** (`../art-bible.md`) — okunabilirlik kuralları mermi/düşman tasarımını kısıtlar
- **Teknik Mimari** (`../../docs/architecture/architecture.md`) — mermi/düşman bütçeleri

## 8. Tuning Knobs (Ayar Düğmeleri)

| Düğme | Varsayılan | Güvenli aralık | Etki |
|-------|-----------|----------------|------|
| `drag.sensitivity` | 1.25 | 1.0–1.6 | Gemi hareket hızı |
| `player.hitboxRadius` | 0.18 | 0.12–0.25 | Zorluk/adalet hissi |
| `player.iFrames` | 1.2 sn | 0.8–1.6 | Hata toleransı |
| `run.lengthMinutes` | 8 | 6–12 | Oturum süresi |
| `xp.curve` (5, 6, 0.9, 1.7) | — | — | Level-up sıklığı |
| `draft.choices` | 3 | 3–4 | Seçim zenginliği |
| `inventory.weapons/passives` | 4/4 | 3–6 | Build derinliği |
| `dda.maxReduction` | %25 | %0–40 | Yardım miktarı |
| `gems.mergeThreshold` | 250 | 150–400 | Performans/okunabilirlik |

## 9. Acceptance Criteria (Kabul Kriterleri)

- [ ] Yeni oyuncu ilk 60 sn içinde en az **3** level-up yaşar (telemetri medyanı).
- [ ] Tipik koşu 7–10 dk sürer; boss'a ulaşan koşularda ortalama seviye 22–30.
- [ ] Playtest'te ölümlerin **≥%90**'ı için oyuncu "neden öldüğümü gördüm" der.
- [ ] Koşuların **≥%60**'ında en az bir evrim gerçekleşir (bölüm 1 hariç).
- [ ] Koşu sonu ekranından "Tekrar Oyna"ya geçiş oranı **≥%55** (D1 kohortu).
- [ ] Hedef KPI: D1 ≥ %40, D7 ≥ %15, ortalama oturumda ≥ 2.5 koşu.
- [ ] Orta segment cihazda (ör. Snapdragon 680 / 4 GB) 800 mermi + 150 düşmanla **60 FPS**.
