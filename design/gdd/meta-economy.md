# Kalıcı Gelişim (Meta-Progression) Ekonomisi GDD

> Bağlı: `game-concept.md` §4 (psikoloji), `weapon-system.md` (silah kilitleri)

## 1. Overview

Ana menü **"Pamukşeker Üssü"** — oyuncunun her koşudan sonra döndüğü, harcama yapmaya
sabırsızlandığı yer. Beş harcama noktası (sink), üç para birimi ve **"her dönüşte en az bir
şey satın alınabilir"** ilkesi üzerine kurulu.

## 2. Player Fantasy

"Her koşu, üssüme bir tuğla daha koyuyor." Oyuncu ana menüye döndüğünde bir sonraki
alımını çoktan planlamıştır — koşu sonu ekranı ona tam olarak ne kadar kaldığını göstermiştir.

## 3. Detailed Rules

### 3.1 Para birimleri

| Birim | Kaynak | Harcama | Rol |
|-------|--------|---------|-----|
| 🪙 **Altın** (yumuşak) | Her koşu (ölümde de korunur), günlük görev, formasyon bonusları | Atölye statları, silah kilitleri | Ana ilerleme motoru; bol, sürekli |
| ✨ **Yıldız Tozu** (materyal) | Sadece **boss yenince** (3–8), elit sandıkları (%10), haftalık görev | Karakterler, Takımyıldız yetenek tahtası, evrim kitapları | "Başarı" parası; nadir, beceri ödülü |
| 💎 **Elmas** (premium, isteğe bağlı) | Başarımlar, IAP | **Sadece kozmetik** + koşu devam (reklam alternatifi) | Pay-to-win yok |

### 3.2 Gelir modeli (koşu başına ortalama altın)

```
Altın_koşu = (Σ düşman altını + formasyon bonusları) × (1 + Şans%) × Bölüm çarpanı
Bölüm çarpanı(c) = 1.35^c        (c = takımyıldız indeksi, 0'dan)
```

| Takımyıldız | Ölümle biten koşu | Boss yenilen koşu | Yıldız Tozu (boss) |
|-------------|------------------|-------------------|--------------------|
| 1 — Şekerkamışı | ~150 | ~250 | 3 |
| 2 — Jöle Nebulası | ~200 | ~340 | 4 |
| 3 — Kurabiye Kuşağı | ~275 | ~455 | 5 |
| 4 — Dondurma Kutbu | ~370 | ~615 | 6 |
| 5 — Sakız Galaksisi | ~500 | ~830 | 7 |
| 6 — Kraliçenin Yuvası | ~670 | ~1120 | 8 |

### 3.3 Beş harcama noktası

#### A) 🔧 Atölye — kalıcı statlar (Altın)

Maliyet formülü: `Maliyet(L) = round10( Taban × Büyüme^(L−1) )`

| Yükseltme | Etki/seviye | Maks | Taban | Büyüme | Seviye maliyetleri | Toplam |
|-----------|------------|------|-------|--------|--------------------|--------|
| ❤️ Can | +%8 maks can | 10 | 80 | 1.32 | 80 → 970 | 3.760 |
| ⚔️ Hasar | +%5 hasar | 10 | 100 | 1.35 | 100 → 1.490 | 5.470 |
| 🔫 Atış Hızı | −%3 bekleme | 10 | 120 | 1.36 | 120 → 1.910 | 6.870 |
| 🧲 Mıknatıs | +%15 toplama | 5 | 60 | 1.30 | 60 → 170 | 540 |
| 🛡️ Zırh | −1 alınan hasar | 5 | 150 | 1.40 | 150 → 580 | 1.640 |
| 🍀 Şans | +%5 nadir kart, +%5 altın | 5 | 200 | 1.40 | 200 → 770 | 2.190 |
| 📚 Tecrübe | +%4 XP | 5 | 250 | 1.45 | 250 → 1.110 | 3.010 |
| 🔄 Yeniden Çek | +1 reroll/koşu | 3 | 300 | 1.80 | 300 → 970 | 1.810 |
| 🚫 Yasakla | +1 banish/koşu | 3 | 400 | 1.80 | 400 → 1.300 | 2.420 |
| 🐣 Diriliş | +1 diriliş/koşu | 2 | 1.500 | 3.00 | 1.500 → 4.500 | 6.000 |
| | | | | | **Atölye toplamı** | **≈ 33.700** |

**İlk koşu kancası:** 1. koşu ~150–250 altın getirir → Can Sv.1 (80) + Hasar Sv.1 (100)
**+ Mıknatıs Sv.1 (60)** — yani ilk dönüşte **2–3 alım** yapılır. Yeni oyuncu üssün
"yaşayan" bir yer olduğunu hemen öğrenir.

#### B) 🐰 Hangar — karakterler (Yıldız Tozu + Altın)

Her karakter farklı başlangıç silahı + benzersiz pasif = yeni oyun tarzı (en güçlü
"bir el daha" motoru: *yeni oyuncak*).

| Karakter | Başlangıç silahı | Benzersiz pasif | Kilit |
|----------|------------------|-----------------|-------|
| Pıtır (tavşan) | Tüy Blaster | Her 10 seviyede +1 kart seçeneği | Başlangıç |
| Cıvık (civciv) | Yumurta Havanı | Patlamalar %20 büyük, can −%10 | 600 🪙 + 5 ✨ |
| Mırnav (kedi) | Kıvılcım Kedi | Sersemletme süresi ×2 | 1.500 🪙 + 12 ✨ |
| Balonbaş (hamster) | Sakız Balonu | Yuttuğu her mermi 1 can | 3.000 🪙 + 20 ✨ |
| Yıldızpati (tilki) | Yıldız Bumerang | Her evrim +%15 hasar | 5.000 🪙 + 35 ✨ |
| ??? (gizli) | Rastgele | Her koşu rastgele pasif | Tüm bölümleri "Zor"da bitir |

#### C) 🧪 Silah Laboratuvarı — kart havuzunu genişlet (Altın)

Başlangıçta havuzda 3 silah (Tüy, Yumurta, Kıvılcım) + 4 pasif. Diğerleri laboratuvardan
açılır: Yıldız Bumerang (400), Sakız Balonu (700), kalan pasifler (250–500). Yeni içerik
güncellemeleri buraya eklenir. **Evrim Kitapları** (✨ 5–10): evrimi ilk kez görmek için
"ansiklopedi" girdisi açar; açılmadan evrim olmaz → koşu sonu hedefi.

#### D) ⭐ Takımyıldız Tahtası — yetenek ağacı (Yıldız Tozu)

Yıldız haritası şeklinde 30 düğümlü tahta; her düğüm 2–10 ✨. Küçük ama *hissedilen*
kurallar değiştirir: "Level-up'ta 4 kart göster", "Elitlerden %25 daha fazla altın",
"Evrim sandığı 1 ek pasif seviyesi verir", "Kıl payı kaçış XP'si ×2". Düğümler dallanır —
oyuncu kendi yolunu seçer (tam doldurma ≈ 150 ✨ ≈ 30 boss zaferi).

#### E) 🏅 Ustalık ve Kozmetik

- **Silah ustalığı:** Silahla verilen hasar ustalık puanı biriktirir (ödeme gerektirmez).
  Kademeler: kozmetik mermi rengi → +%3 hasar → altın çerçeveli kart → özel evrim VFX'i.
- **Kozmetik:** Gemi derileri, iz efektleri, patlama temaları (konfeti / kalpler / yıldızlar).
  Elmas ile veya başarımlarla. **Oynanışa etkisi sıfır.**

### 3.4 "Sabırsızlanma" tasarımı — oyuncu neden hemen harcamak ister?

1. **Hedef önceden bildirilir:** Koşu sonu ekranı en yakın 2 hedefi gösterir
   ("Mırnav'a 340 🪙 + 4 ✨ kaldı"). Ana menüye dönünce ilgili buton **parlar**.
2. **Her dönüşte en az bir alım:** Tüm oyunda bir yükseltmenin fiyatı hiçbir zaman ortalama
   2 koşu gelirinden fazla olmamalı (Diriliş hariç — o bir "büyük hedef").
3. **Hissedilen alım:** Her satın alma anında görsel geri bildirim verir (karakter maketi
   büyür, tezgahta dönen kalp parlar) — sayı yerine *his*.
4. **Katmanlı hedefler:** Kısa (sonraki Atölye seviyesi), orta (yeni karakter), uzun
   (Takımyıldız tahtası + gizli karakter). Her an üçü de görünür.
5. **"Bir tane daha" fiyatlaması:** Kilitler, koşu gelirinin 1.2–1.8 katına yerleştirilir →
   genelde "bir koşu daha" ile ulaşılır.

## 4. Formulas

```
Atölye_maliyeti(L)  = round10( Taban × Büyüme^(L−1) )
Gelir_koşu(c)       ≈ 250 × 1.35^c   (boss yenildiğinde)
Alım_sıklığı_hedef  = Maliyet_sıradaki / Gelir_koşu  ∈ [0.5 , 2.0]
```

**Tempo simülasyonu (ortalama oyuncu, oturumda 3 koşu):**

| Oynanan süre | Kümülatif altın | Durum |
|--------------|-----------------|-------|
| 1. gün (6 koşu) | ~1.400 | Atölye ilk kademeler, Cıvık yakın |
| 1. hafta (~40 koşu) | ~14.000 | 2–3 karakter, takımyıldız 3 |
| 1. ay (~120 koşu) | ~65.000 | Atölye ~%90, takımyıldız 5–6 |
| Endgame | — | Zor mod, ustalık, sezonluk etkinlikler, sonsuz mod liderlik tablosu |

## 5. Edge Cases

- Atölye maks → altın fazlası **Altın Heykel** (kozmetik, üs dekorasyonu) ve etkinlik
  dükkânına akar; altın asla anlamsızlaşmaz.
- İade (respec): Atölye ücretsiz sıfırlanabilir (deneme özgürlüğü); Takımyıldız tahtası
  %100 iade ile 24 saatte bir sıfırlanabilir.
- Kayıt bozulması: yerel JSON + bulut (Unity Cloud Save); çakışmada "daha fazla ilerleme
  puanı olan" kayıt korunur. Hile önleme için kayıt HMAC imzalı (bkz. mimari §9).

## 6. Dependencies

`game-concept.md` (koşu ödülleri, DDA), `weapon-system.md` (kilitler, evrim kitapları),
`architecture.md` §9 (SaveService).

## 7. Tuning Knobs

`MetaUpgradeDefinition` ScriptableObject'lerinde: `Taban`, `Büyüme`, `Maks`, `Etki/seviye`.
Global: `Bölüm çarpanı (1.35)`, `Ölüm altın koruma oranı (%100)`, `Boss ✨ ödülü`.

## 8. Acceptance Criteria

- [ ] İlk koşudan sonra oyuncu en az **2** yükseltme alabilir.
- [ ] İlk 7 gün boyunca, oyuncuların ≥%80'i her 1–2 koşuda bir alım yapar (telemetri).
- [ ] Hiçbir tek Atölye yükseltmesi oyuncu gücünü >%5 artırmaz (meta, beceriyi gölgelemez).
- [ ] Tam meta ilerlemeli oyuncu, yeni oyuncuya göre bölüm 1'i en fazla **~2.2×** daha hızlı bitirir.
- [ ] Hiçbir para birimi bakiyesi ortalama oyuncuda 3 günden uzun süre "harcanamaz" durumda kalmaz.
