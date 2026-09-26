# Google Play Listing: Galaxy Paws

> Package: `com.ehtiyarsgame.pofudukfilo` (fixed forever once published) · Developer: Ehtiyars Game
> Category: Games › Arcade · Content rating: expected PEGI 3 / Everyone (cartoon shooting, no blood, no chat)

## Title (≤ 30 characters)
- **EN:** Galaxy Paws: Space Shooter
- **TR:** Galaxy Paws: Uzay Savaşı

## Short description (≤ 80 characters)
- **EN:** Fluffy pilots, candy monsters, endless galaxy. How long can you survive?
- **TR:** Pofuduk pilotlar, şeker canavarlar, sonsuz galaksi. Ne kadar dayanabilirsin?

## Full description

### EN
Take the controls of a fluffy starship and blast through an endless galaxy full of candy monsters!

**One finger, endless action.** Drag to steer, and your ship fires on its own. Dodge egg bombs, jelly blobs,
laser-beaming donut UFOs and huge bosses. Don't let the swarm slip past you!

**Build your power every run.**
- Level up and choose from dozens of cards: new weapons, passives and wild evolutions.
- Fill the Sugar meter and unleash the Sugar Bomb.
- Recruit wingmen for your fleet.

**Grow stronger between runs.**
- Upgrade Firepower and Fire Rate in R&D.
- Unlock pilots, each with a signature gun that fires and grows in its own way.
- Upgrade every weapon down its own path.
- Climb the constellation board.

**Three maps, ever harder.** Survive 15 minutes on one to open the next. Each new map pays far more gold.

**Daily goals.** Missions, a 7-day login streak, record chests and a daily gift.

Galaxy Paws plays offline and never needs a connection.

### TR
Pofuduk bir uzay gemisinin başına geç ve şeker canavarlarla dolu sonsuz bir galakside savaş!

**Tek parmak, bitmeyen aksiyon.** Parmağını sürükle, gemin kendi kendine ateş etsin. Yumurta bombalarından, jöle
toplarından, lazer atan donut UFO'lardan ve dev boss'lardan kaç. Sürünün yanından kaçıp gitmesine izin verme!

**Her oyunda gücünü kur.**
- Seviye atla ve onlarca kart arasından seç: yeni silahlar, pasif güçler ve çılgın evrimler.
- Şeker barını doldur ve Şeker Bombası'nı patlat.
- Filona yardımcı pilotlar kat.

**Oyunlar arasında güçlen.**
- Ar-Ge'de Ateş Gücü ve Ateş Hızı geliştir.
- Pilotların kilidini aç; her birinin kendine özgü ateş eden ve güçlenen bir silahı var.
- Her silahı kendi yolunda geliştir.
- Takımyıldız tahtasında ilerle.

**Giderek zorlaşan üç harita.** Birinde 15 dakika hayatta kalırsan sonrakinin kilidi açılır. Her yeni harita çok
daha fazla altın kazandırır.

**Günlük hedefler.** Görevler, 7 günlük giriş serisi, rekor sandıkları ve günlük hediye.

Galaxy Paws çevrimdışı oynanır; internet bağlantısı gerekmez.

## Graphics (Play requirements)

| Asset | Size | Source |
|---|---|---|
| App icon | 512 × 512 PNG | `ArtRecipes` app icon, exported with `tools/verify/art` |
| Feature graphic | 1024 × 500 | To make: hero ship and swarm on the lobby backdrop, with the logo |
| Phone screenshots | 4–8, 9:16 (for example 1080 × 1920) | QA captures are 540 × 1170 (a ratio over 2:1, which Play rejects); crop to 9:16 or capture at 1080 × 1920 |

## Before the first upload (with the ads step)

1. **Upload keystore.** Generate it once, store the base64 and passwords as the repository secrets listed in
   `.github/workflows/release.yml`, and keep an offline backup. Losing it means asking Google to reset the
   upload key.
2. **Privacy policy URL.** Required because of AdMob (advertising ID). It can be a GitHub Pages page.
3. **Data safety form.**
   - AdMob collects the device/advertising ID and diagnostics.
   - The game itself keeps progress only on the device.
4. **Real AdMob IDs** in place of the test IDs, and an app-ads.txt on the developer website.
5. **Target audience:** 13+. Choosing children as an audience brings Families policy ad restrictions.
6. **Build:** run the "Play Store bundle" workflow and upload the `.aab` to Internal testing first.
