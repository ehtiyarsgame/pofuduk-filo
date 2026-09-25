# Silah ve Yetenek Sistemi GDD

> Bağlı: `design/gdd/game-concept.md` §3.4 (envanter, kart taslağı), `design/art-bible.md` §5 (VFX)

## 1. Overview (Genel Bakış)

Oyuncu koşuya **1 başlangıç silahıyla** (seçilen karaktere göre) başlar. Level-up kartlarıyla
maks. **4 silah + 4 pasif** toplar. Her silah **Sv.1 → Sv.5** yükselir; Sv.5 + eşleşen pasif
ile boss/elit sandığında **Evrim**'e (Sv.★) dönüşür. İki evrimleşmiş silah belirli
kombinasyonlarda **Füzyon** (Sv.★★) oluşturabilir — koşunun "jackpot" anı.

## 2. Player Fantasy

"Her silah bir oyuncak gibi başlar, bir doğa olayı gibi biter." Sv.1 sevimli ve zayıftır;
evrim, ekranın tamamını kaplayan, *renkli ama okunabilir* bir gösteriye dönüşür.

## 3. Detailed Rules

### 3.1 Silah istatistik modeli

Her silahın ortak statları: `Hasar`, `Bekleme (cooldown)`, `Mermi Sayısı`, `Alan (area)`,
`Hız`, `Süre`, `Delme (pierce)`. Pasifler bu statları **global** çarpar. Oyuncu mermileri
düşman mermilerinden daha soluk/yarı saydamdır (bkz. art-bible §4) — kaos büyüdükçe bile
tehlike okunur kalır.

### 3.2 Beş temel silah

---

#### 🪶 1) Tüy Blaster — *düz ileri ana atış (başlangıç silahı)*

Pıtır'ın kulaklarından pastel tüy mermileri. Yüksek atış hızı, tek hedef odaklı.

| Sv | Değişiklik |
|----|-----------|
| 1 | 2 paralel tüy, 12 hasar, 0.25 sn bekleme *(QA run 12: tek şerit erken dalgaları temizleyemiyordu)* |
| 2 | 3 paralel tüy, 0.22 sn bekleme |
| 3 | 3 tüy, hafif yelpaze (±8°) |
| 4 | Hasar +%30, tüyler 1 düşmanı deler |
| 5 | 5 tüy yelpaze, her 5. atış **dev tüy** (3× hasar) |
| **★ Evrim: Gökkuşağı Prizma Işını** (pasif: **Kristal Gözlük** — kritik şansı) | Tüyler birleşip **sürekli bir gökkuşağı lazerine** dönüşür. Lazer ekranın üstüne kadar uzanır; bir düşmana değdiğinde **prizmada kırılarak** 3 yan ışına ayrılır (kırılan ışınlar da kırılabilir → fraktal ağaç). Renkler lazer boyunca kayar (shader'da UV kaydırma). Kritik vuruşta ışın kalınlaşır ve "pling" sesi. |

---

#### 🥚 2) Yumurta Havanı — *alan hasarı, yavaş ve güçlü*

Gemi yukarıya kavisli yumurtalar fırlatır; düştükleri yerde patlar (Chicken Invaders'a saygı).

| Sv | Değişiklik |
|----|-----------|
| 1 | 1 yumurta, 30 hasar, 1.5 m patlama, 2.0 sn bekleme |
| 2 | 2 yumurta, farklı hedeflere |
| 3 | Patlama alanı +%40 |
| 4 | Patlayan yumurtadan **2 mini civciv** çıkar, 3 sn en yakın düşmana dalar |
| 5 | 4 yumurta, bekleme 1.4 sn |
| **★ Evrim: Süpernova Omleti** (pasif: **Sıcak Tava** — alan +%) | Her 6 sn'de bir **dev altın yumurta** ekranın ortasına çatlar: 0.4 sn beyaz-sarı flaş (yumuşak, göz yormayan kremsi ton), içinden **sarı güneş gibi yayılan omlet dalgası** tüm ekrana hasar verir ve düşman mermilerini **pofuduk tüylere** çevirir (mermi temizleme!). Normal yumurtalar artık küçük süpernovalar. |

---

#### ⭐ 3) Yıldız Bumerang — *delen, geri dönen çok hedefli silah*

Fırlatılan yıldızlar ileri gidip geri döner; gidiş ve dönüşte vurur.

| Sv | Değişiklik |
|----|-----------|
| 1 | 1 yıldız, 15 hasar, sınırsız delme, 1.8 sn bekleme |
| 2 | 2 yıldız (±20°) |
| 3 | Menzil +%30, dönüşte hasar ×1.5 |
| 4 | 3 yıldız |
| 5 | Yıldızlar geri döndüğünde gemi etrafında **1 tur atar**, sonra tekrar fırlatılır |
| **★ Evrim: Galaksi Girdabı** (pasif: **Ay Tozu** — mermi süresi +%) | Yıldızlar artık geri dönmez: ekranın üst yarısında **dönen bir galaksi sarmalı** oluşturup düşmanları merkeze **çeker** (vakum). 4 sn sonra girdap çöker → yüzlerce minik yıldız havai fişek gibi saçılır. Sarmal kolları mor-mavi pastel gradyan, merkezde yanıp sönen kalp. |

---

#### 🫧 4) Sakız Balonu Yörüngesi — *gemi etrafında dönen savunma silahı*

Gemi etrafında dönen sakız balonları temas eden düşmana hasar verir ve **düşman mermilerini emer**.

| Sv | Değişiklik |
|----|-----------|
| 1 | 2 balon, 10 hasar/temas, yörünge yarıçapı 1.2 |
| 2 | 3 balon |
| 3 | Balon başına 1 düşman mermisini yutar (5 sn yenilenme) |
| 4 | 4 balon, yarıçap +%25 |
| 5 | Balon mermi yuttuğunda şişer ve **patlayıp** alan hasarı verir |
| **★ Evrim: Sakız Gezegen Halkaları** (pasif: **Esnek Sakız** — alan/menzil +%) | Balonlar büyüyüp **3 katmanlı gezegen halkasına** dönüşür (Satürn gibi, pembe-mint-lila). İç halka mermi emer, orta halka düşmanları **yapıştırıp yavaşlatır**, dış halka ters yönde dönerek biçer. Emilen her 20 mermi → halkalardan dışarı **sakız meteor** fırlar. |

---

#### ⚡ 5) Kıvılcım Kedi — *zincirleme elektrik, otomatik hedefleyen*

Geminin yanında süzülen minik bir kedi, en yakın düşmana zincirleme şimşek atar.

| Sv | Değişiklik |
|----|-----------|
| 1 | 1 şimşek, 18 hasar, 2 zıplama, 1.2 sn bekleme |
| 2 | 4 zıplama |
| 3 | Vurulan düşman 1 sn **sersemler** (mermi atamaz) |
| 4 | 2. kedi eklenir |
| 5 | Zıplama başına hasar azalmaz (+%0 düşüş) |
| **★ Evrim: Fırtına Kedisi Tanrıçası** (pasif: **Pil Tasması** — bekleme −%) | Kedi dev bir **bulut-kediye** dönüşür, ekranın tepesinde uyur gibi süzülür; her 0.5 sn'de rastgele 3 düşmana **pastel sarı yıldırım** düşer, yıldırım düştüğü yerde **elektrikli papatya** açar (1.5 sn alan hasarı). Kedi esnediğinde (her 8 sn) ekran boyunca zincir 20 hedefe sekiyor. |

#### 🐟 6) Balık Füzesi — *hedef takip eden, ıskalamayan* (ad-rewards.md: 1 200 altın veya 3 reklam)

Yelpaze halinde fırlayan küçük balıklar 0.18 sn düz gider, sonra en yakın düşmana döner (320°/sn).

| Sv | Değişiklik |
|----|-----------|
| 1 | 2 balık, 14 hasar, 1.1 sn bekleme, hız 9 |
| 2 | 3 balık |
| 3 | Çarpınca küçük patlama (yarıçap 0.8, %50 hasar) |
| 4 | 4 balık, 0.95 sn bekleme |
| 5 | 5 balık, 16 hasar, her biri 2 düşmana çarpar, patlama 1.0 |
| **★ Evrim: Köpekbalığı Sürüsü** (pasif: **Mıknatıs Kulak**) | 4 dev köpekbalığı (24 hasar); her vuruşta 2 yavru balık (%45 hasar) kendi başına avlanır. |

#### 🧶 7) Yün Yumağı — *Ball Blast topu, kenarlardan seker* (ad-rewards.md: 1 600 altın veya 4 reklam)

Yumak oyun alanının kenarlarından (üstte HUD çizgisinin altından) seker; değdiği düşmana 0.25 sn'de bir vurur. Ölen yumak her bekleme süresinde yeniden fırlatılır.

| Sv | Değişiklik |
|----|-----------|
| 1 | 1 yumak, 10 hasar, yarıçap 0.45, 8 sn ömür |
| 2 | 2 yumak |
| 3 | Yarıçap 0.6 |
| 4 | 3 yumak, hız 8.5 |
| 5 | 12 hasar; her sekme hasarı %10 artırır (en çok 5 sekme) |
| **★ Evrim: Kozmik Yumak** (pasif: **Havuç Kalkan**) | Tek dev yumak (yarıçap 1.1, 22 hasar, 12 sn); her sekmede yana fırlayan mini yumak (en çok 6, 3 sn). |

---

### 3.3 Pasifler (8 adet; 5'i evrim anahtarı)

| Pasif | Etki / seviye (maks 5) | Evrim anahtarı |
|-------|-----------------------|----------------|
| Kristal Gözlük | Kritik şansı +%6, kritik hasarı +%10 | Tüy Blaster |
| Sıcak Tava | Alan +%10 | Yumurta Havanı |
| Ay Tozu | Süre +%12, mermi hızı +%5 | Yıldız Bumerang |
| Esnek Sakız | Yörünge/menzil +%10 | Sakız Balonu |
| Pil Tasması | Bekleme −%7 | Kıvılcım Kedi |
| Havuç Kalkan | Maks can +%15, saniyede 0.3 can yenile | — |
| ~~Mıknatıs Kulak~~ | *Kaldırıldı (2026-09-25): toplananlar zaten gemiye uçuyor* | — |
| Şans Yoncası | Kart nadirlik şansı +, altın +%10 | — |

### 3.3b Yapı kartları (2026-09-25 — "oyun içi seçenek yok, strateji olmalı")

Her kart farklı bir oyun tarzını öne çıkarır; bazılarının bedeli vardır (kart üstünde kırmızı satır).

| Kart | Etki / seviye | Maks | Bedel / seviye | Kilit |
|------|---------------|------|----------------|-------|
| Delici Pençe | +1 delme | 3 | — | ücretsiz · **Balık Füzesi evrim anahtarı** |
| Çift Namlu | Tüm silahlar +1 mermi/top/sekme | 2 | −%8 hasar | Lab 900 |
| Kaplan Gözü | Kritik çarpanı +0.35 (2× → 2.35×) | 5 | — | ücretsiz |
| Cam Top | +%20 hasar | 3 | −%12 maks. can | Lab 600 |
| Son Direniş | Can < %40 iken +%25 hasar | 4 | — | Lab 450 |
| Şeker Kalbi | +%15 şeker dolumu | 5 | — | ücretsiz |
| Altın Pati | +%15 altın (oyun dışı gelişime yatırım) | 5 | — | ücretsiz |
| Bilge Baykuş | +%12 tecrübe | 5 | — | ücretsiz |
| Kaplumbağa Kabuğu | +2 zırh (her darbe 2 az) | 4 | −%4 atış hızı | ücretsiz |

Uygulama: `PassiveDefinition.drawbackStat/drawbackPerLevel`; `ExtraProjectiles` ve `Pierce` her atışta
`WeaponBehaviour` içinde eklenir (mermi sayısı 0 olan silahlara değil); `CritDamage` ve `LowHpDamage`
`RollDamage` içinde; `RushGain`, `Armor`, `Experience` pasif değişince `RunController` tarafından yeniden uygulanır.

### 3.4 Füzyon (★★) — iki evrimden tek efsane

Envanterde iki evrimleşmiş silah varsa, final boss sandığı füzyon kartı teklif edebilir
(silah slotu açılır, yeni bir efsane silah olur):

| Füzyon | Bileşenler | Görünüm |
|--------|-----------|---------|
| **Gökkuşağı Fırtınası** | Prizma Işını + Fırtına Kedisi | Yıldırımlar prizmalardan kırılıp renk renk ağ örer |
| **Kozmik Kahvaltı** | Süpernova Omleti + Galaksi Girdabı | Girdabın merkezinde omlet-güneş doğar, gezegen gibi yörüngede yumurtalar döner |
| **Şeker Kalkanı Galaksisi** | Sakız Halkaları + Galaksi Girdabı | Halkalar galaksi kollarına dönüşür, emilen mermiler yıldız olarak geri fırlatılır |

### 3.5 Sinerjiler (evrim gerektirmeyen küçük etkileşimler)

- **Kıvılcım + Sakız:** Şimşek balonlardan da seker (balonlar "iletken").
- **Yumurta + Bumerang:** Bumerang yumurtaya değerse yumurta havada patlar (tetikleyici).
- **Tüy + Kristal Gözlük** sadece evrim değil: Sv.3'ten itibaren kritikler tüy yağdırır.

Sinerjiler kartta küçük bir **🔗 ikonu** ile gösterilir → oyuncu keşfettikçe "aha!" anı.

## 4. Formulas

```
Hasar_final   = Hasar_base(Sv) × (1 + ΣPasifHasar% + MetaHasar%) × (kritik ? KritÇarpan : 1)
Bekleme_final = Bekleme_base(Sv) × Π(1 − Bekleme%_i) ,  alt sınır = base × 0.35
Alan_final    = Alan_base × (1 + ΣAlan%)
DPS_hedef(t)  ≈ 1.15 × Ortalama düşman HP(t) × Spawn/sn(t)   // oyuncu hafif önde
```

Evrimleşmiş silah DPS'i, aynı silahın Sv.5 DPS'inin **2.5–3.5×**'ü olmalıdır.

## 5. Edge Cases

- Evrimleşmiş silah + pasif satılamaz/değiştirilemez (koşu içinde kalıcı).
- Envanterde pasif var ama silah yok → pasif yine global etkisini verir; evrim kartı çıkmaz.
- Mermi emen etkiler (Sakız, Omlet) boss'un **"özel saldırı"** mermilerini emmez (okunabilir
  kırmızı kenarlı mermiler; adil boss dövüşü).
- Füzyon, iki evrim slotunu tek slota indirir → yeni silah için yer açılır (bilinçli ödül).

## 6. Dependencies

Kart taslağı (`game-concept.md` §3.4), meta silah kilitleri (`meta-economy.md` §3.3),
mermi sistemi / BulletSystem (`architecture.md` §4).

## 7. Tuning Knobs

Tüm değerler `WeaponDefinition` ScriptableObject'lerinde (`Assets/_Project/Data/Weapons/`)
tutulur; kod değişikliği olmadan dengelenebilir. Kritik düğmeler: seviye başına hasar
eğrisi, bekleme alt sınırı (%35), evrim DPS çarpanı hedefi (2.5–3.5×).

## 8. Acceptance Criteria

- [ ] Her silah tek başına (sadece o silah + pasifleriyle) bölüm 1 boss'unu yenebilir.
- [ ] Hiçbir silahın seçilme oranı telemetride **> %35** değildir (baskın strateji yok).
- [ ] Evrim animasyonu ≤ 1.2 sn, oyunu en fazla 0.6 sn dondurur.
- [ ] Füzyon silahı aktifken bile düşman mermileri %100 okunabilir (playtest körlük testi).
