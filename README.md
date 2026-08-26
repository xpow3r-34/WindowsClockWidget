# WindowsClockWidget

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

## Geliştiren

xpow3r-34 • 2026
