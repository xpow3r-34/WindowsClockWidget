# WindowsClockWidget — Yol Haritası

Windows masaüstü için şeffaf saat widget'ı (C# / WPF / .NET 8).
Geliştiren: xpow3r-34 • 2026

---

## ✅ Tamamlanan İşler

### v1.0.0 — Temel Widget
- Şeffaf, çerçevesiz, her zaman üstte duran saat widget'ı
- Sürükleyerek konumlandırma, ekran kenarına yapışma yok (serbest konum)
- Sağ tık menüsü: Ayarlar / Kapat + sürüm bilgisi ("xpow3r 2026 • v{surum}")
- Tek örnek (single instance): uygulama ikinci kez açılamaz
- Hata yakalama ve günlük kaydı (`%APPDATA%\WindowsClockWidget\error.log`)

### v1.0.1 — Özelleştirme ve Hava Durumu
- **Ayarlar penceresi**: Uygula / Kaydet / Vazgeç butonları, canlı önizleme
- Saat (HH) ve dakika (mm) için ayrı ayrı: yazı tipi, punto, kalınlık, renk (Windows renk paleti)
- Tarih rengi ayarı
- ":" ayacı göster/gizle
- **Hava durumu** (Open-Meteo API, anahtar gerekmez):
  - ~44 Türkiye şehri listesi, Türkçe açıklamalar
  - Sıcaklık + açıklama + ikon; detay satırı (hissedilen / nem / rüzgâr) açılıp kapanabilir
  - Konum: saatin üstünde veya altında; hizalama: sol / orta / sağ
  - Yenileme aralığı ayarı
- Widget'ı her zaman üstte tutma (isteğe bağlı)
- Konumu sabitleme (sürükleme kapalı, konum hatırlanır)
- Konum ve tüm ayarlar `%APPDATA%\WindowsClockWidget\settings.json` dosyasına kaydedilir
- Uygulama ikonu (app.ico)

### v1.0.2 — Saydamlık ve Hizalama
- Arka plan **kalıcı olarak tamamen saydam** (saydamlık ayarı kaldırıldı, gölge kaldırıldı)
- Rakamlar arası dikey hizalama: font varsanımları yerine **gerçek glif geometrisi ölçümü**
  (`BuildGeometry`) — farklı boyut/kalınlıktaki saat ve dakika rakamları piksel hassasiyetinde hizalanır

### v1.0.3 — Üstte Konum Düzeltmesi + Saat Başı Uyarısı
- Hava durumu "üstte" modunda saat ile iç içe girme sorunu giderildi
- **Saat başı uyarısı**: her tam saatte seçilen ses çalar
- Ses seçimi: `C:\Windows\Media` içindeki tüm Windows sesleri listelenir,
  **Dinle** butonu ile dinleyerek seçim yapılır

### v1.0.4–v1.0.5 — Üstte Modu Boşluk Düzeltmesi
- Hava durumu üstteyken saat ile arasındaki boşluk, altta modundaki gibi sıkılaştırıldı (~4-6px)
- Kök neden: hava durumu ikonunun (Segoe UI Symbol) dev satır kutusu paneli şişiriyordu;
  ikon satır yüksekliği sıcaklık metnine kilitlendi (`BlockLineHeight`)
- Saat satırı üstte modunda tam ölçüyle yukarı çekiliyor

### v1.0.6 — Windows ile Başlat
- Sağ tık menüsünde işaretlenebilir **"Windows ile başlat"** seçeneği + Ayarlar'da onay kutusu
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` kaydı ile yönetim (yönetici izni gerekmez)
- Exe taşınırsa kayıt yolu her açılışta otomatik tazelenir
- Durum `settings.json` içinde `StartWithWindows` olarak saklanır

### v1.0.7 — Takvim ve Hatırlatmalar
- Sağ tık menüsünden "Windows ile başlat" kaldırıldı (yalnızca Ayarlar'dan erişilebilir)
- **Mini takvim widget'ta**: ay/yıl başlığı, gün isimleri (Pzt-Paz), today vurgusu, hatırlatma göstergeleri
- **Takvim penceresi**: tam aylık görünüm, güne tıklayınca o günün hatırlatmaları, hatırlatma ekleme
- **Hatırlatma sistemi**: başlık, tarih/saat, tekrar sıklığı (yok/günlük/haftalık/aylık), ses seçimi, ne kadar önce hatırlatılacağı
- **Hatırlatma bildirimi**: 30 sn aralıkla kontrol, tetiklendiğinde widget'ta mesaj (8 sn) + ses çalma
- Tekrar eden hatırlatmalar otomatik olarak bir sonraki tarihe kaydırılır
- Veriler `%APPDATA%\WindowsClockWidget\reminders.json` dosyasında depolanır
- Sağ tık menüsüne "Takvim" ve "Hatırlatmalar" eklendi
- Ayarlar penceresine "Widget'ta mini takvim göster" seçeneği eklendi

### v1.0.8 — Tarih Gösterimi ve Serbest Konumlandırma
- **Tarih gösterimi**: saat altında konumlandırılabilir tarih satırı (yazı tipi, boyut, kalınlık, renk, konum)
- **Serbest konumlandırma**: hava durumu ve tarih için X/Y offset kaydırıcıları
- Chime sesi düzeltmesi: `SoundPlayer` nesnesi artık `_chimePlayer` alanı ile tutuluyor

### v1.0.9 — Düzeltmeler ve Takvim Renk
- Y offset yönü düzeltmesi: +Y = Yukarı, -Y = Aşağı (kullanıcı beklentisine uygun)
- Hizalama kutusu (AlignBox) kaldırıldı — hava durumu hizalama ayarları temizlendi
- **TARİH grubu** ayarlar penceresine eklendi: Tarih göster onay kutusu + renk seçici
- **Takvim rengi** ayarı: widget'taki mini takvimin vurgu rengi özelleştirilebilir
- Şehir ismi gösterimi: "Açık - İstanbul" formatında

### v1.1.0 — Düzeltmeler ve Bildirim İyileştirmeleri
- ":" ayacı artık **Dakika** fontunu kullanıyor (aile, boyut, kalınlık, renk)
- **NotifyIcon baloncuk bildirimi**: hatırlatma tetiklendiğinde Windows bildirim baloncuğu gösteriliyor
- DateText renk/boyut düzeltmesi: bildirim sonrası rengin bozulması engellendi
- Takvim göster/rengi ayarları GENEL grubundan **TARİH** grubuna taşındı

### v1.1.1 — Hatırlatma Sistemi Yeniden Yazımı
- **ReminderWindow tamamen yeniden yazıldı**: sadece form, liste kaldırıldı
- ReminderWindow artık doğrudan `Reminder` nesnesi alıyor — listede seçim/düzenleme karmaşası kaldırıldı
- Kaydet butonu doğrudan nesneyi değiştiriyor ve JSON'a yazıyor
- CalendarWindow'da her hatırlatma yanında **✏️ (düzenle)** ve **🗑 (sil)** ikonları eklendi
- **Silme onayı**: hatırlatma silmeden önce Onay kutusu gösteriliyor
- Saat taşması düzeltmesi: `Hour + 1 = 24` durumunda tarih kayması engellendi (`% 24`)
- Liste yenileme düzeltmesi: `ItemsSource = null` ile zorunlu yenileme
- Kayıt sonrası `_selectedDate` güncelleniyor

### v1.2.1 — Ayarlar Penceresi Boyutlandırma Düzeltmesi
- Ayarlar penceresi içeriği ekranı aşıyordu; içerik **ScrollViewer** içine alındı (dikey kaydırma)
- Pencere `MaxHeight` değeri çalışma alanı yüksekliğine kilitlendi
  (`WorkArea.Height - 40`, en az 300px) — `OnLoaded` olayı ile

---

## 🗺️ Planlanan Geliştirmeler

| Özellik | Açıklama | Durum |
|---|---|---|
| ⏰ Alarm | Belirlenen saatte sesli uyarı; saat başı uyarısı altyapısı yeniden kullanılacak; alarm listesi, tekrar günleri, erteleme (snooze) | Planlandı |
| 🌍 Farklı saat dilimleri | Başka şehirlerin saatini de göster (New York, Londra, Tokyo); widget altında ek satır | Planlandı |
| ⏱ Pomodoro Zamanlayıcı | 25dk çalış / 5dk mola döngüsü, widget üzerinde geri sayım göstergesi, sesli uyarı | Planlandı |
| 🔕 Odak modu | Windows Odak Yardımcısı ile senkronize; belirli saatlerde bildirimleri ve sesleri sessize alma | Planlandı |
| 📊 Sistem bilgisi | CPU, RAM, disk kullanımı (opsiyonel, widget altında gösterim) | Planlandı |

---

## Sürüm Geçmişi
- **1.2.1** — Ayarlar penceresi boyutlandırma düzeltmesi (ScrollViewer + MaxHeight)
- **1.2.0** — Takvim ve hatırlatma sistemi yeniden yazımı, tarih gösterimi, serbest konumlandırma
- **1.1.0** — Ayacı font değişikliği, NotifyIcon bildirimi, DateText düzeltmesi, ayar grupları
- **1.0.9** — Y offset düzeltmesi, takvim rengi, şehir ismi gösterimi
- **1.0.8** — Tarih gösterimi, serbest konumlandırma, chime düzeltmesi
- **1.0.7** — Takvim ve hatırlatmalar (mini takvim, hatırlatma sistemi, bildirim)
- **1.0.6** — Windows ile başlat seçeneği (sağ tık menüsü + ayarlar, registry Run anahtarı)
- **1.0.5** — Üstte modu boşluk kök nedeni giderildi (ikon satır kutusu)
- **1.0.4** — Üstte modu boşluk denemesi
- **1.0.3** — Üstte modu çakışma düzeltmesi + saat başı uyarısı + Windows sesleri
- **1.0.2** — Kalıcı saydam arka plan + glif geometrisiyle piksel hizalama
- **1.0.1** — Ayarlar penceresi, hava durumu, özelleştirme
- **1.0.0** — İlk sürüm
