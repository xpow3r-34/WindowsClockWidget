# WindowsClockWidget

**Güncel sürüm: 1.2.6**

Windows masaüstü için şeffaf, özelleştirilebilir saat widget'ı.

C# / WPF / .NET 8 ile geliştirilmiştir.

## Özellikler

- Şeffaf, çerçevesiz, her zaman üstte duran saat widget'ı
- Sürükleyerek konumlandırma
- Hava durumu gösterimi (~44 Türkiye şehri, Open-Meteo API)
- Tarih gösterimi
- Mini takvim
- Hatırlatma sistemi (tekrar sıklığı, ses, bildirim)
- Saat başı uyarısı
- Windows ile başlatma
- Ayarlar penceresi (canlı önizleme)
- Hatırlatmalar için seçilen süre kadar önceden bildirim
- Okuma ve ses hatalarının hata günlüğüne kaydedilmesi
- Okunamayan ayar ve hatırlatma dosyaları için yedekleme ve kullanıcı uyarısı
- Sağ tık menüsünden hava durumunu elle yenileme
- Hava durumu kaynağı ve otomatik yenileme aralığı seçimi
- Ayarlar penceresinde küçük ekranlar için dikey kaydırma
- Mini takvimde ay başlığı, hafta günleri ve gezinme okları için seçilebilir renk

## 1.2.6 değişiklikleri

- Ayarlar penceresindeki mevcut dikey kaydırma, daha küçük çalışma alanlarına uyacak şekilde iyileştirildi.
- Mini takvimde ay/yıl başlığı, gün kısaltmaları ve sağ/sol oklar seçilen takvim rengini kullanır; renk değişince görünüm de yenilenir.

## 1.2.5 değişiklikleri

- Hava durumu kaynağı olarak Open-Meteo, OpenWeather, WeatherAPI.com veya MET Norway seçilebilir.
- API anahtarı gerektiren kaynakların bilgileri ayarlardan girilir ve kullanıcı ayarlarında saklanır.
- Otomatik yenileme aralığı ayarlanabilir; MET Norway için 3 saatlik aralık uygulanır.

## 1.2.4 değişiklikleri

- Takvim görünümü açıkken oluşan başlangıç çökmesi giderildi.
- Hava durumu konum bilgisi önbelleğe alınır; servis 429 istek sınırı döndürürse yeniden deneme zamanına kadar istek gönderilmez.

## 1.2.3 değişiklikleri

- Hava durumu hatalarında genel “bağlantı yok” metni yerine hata türüne göre açıklama gösterilir.
- Sağ tık menüsüne “Hava Durumunu Güncelle” komutu eklendi.
- Aynı anda birden fazla hava durumu isteği gönderilmesi engellendi.

## 1.2.2 değişiklikleri
- Hatırlatmalardaki “kaç dakika önce” seçeneği artık bildirim zamanlamasında uygulanır.
- Okunamayan ayar ve hatırlatma dosyaları için hata günlüğü, yedekleme ve kullanıcı uyarısı eklendi.
- Hava durumu hataları hata günlüğüne yazılır ve ayrıntısı widget araç ipucunda gösterilir.
- Saatlik uyarı, hatırlatma ve ses önizleme sorunları hata günlüğüne kaydedilir; ses önizlemesi başarısız olursa kullanıcı bilgilendirilir.

## Kurulum

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) yükleyin
2. Repo'yu klonlayın:
   ```
   git clone https://github.com/xpow3r-34/WindowsClockWidget.git
   ```
3. Proje dizininde derleyin:
   ```
   dotnet build -c Release
   ```
4. Çalıştırın:
   ```
   dotnet run
   ```

## Kullanım

- Widget'ı fare ile sürükleyerek konumlandırın
- Sağ tık menüsü ile Ayarlar, Takvim, Hatırlatmalar ve daha fazlasına erişin
- Ayarlar'dan yazı tipi, renk, hava durumu, tarih ve diğer seçenekleri özelleştirin

## Gereksinimler

- Windows 10/11
- .NET 8 Runtime veya SDK

## Lisans

MIT

## Ekran Görüntüleri
<img width="1028" height="1203" alt="clockwidget-ss1" src="https://github.com/user-attachments/assets/434a521d-6596-4fa9-9a5f-8301dc14189f" />
<img width="1026" height="1198" alt="clockwidget-ss3" src="https://github.com/user-attachments/assets/5332c567-e1f2-4d1b-90c8-765922e2a1fb" />
<img width="1304" height="504" alt="clockwidget-ss2" src="https://github.com/user-attachments/assets/9604368e-fcd7-40c0-8149-63d8fe8c7d56" />





## Geliştiren

xpow3r-34 • 2026
