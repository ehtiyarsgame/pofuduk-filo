# Sanat Yönetimi Brief'i (Art Bible) — Pofuduk Filo

> Amaç: **"Tatlı kaos"** — ekranda yüzlerce nesne varken bile göz yormayan, sempati
> uyandıran, dokunulası bir dünya. Bu belge tüm görsel üretimi kısıtlayan kuralları tanımlar.

## 1. Görsel Kimlik Özeti

| Anahtar | Karar |
|---------|-------|
| Stil | **Chibi / kawaii + "oyuncak" hissi** — yumuşak vinil figürler, şeker kaplama |
| Referanslar | *Kirby*, *Animal Crossing* renkleri, *Ooblets*, *Cult of the Lamb* (tatlı-kaos dengesi), *Archero* UI netliği |
| Anahtar kelimeler | Yuvarlak, zıplayan, parlak, pofuduk, şekerli, dokunulası |
| Kaçınılacaklar | Keskin köşeler, kan, siyah dolgulu yüzeyler, neon-cyberpunk agresifliği, gerçekçi doku |
| Teknik | 2D sprite (512 px kaynak, 128–256 px oyun içi), **koyu mor (#3B2A4F) 3–4 px dış hat**, cel-shading tek gölge + tek parlama |

## 2. Renk Paleti

### 2.1 Ana palet (pastel ama doygun — "süt katılmış şeker")

| Rol | Renk | Hex |
|-----|------|-----|
| Gökyüzü üst | Lavanta gecesi | `#6B5B95` |
| Gökyüzü alt | Pembe şafak | `#F7A6C1` |
| Oyuncu vurgu | Nane | `#7FE0C4` |
| Oyuncu vurgu 2 | Krem sarı | `#FFE8A3` |
| Dost mermi | Beyaz-nane, **%70 opaklık** | `#DFFFF4` |
| **Düşman mermisi (gövde)** | Sıcak pembe-magenta | `#FF4F9A` |
| **Düşman mermisi (çekirdek)** | Saf beyaz | `#FFFFFF` |
| **Düşman mermisi (kontur)** | Koyu erik | `#3B2A4F` |
| Tehlike / boss özel saldırı | Mercan kırmızısı + beyaz kenar | `#FF6B5E` |
| XP gem | Mavi / Yeşil / Pembe | `#6EC6FF` / `#8BE28B` / `#FF9EDB` |
| Altın | Bal | `#FFC84A` |
| Dış hat (global) | Koyu erik | `#3B2A4F` |

### 2.2 Değer (value) hiyerarşisi — okunabilirliğin sırrı

Parlaklık katmanları kesin olarak ayrılmalıdır (gri tonlamaya çevrilmiş ekran testi zorunlu):

```
EN YÜKSEK KONTRAST  →  Düşman mermileri (beyaz çekirdek + koyu kontur)
                    →  Oyuncu hitbox'ı (parlayan kalp)
                    →  Düşmanlar (doygun, koyu dış hatlı)
                    →  Oyuncu gemisi
                    →  XP gem'leri / eşyalar
                    →  Oyuncu mermileri (soluk, yarı saydam, dış hatsız)
EN DÜŞÜK KONTRAST   →  Arka plan (düşük doygunluk, bulanık paralaks, %60 parlaklık)
```

> **Altın kural:** Oyuncunun kendi mermileri *asla* düşman mermilerinden daha dikkat çekici
> olamaz. Evrimleşmiş silahlarda bile oyuncu VFX'i **toplam ekran parlaklığının %40'ını**
> geçemez (VFX yoğunluğu otomatik soluklaştırılır — bkz. mimari §7).

### 2.3 Renk körlüğü

Düşman mermileri renk + **şekil** ile ayrılır (pembe **yuvarlak/kalp/yıldız**); oyuncu
mermileri **uzun/tüy** şekilli. Deuteranopi/protanopi filtreleri ile test edilir; ayarlarda
"Yüksek kontrast mermi" seçeneği (düşman mermisi sarı-siyah).

## 3. Karakter ve Düşman Tasarımı

### 3.1 Oranlar ve şekil dili

- **Baş/gövde oranı 1 : 1.2** (chibi); gözler yüzün **%35–40**'ı, göz bebeğinde 2 parlama.
- Şekil dili: **daire = dost/sevimli**, **yumuşatılmış üçgen = tehlike** (düşman silah
  namluları, boss boynuzları). Tüm köşeler min. 8 px yarıçaplı.
- **Silüet testi:** Her düşman, siyah silüet halindeyken 64 px'de tanınabilmeli.

### 3.2 Düşman "kötülüğü" — sevimli ama rakip

Düşmanlar kötü değil, **aç ve yaramaz**. İfadeler: kararlı kaşlar, dışarı çıkmış dil,
salya damlası (atıştırmalık açlığı). Ölünce *acı çekmezler* — "pıt" diye patlayıp şaşkın bir
yüzle yıldızlarla uçarlar (sayılar ve konfetiyle).

| Düşman | Tasarım notu | Animasyon |
|--------|-------------|-----------|
| Civciv | Sarı top, pilot gözlüğü, minik pervane | Kanat çırpma 4 kare, formasyonda sağa-sola sallanma |
| Jöle Ayı | Yarı saydam, içinde görülen şeker kalp | Squash & stretch zıplama; vurulunca titreme |
| Kurabiye Robot | Çikolata parçası "vidalar", anten | Nişan alırken göz kısar (telegraph!) |
| Dondurma Kulesi | 3 top, her top farklı renk ve atış deseni | Atıştan önce 0.4 sn erime/şişme |
| Sakız Balonu | Şişen yüz, yanakları kırmızı | Patlamadan 0.5 sn önce şişer |
| Elit | Altın taç + ışıltı partikülleri | Ek parlak kontur |
| Boss: Kraliçe Tavuk | Dev, taçlı, yastık gibi pofuduk | Faz değişiminde "sinirlenir", yanakları kızarır |

**Telegraph (önceden haber) kuralı:** Her düşman saldırısından önce **≥ 0.35 sn** görsel +
işitsel ipucu. Boss özel saldırıları ≥ 0.8 sn ve ekranda yarı saydam **tehlike bölgesi**.

## 4. Arayüz (UI) Hissi — "Jöle UI"

- **Şekiller:** Kalın yuvarlatılmış dikdörtgenler (yarıçap 24–32 px), alt kenarda 6 px koyu
  "kalınlık" (3D şeker tablet hissi). Dokunulunca butonun "kalınlığı" kaybolur (basılma).
- **Hareket:** Her şey esnek. Buton basma → ölçek 0.92 → 1.06 → 1.0 (DOTween
  `Ease.OutBack`). Paneller alttan zıplayarak gelir (`OutBack`, 0.35 sn). Hiçbir UI öğesi
  "düz" belirmez.
- **Tipografi:** Yuvarlak, kalın, geniş (ör. *Baloo 2*, *Fredoka*; Türkçe karakter desteği
  şart: ğ, ş, ı, İ). Sayılar tabular. Kontur + hafif alt gölge.
- **Level-up kartları:** Tarot/oyun kartı gibi; nadirliğe göre çerçeve (Yaygın beyaz, Nadir
  mavi, Epik mor, Efsane gökkuşağı animasyonlu). Kartlar sırayla **düşer ve zıplar**
  (0.08 sn aralıkla); efsane kart çıkınca ekran hafif titreşir + "tın" sesi.
- **HUD (oyun sırasında) minimal:** Üstte ince XP çubuğu (dolarken şekerli parıltı),
  sol üstte can kalpleri, sağ üstte duraklat. Oyuncunun baş parmağının olduğu alt bölgede
  **hiç** UI yok.
- **Sayılar:** Hasar sayıları küçük, zıplayan, kritikte büyük + sarı; yüksek yoğunlukta
  birleştirilir (aynı düşmana 0.2 sn içindeki vuruşlar tek sayı).
- **Erişilebilirlik:** Min. dokunma alanı 48×48 dp; "Ekran titremesi" ve "Flaş efektleri"
  kapatılabilir; UI ölçeği %90–120.

## 5. VFX — Patlama ve Vuruş Hissi

### 5.1 Vuruş hissi (juice) katmanları

| Katman | Uygulama | Değer |
|--------|----------|-------|
| **Hit flash** | Vurulan düşman sprite'ı 1 kare beyaza döner (shader `_FlashAmount`) | 0.05 sn |
| **Squash** | Vurulunca ölçek (1.15, 0.88) → 1 | 0.08 sn |
| **Knockback** | Küçük düşmanlarda 0.05 birim itme | — |
| **Hitstop** | Sadece elit/boss öldürmede zaman ölçeği 0.05 | 40–60 ms |
| **Ekran sarsıntısı** | Sadece büyük olaylarda (evrim, boss faz, bomba); Perlin tabanlı, düşük frekans | genlik ≤ 0.15 birim |
| **Titreşim (haptic)** | Hafif (iOS "light impact"), kendi hasarında orta | Ayarlarla kapatılabilir |
| **Ses** | Her ölüm "pıt/pop" — **perde rastgele ±%8**, aynı karede maks 4 ses | — |

### 5.2 Patlama tasarımı — "Şeker Konfetisi"

Bir küçük düşman ölümü (toplam **≤ 0.45 sn**, ekrandan hızla temizlenir):

1. **0.00 sn:** Beyaz yuvarlak "pof" dairesi, hızla büyüyüp söner (0.12 sn, `OutQuad`).
2. **0.03 sn:** 6–10 konfeti parçacığı (kalp, yıldız, şeker) düşmanın paletinden, yerçekimli.
3. **0.05 sn:** 3–4 yumuşak toz bulutu (düşük opaklık %35) — göz yormayan hacim.
4. Düşmanın **şaşkın yüzü** sprite'ı dönerek yukarı uçar ve küçülür (sevimli "yenilgi").
5. XP gem'i küçük bir zıplamayla belirir.

Elit/boss patlamaları: + halka şok dalgası (distortion yerine **ucuz**, pastel renkli halka
sprite'ı), + altın para yağmuru, + hitstop.

### 5.3 Kaos kontrolü — "göz yormayan" kurallar

- **Tam ekran beyaz flaş yasak.** En fazla %35 opaklık, krem renkli (`#FFF6E0`), ≤ 0.1 sn.
- **Toplam parçacık bütçesi** (bkz. mimari §7): Aşılırsa en eski/en küçük efektler ilk önce
  kısılır; düşman mermileri **asla** kısılmaz.
- **Additive blend** sadece küçük vurgu parçacıklarında; büyük alanlarda alpha blend
  (additive üst üste binince beyaza patlar → göz yorar).
- Evrimleşmiş silah VFX'leri 2 katmanlıdır: **okunabilir çekirdek** (hitbox'ı gösteren) +
  **dekoratif hale** (yoğunlukta otomatik solan).
- Arka plan, oyun yoğunluğu arttıkça **%15'e kadar kararır ve doygunluğu düşer** (dinamik
  "odak"), böylece ön plan kaosu nefes alır.

## 6. Arka Plan ve Dünyalar

Her takımyıldız bir tatlı temasıdır; 3 katman paralaks (uzak bulutlar/gezegenler, orta
şeker adaları, yakın parıltı tozu). Arka plan **düşük kontrastlı ve bulanık** —
oyuncunun gözü hiçbir zaman orada dinlenmek için değil, sadece "yer hissi" içindir.

| Takımyıldız | Tema | Baskın renkler |
|-------------|------|---------------|
| 1 Şekerkamışı | Pembe-beyaz çizgili bulutlar | Pembe, krem |
| 2 Jöle Nebulası | Yarı saydam jöle gezegenleri | Mint, lila |
| 3 Kurabiye Kuşağı | Asteroitler = kurabiyeler | Karamel, bal |
| 4 Dondurma Kutbu | Eriyen buz gezegenler | Buz mavisi, vanilya |
| 5 Sakız Galaksisi | Balon gezegenler | Magenta, turkuaz |
| 6 Kraliçenin Yuvası | Dev yuva + altın yumurtalar | Altın, gece moru |

## 7. Üretim Standartları

| Varlık | Kaynak boyut | Format | Not |
|--------|-------------|--------|-----|
| Karakter/düşman | 512×512 | PNG → Sprite Atlas | Tek atlas/bölüm, ASTC 6×6 |
| Mermiler | 64×64 | Tek "bullet atlas" | Tip başına **tek instanced materyal** (GPU instancing için kritik) |
| VFX parçacıkları | 128×128 | Flipbook atlas | Paylaşılan materyal |
| UI | 9-slice | Sprite Atlas | UI Toolkit veya UGUI |
| Arka plan | 1080×2400 katman | ASTC 8×8 | Mip yok |

İsimlendirme: `spr_enemy_chick_idle_01`, `vfx_pop_confetti`, `ui_btn_play`.
