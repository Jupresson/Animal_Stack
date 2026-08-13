# 🐾 Animal Stack

**Animal Stack** on fysiikkapohjainen mobiili-AR-peli, jossa tavoite on yksinkertainen:  
pinot mahdollisimman monta eläintä aloitusalustalle ilman, että yksikään tippuu reunan yli.

Leikkisä safari-/eläintarhateema yhdistyy taitopohjaiseen tasapainotteluun, jossa jokainen pudotus ratkaisee.

---

## 🎮 Pelin perusidea

- **Nimi:** Animal Stack  
- **Genre:** Fysiikkapohjainen AR stacker / skill puzzle  
- **Tavoite:** Pinota eläimiä mahdollisimman paljon aloitusalustalle  
- **Häviöehto:** Peli päättyy heti, kun yksi tai useampi eläin putoaa alustan ulkopuolelle  
- **Pisteytys:** Pisteet = onnistuneesti pinottujen eläinten määrä

### Mikä tekee pelistä kiinnostavan?

- Jokainen eläin on eri muotoinen ja painopisteeltään erilainen  
  (esim. pitkä kirahvi vs. matala/pyöreä kilpikonna)
- Pelaaja päättää **sekä sijainnin että rotaation** ennen pudotusta
- Fysiikka ratkaisee lopputuloksen: hyvä arviointi palkitaan, huono arvio kaataa pinon

---

## 🧠 Peliloop (ydinsilmukka)

1. Pelaaja tunnistaa pinnan (pöytä/lattia)  
2. Aloitusalusta asetetaan AR-ympäristöön  
3. Seuraava eläin näkyy esikatseluna pinon yllä  
4. Pelaaja vetää eläimen haluttuun kohtaan  
5. Pelaaja kiertää eläimen haluttuun kulmaan  
6. Eläin pudotetaan  
7. Fysiikka ratkaisee tasapainon  
8. Jos kaikki pysyy alustalla → **+1 piste** ja uusi eläin  
9. Jos eläin tippuu alustan ulkopuolelle → **Game Over**

---

## 📱 AR ja mobiilikäyttö

### AR-ratkaisu
- **AR-tyyppi:** Pintojen tunnistus  
- **Paikannus:** Ei GPS-vaatimusta  
- **Miksi AR on tärkeä:** Pelaaja voi liikkua pöydän ympäri ja arvioida pinoa eri kulmista ennen pudotusta

### Ohjaus
- **Drag:** siirtää eläintä pinon yllä  
- **Two-finger rotate:** kiertää eläintä  
- **Release / tap:** pudottaa eläimen  
- Vaihtoehtona kiertopainikkeet yhden käden pelaamista varten

### UI-ajatukset
- Yläreunassa iso pistelaskuri  
- Pieni “seuraava eläin” -esikatselu  
- Ensimmäiseen peliin lyhyt ohjevihje:
  - *“Vedä sijoittaaksesi, kierrä kahdella sormella, päästä irti pudottaaksesi.”*

---

## ✅ Ominaisuudet

### Pakolliset (MVP)
- [x] Pinnantunnistus + aloitusalustan asetus
- [x] Eläimen veto- ja rotaatiohallinta
- [x] Fysiikkapohjainen pinoutuminen
- [x] Häviöehdon tunnistus (putoaminen alustalta)
- [x] Pistelaskuri

### Lisäominaisuudet (jatkoon)
- [ ] Useita eläinmalleja eri muodoilla/koolla
- [ ] Äänitehosteet
- [ ] Heilunta- ja reaktiiviset animaatiot
- [ ] Kasvava vaikeustaso / ajastin
- [ ] Tulostaulukko
- [ ] Bonuspisteet erityisen vakaasta pinoamisesta

---

## 🛡️ Turvallisuus ja testattavuus

- Pelataan paikallaan, mielellään istuen pöydän ääressä  
- Toimii noin **50 × 50 cm** pöytätilassa  
- Testattavissa helposti luokkahuoneessa tai kotona  
- Ei vaadi kävelyä näyttöön tuijottaen → turvallisempi AR-kokemus

---

## 🧰 Tekniikka

- **Moottori:** Unity  
- **Unity-versio:** `6000.5.8f1`  
- **AR:** AR Foundation + ARCore + ARKit  
- **Renderöinti:** URP

> Tarkemmat paketit löytyvät tiedostosta:  
> `/home/runner/work/Animal_Stack/Animal_Stack/Packages/manifest.json`

---

## 🚀 Kehitysstatus

Repository sisältää tällä hetkellä Unity-AR-pohjan, jonka päälle Animal Stackin varsinainen pelilogiikka rakennetaan.

Seuraavat käytännön stepit:
1. Rakennetaan yksi selkeä peliskenaario (single scene flow)  
2. Lisätään spawn/esikatselu + drag/rotate/drop -putki  
3. Toteutetaan häviöehto ja score loop  
4. Viimeistellään UI + äänet + lisäeläimet

---

## 📌 Vision ydin

**Helppo aloittaa, vaikea mestaroida.**  
Yksi eläin kerrallaan, yksi virhe kerrallaan – kuinka korkealle pinosi kestää?
