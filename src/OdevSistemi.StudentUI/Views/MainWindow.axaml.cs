using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using OdevSistemi.Shared.Contracts;

namespace OdevSistemi.StudentUI.Views;

public partial class MainWindow : Window
{
    // ---------- Servisler ----------
    private readonly ApiService _api = new();
    private readonly ExamHubService _hub = new();

    // ---------- Zamanlayıcılar ----------
    private readonly DispatcherTimer _examTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _lobbyTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    // ---------- Durum ----------
    private bool _demo;
    private string _userName = "";
    private int _userId;

    private ExamPackage? _exam;
    private readonly Dictionary<int, string> _answers = new();
    private readonly HashSet<int> _flags = new();
    private int _current;
    private DateTime _startedAt, _endAt, _lobbySince;
    private int _totalSec;
    private bool _submitted, _timeUp;

    // Gönderilmiş (veya sunucunun kesin reddettiği) sınavlar.
    // Yeniden bağlanınca sunucu aktif sınavı tekrar yollar; bunlar tekrar açılmasın.
    private readonly HashSet<int> _finishedExamIds = new();

    private List<ExamQuestion> Qs => _exam?.Questions ?? new List<ExamQuestion>();

    public MainWindow()
    {
        InitializeComponent();

        _examTimer.Tick += (_, _) => Tick();
        _lobbyTimer.Tick += (_, _) =>
            LobbyWait.Text = Fmt((int)(DateTime.Now - _lobbySince).TotalSeconds);

        // SignalR olayları arka plan thread'inden gelir, arayüze UI thread'inde dokunulmalı.
        _hub.ExamReceived += exam => Dispatcher.UIThread.Post(() =>
        {
            if (_finishedExamIds.Contains(exam.ExamId)) return;   // zaten gönderildi, tekrar açma
            StartExam(exam);
        });
        _hub.ExamEnded += () => Dispatcher.UIThread.Post(() =>
        {
            if (_exam != null && !_submitted) _ = SubmitAsync(false);
        });
        _hub.StateChanged += state => Dispatcher.UIThread.Post(() => SetPill(state));

        TxtPass.KeyDown += async (_, e) => { if (e.Key == Key.Enter) await LoginAsync(); };
    }

    // =====================================================================
    //  Yardımcılar
    // =====================================================================
    private static string Fmt(int sec)
    {
        sec = Math.Max(0, sec);
        return $"{sec / 60:00}:{sec % 60:00}";
    }

    private void Show(Control panel)
    {
        foreach (var p in new Control[] { LoginPanel, LobbyPanel, ExamPanel, DonePanel })
            p.IsVisible = p == panel;
    }

    private void SetPill(string state)
    {
        (string text, string bg, string fg) = state switch
        {
            "reconnecting" => ("Bağlantı koptu, yeniden bağlanılıyor…", "#FEF3C7", "#D97706"),
            "closed" => ("Bağlantı kesildi", "#FEE2E2", "#DC2626"),
            "demo" => ("Demo modu", "#D1FAE5", "#059669"),
            _ => ("Bağlı", "#D1FAE5", "#059669")
        };
        PillText.Text = text;
        PillText.Foreground = Brush.Parse(fg);
        PillDot.Fill = Brush.Parse(fg);
        PillBorder.Background = Brush.Parse(bg);
    }

    // =====================================================================
    //  1) Giriş
    // =====================================================================
    private async void OnLogin(object? sender, RoutedEventArgs e) => await LoginAsync();

    private async Task LoginAsync()
    {
        LoginError.Text = "";
        var ip = TxtIp.Text?.Trim() ?? "";
        var user = TxtUser.Text?.Trim() ?? "";
        var pass = TxtPass.Text ?? "";

        if (ip == "" || user == "" || pass == "")
        {
            LoginError.Text = "IP adresi, kullanıcı adı ve şifre gerekli.";
            return;
        }

        BtnLogin.IsEnabled = false;
        _demo = false;
        _api.SetServer(ip);

        var (res, data) = await _api.LoginAsync(user, pass);

        if (res.Status == 0)
            LoginError.Text = "Sunucuya ulaşılamadı. IP adresini ve aynı ağda olduğunu kontrol et.";
        else if (res.Status is 400 or 401)
            LoginError.Text = res.Error ?? "Öğrenci numarası veya şifre hatalı.";
        else if (res.Status == 403)   // rol kontrolü: eğitmen hesabıyla öğrenci uygulamasına girilemez
            LoginError.Text = res.Error ?? "Bu uygulamaya sadece öğrenciler giriş yapabilir.";
        else if (!res.Ok || data is null)
            LoginError.Text = $"Sunucu hatası ({res.Status}). Eğitmene haber ver.";
        else
        {
            // Hub'a kayıtta veritabanındaki kullanıcı Id'si kullanılır (öğrenci numarası değil).
            _userId = data.User.Id;
            _userName = string.IsNullOrWhiteSpace(data.User.FullName) ? user : data.User.FullName;
            try
            {
                await _hub.ConnectAsync(_api.BaseUrl, _api.Token, _userId, _userName);
                if (!TryResume()) EnterLobby();
            }
            catch (Exception ex)
            {
                LoginError.Text = "Giriş yapıldı ama sınav bağlantısı kurulamadı: " + ex.Message;
            }
        }

        BtnLogin.IsEnabled = true;
    }

    private void OnDemo(object? sender, RoutedEventArgs e)
    {
        _demo = true;
        _userId = 0;
        _userName = "Demo Öğrenci";
        EnterLobby();
    }

    // =====================================================================
    //  2) Lobi
    // =====================================================================
    private void EnterLobby()
    {
        LobbyName.Text = $"Merhaba, {_userName}";
        LobbyServer.Text = _demo ? "Demo modu" : _api.BaseUrl.Replace("http://", "");
        SetPill(_demo ? "demo" : "connected");
        BtnDemoExam.IsVisible = _demo;
        _lobbySince = DateTime.Now;
        LobbyWait.Text = "00:00";
        _lobbyTimer.Start();
        Show(LobbyPanel);
    }

    private async void OnLogout(object? sender, RoutedEventArgs e)
    {
        _lobbyTimer.Stop();
        await _hub.DisconnectAsync();
        _exam = null;
        Show(LoginPanel);
    }

    // =====================================================================
    //  3) Sınav
    // =====================================================================
    private void StartExam(ExamPackage exam, ExamBackupData? restore = null)
    {
        if (_exam != null && !_submitted) return;   // devam eden sınav varken yenisini alma

        // Aynı sınavın yedeği varsa (uygulama kapanıp açıldıysa) kaldığı yerden devam et.
        if (restore is null && !_demo)
        {
            var b = ExamBackup.Load(_userId);
            if (b != null && b.ExamId == exam.ExamId) restore = b;
        }

        _lobbyTimer.Stop();
        _exam = exam;
        _answers.Clear();
        _flags.Clear();
        _current = 0;
        _submitted = false;
        _timeUp = false;
        _startedAt = DateTime.Now;
        _totalSec = Math.Max(1, exam.RemainingSeconds ?? exam.DurationMinutes * 60);
        _endAt = _startedAt.AddSeconds(_totalSec);

        if (restore != null)
        {
            foreach (var kv in restore.Answers) _answers[kv.Key] = kv.Value;
            foreach (var f in restore.Flags) _flags.Add(f);
            _current = Math.Clamp(restore.Current, 0, Math.Max(0, Qs.Count - 1));
            _totalSec = Math.Max(1, restore.TotalSec);
            _endAt = restore.EndAtUtc.ToLocalTime();           // süre sıfırlanmaz, kaldığı yerden akar
            _startedAt = _endAt.AddSeconds(-_totalSec);
        }
        SaveBackup();

        ExamTitle.Text = exam.Title;
        BtnFinish.IsEnabled = true;
        RenderNav();
        RenderQuestion();
        UpdateProgress();

        WindowState = WindowState.FullScreen;
        Topmost = true;
        Show(ExamPanel);

        _examTimer.Start();
        Tick();
    }

    // ---------- Yerel yedek ----------
    private void SaveBackup()
    {
        if (_demo || _exam is null || _submitted) return;
        ExamBackup.Save(new ExamBackupData
        {
            UserId = _userId,
            ExamId = _exam.ExamId,
            EndAtUtc = _endAt.ToUniversalTime(),
            TotalSec = _totalSec,
            Current = _current,
            Exam = _exam,
            Answers = new Dictionary<int, string>(_answers),
            Flags = _flags.ToList()
        });
    }

    /// <summary>Giriş sonrası: bu öğrenciye ait yarım kalmış sınav yedeği varsa sınavı geri açar.</summary>
    private bool TryResume()
    {
        if (_demo) return false;
        var b = ExamBackup.Load(_userId);
        if (b is null) return false;
        // Süre dolmuşsa StartExam içindeki ilk Tick cevapları otomatik göndermeyi dener.
        StartExam(b.Exam, b);
        return true;
    }

    private void Tick()
    {
        double leftMs = (_endAt - DateTime.Now).TotalMilliseconds;
        int left = (int)Math.Ceiling(leftMs / 1000);
        TimerText.Text = Fmt(left);

        bool warn = left <= 300 && left > 60;
        bool danger = left <= 60;
        TimerBorder.Background = Brush.Parse(danger ? "#DC2626" : warn ? "#EA580C" : "#334155");
        TimeBar.Foreground = Brush.Parse(danger ? "#DC2626" : warn ? "#EA580C" : "#94A3B8");
        TimeBar.Value = Math.Clamp(leftMs / (_totalSec * 1000.0) * 100, 0, 100);

        if (left <= 0) _ = SubmitAsync(true);
    }

    // ---------- Soru paneli (numaralar) ----------
    private void RenderNav()
    {
        NavPanel.Children.Clear();
        for (int i = 0; i < Qs.Count; i++)
        {
            var q = Qs[i];
            bool answered = _answers.ContainsKey(q.Id);
            bool flagged = _flags.Contains(q.Id);
            bool cur = i == _current;

            var item = new Border
            {
                Width = 42,
                Height = 42,
                Margin = new Thickness(0, 0, 8, 8),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(flagged ? 3 : 2),
                Background = Brush.Parse(cur ? "White" : answered ? "#DCFCE7" : "#334155"),
                BorderBrush = Brush.Parse(flagged ? "#F59E0B" : cur ? "White" : answered ? "#DCFCE7" : "#334155"),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brush.Parse(cur ? "#1E293B" : answered ? "#166534" : "White")
                }
            };
            int idx = i;
            item.PointerPressed += (_, _) => GoTo(idx);
            NavPanel.Children.Add(item);
        }
    }

    private void GoTo(int i)
    {
        if (i < 0 || i >= Qs.Count) return;
        _current = i;
        RenderNav();
        RenderQuestion();
        QScroll.ScrollToHome();
        SaveBackup();
    }

    // ---------- Aktif soru ----------
    private void RenderQuestion()
    {
        var q = Qs[_current];
        QArea.Children.Clear();

        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Margin = new Thickness(0, 0, 0, 14) };
        head.Children.Add(new TextBlock
        {
            Text = $"Soru {_current + 1} / {Qs.Count}",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        head.Children.Add(new Border
        {
            Background = Brush.Parse("#F1F5F9"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(11, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = $"{q.Points:0.##} puan", Foreground = Brush.Parse("#334155"), FontWeight = FontWeight.SemiBold, FontSize = 13 }
        });
        QArea.Children.Add(head);

        QArea.Children.Add(new TextBlock
        {
            Text = q.Text,
            FontSize = 17,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 740,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 22)
        });

        if (q.Type == "MultipleChoice")
        {
            foreach (var o in q.Options)
                QArea.Children.Add(BuildOption(q, o));
        }
        else
        {
            _answers.TryGetValue(q.Id, out var saved);
            var tb = new TextBox
            {
                Text = saved ?? "",
                Watermark = "Cevabını buraya yaz",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 220,
                FontSize = 15,
                MaxWidth = 800,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var count = new TextBlock
            {
                Text = $"{tb.Text?.Length ?? 0} karakter",
                Foreground = Brush.Parse("Gray"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 800,
                Margin = new Thickness(0, 6, 0, 0)
            };
            tb.TextChanged += (_, _) =>
            {
                var t = tb.Text ?? "";
                if (string.IsNullOrWhiteSpace(t)) _answers.Remove(q.Id); else _answers[q.Id] = t;
                count.Text = $"{t.Length} karakter";
                RefreshStatus();
            };
            QArea.Children.Add(tb);
            QArea.Children.Add(count);
        }

        BtnPrev.IsEnabled = _current > 0;
        BtnNext.IsEnabled = _current < Qs.Count - 1;
        UpdateFlagButton();
    }

    private Border BuildOption(ExamQuestion q, ExamOption o)
    {
        bool sel = _answers.TryGetValue(q.Id, out var a) && a == o.Key;

        var key = new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(8),
            Background = Brush.Parse(sel ? "#1E293B" : "#F1F5F9"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0),
            Child = new TextBlock
            {
                Text = o.Key,
                FontWeight = FontWeight.Bold,
                Foreground = Brush.Parse(sel ? "White" : "#1E293B"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        var text = new TextBlock { Text = o.Text, FontSize = 15, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(key, 0);
        Grid.SetColumn(text, 1);
        row.Children.Add(key);
        row.Children.Add(text);

        var card = new Border
        {
            Padding = new Thickness(16, 13),
            Margin = new Thickness(0, 0, 0, 10),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(2),
            Background = Brush.Parse(sel ? "#F1F5F9" : "White"),
            BorderBrush = Brush.Parse(sel ? "#1E293B" : "#E2E8F0"),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = row
        };
        card.PointerPressed += (_, _) => SelectOption(q, o.Key);
        return card;
    }

    private void SelectOption(ExamQuestion q, string key)
    {
        _answers[q.Id] = key;
        RenderQuestion();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        RenderNav();
        UpdateProgress();
        SaveBackup();
    }

    private void UpdateProgress() =>
        ExamProgress.Text = $"{_answers.Count} / {Qs.Count} soru cevaplandı";

    // ---------- Önceki / Sonraki / İşaretle ----------
    private void OnPrev(object? sender, RoutedEventArgs e) => GoTo(_current - 1);
    private void OnNext(object? sender, RoutedEventArgs e) => GoTo(_current + 1);

    private void OnFlag(object? sender, RoutedEventArgs e)
    {
        var q = Qs[_current];
        if (!_flags.Add(q.Id)) _flags.Remove(q.Id);
        RenderNav();
        UpdateFlagButton();
        SaveBackup();
    }

    private void UpdateFlagButton()
    {
        bool on = _flags.Contains(Qs[_current].Id);
        BtnFlag.Content = on ? "İşareti kaldır" : "Sonra bak";
        BtnFlag.Foreground = Brush.Parse(on ? "#D97706" : "#1E293B");
    }

    // ---------- Klavye kısayolları ----------
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || _exam is null || _submitted || !ExamPanel.IsVisible || ModalOverlay.IsVisible) return;
        if (FocusManager?.GetFocusedElement() is TextBox) return;

        if (e.Key == Key.Right) GoTo(_current + 1);
        else if (e.Key == Key.Left) GoTo(_current - 1);
        else
        {
            var k = e.Key.ToString();
            var q = Qs[_current];
            if (k.Length == 1 && q.Type == "MultipleChoice" && q.Options.Any(o => o.Key == k))
                SelectOption(q, k);
        }
    }

    // =====================================================================
    //  4) Bitirme ve gönderim
    // =====================================================================
    private void OnFinish(object? sender, RoutedEventArgs e)
    {
        int blank = Qs.Count - _answers.Count;
        var parts = new List<string>();
        if (blank > 0) parts.Add($"{blank} soru boş.");
        if (_flags.Count > 0) parts.Add($"{_flags.Count} soruyu \"sonra bak\" diye işaretledin.");
        parts.Add("Gönderdikten sonra cevaplarını değiştiremezsin.");
        ModalText.Text = string.Join(" ", parts);
        ModalOverlay.IsVisible = true;
    }

    private void OnModalCancel(object? sender, RoutedEventArgs e) => ModalOverlay.IsVisible = false;
    private async void OnModalOk(object? sender, RoutedEventArgs e) => await SubmitAsync(false);

    private async Task SubmitAsync(bool timeUp)
    {
        if (_submitted || _exam is null) return;
        _submitted = true;
        _timeUp = timeUp;
        _examTimer.Stop();
        ModalOverlay.IsVisible = false;
        BtnFinish.IsEnabled = false;

        WindowState = WindowState.Normal;
        Topmost = false;

        DoneError.Text = "";
        DoneFacts.IsVisible = false;
        BtnRetry.IsVisible = false;
        BtnBack.IsVisible = false;
        SetDone("sending", timeUp ? "Süre doldu" : "Sınavınız gönderiliyor…",
                "Cevapların gönderiliyor. Lütfen bu pencereyi kapatma.");
        Show(DonePanel);

        await SendAnswersAsync();
    }

    private async Task SendAnswersAsync()
    {
        if (_exam is null) return;
        BtnRetry.IsVisible = false;
        BtnBack.IsVisible = false;
        DoneError.Text = "";

        SubmitResultDto? res;
        if (_demo)
        {
            await Task.Delay(700);
            res = new SubmitResultDto(true, "Demo: cevaplar alındı.");
        }
        else
        {
            // Tüm sorular gönderilir; boş bırakılanların cevabı null (sunucu 0 puan verir).
            // Çoktan seçmelide şık harfi ("A", "B"...) gider; puanlama harfi kabul ediyor.
            var dto = new SubmitExamDto(
                _exam.ExamId,
                Qs.Select(q => new AnswerDto(q.Id, _answers.TryGetValue(q.Id, out var a) ? a : null)).ToList());
            res = await _hub.SubmitAsync(dto);
        }

        // Daha önce gönderilmiş (ör. bağlantı koptuğu an cevap sunucuya ulaşmıştı) -> başarılı say.
        bool alreadySent = res is { Success: false } &&
                           res.Message.Contains("zaten", StringComparison.OrdinalIgnoreCase);

        if (res is { Success: true } || alreadySent)
        {
            if (!_demo) ExamBackup.Delete(_userId);
            _finishedExamIds.Add(_exam.ExamId);

            string text = alreadySent
                ? "Bu sınavı daha önce göndermiştin, cevapların eğitmende kayıtlı."
                : "Cevapların eğitmene ulaştı. Artık bu pencereyi kapatabilirsin.";
            if (AppConfig.ShowScore && !alreadySent && res?.Score is double score && res.MaxScore is double max)
                text += $" Puanın: {score:0.##} / {max:0.##}";

            SetDone(_timeUp ? "time" : "ok",
                    _timeUp ? "Süre doldu, cevapların gönderildi" : "Sınavınız gönderildi",
                    text);
            DoneAnswered.Text = $"{_answers.Count} / {Qs.Count}";
            int used = (int)Math.Min((DateTime.Now - _startedAt).TotalSeconds, _totalSec);
            DoneUsed.Text = Fmt(used);
            DoneFacts.IsVisible = true;
            BtnBack.IsVisible = true;
        }
        else if (res is null)
        {
            // Sunucuya ulaşılamadı: yedek duruyor, tekrar denenebilir.
            SetDone("fail", "Cevaplar gönderilemedi", "Cevapların bu bilgisayarda duruyor.");
            DoneError.Text = "Sunucuya ulaşılamıyor. Ağ bağlantını kontrol edip tekrar dene.";
            BtnRetry.IsVisible = true;
        }
        else
        {
            // Sunucu kesin olarak reddetti (süre doldu, sınav aktif değil...): yedek arşivlenir.
            if (!_demo) ExamBackup.Archive(_userId, _exam.ExamId);
            _finishedExamIds.Add(_exam.ExamId);
            SetDone("fail", "Cevaplar kabul edilmedi", "Cevapların bu bilgisayarda yedeklendi.");
            DoneError.Text = $"{res.Message} Eğitmene haber ver.";
            BtnBack.IsVisible = true;
        }
    }

    private void SetDone(string kind, string title, string text)
    {
        (string glyph, string bg, string fg) = kind switch
        {
            "ok" => ("✓", "#D1FAE5", "#059669"),
            "time" => ("!", "#FEF3C7", "#D97706"),
            "fail" => ("!", "#FEE2E2", "#DC2626"),
            _ => ("…", "#F1F5F9", "#1E293B")
        };
        DoneIconText.Text = glyph;
        DoneIconText.Foreground = Brush.Parse(fg);
        DoneIcon.Background = Brush.Parse(bg);
        DoneTitle.Text = title;
        DoneText.Text = text;
    }

    private async void OnRetry(object? sender, RoutedEventArgs e) => await SendAnswersAsync();

    private void OnBack(object? sender, RoutedEventArgs e)
    {
        _exam = null;
        _submitted = false;
        EnterLobby();
    }

    // =====================================================================
    //  Demo
    // =====================================================================
    private void OnDemoExam(object? sender, RoutedEventArgs e)
    {
        ExamQuestion Mc(int id, string text, string[] options, double points = 10) => new()
        {
            Id = id,
            Type = "MultipleChoice",
            Text = text,
            Points = points,
            Options = options.Select((t, i) => new ExamOption { Key = "ABCDE"[i].ToString(), Text = t }).ToList()
        };
        ExamQuestion Classic(int id, string text, double points) => new()
        {
            Id = id,
            Type = "Classic",
            Text = text,
            Points = points
        };

        StartExam(new ExamPackage
        {
            ExamId = 0,
            Title = "Örnek Sınav: Ağ ve Programlama Temelleri",
            DurationMinutes = 5,
            Questions = new List<ExamQuestion>
            {
                Mc(1, "HTTP protokolünün varsayılan portu hangisidir?", new[] { "21", "80", "443", "3306" }),
                Mc(2, "Aşağıdakilerden hangisi gerçek zamanlı iletişim kütüphanesidir?", new[] { "SignalR", "SQLite", "Dapper" }),
                Mc(3, "Bir yerel ağda cihazları birbirinden ayıran adres hangisidir?", new[] { "IP adresi", "Dosya adı", "Port numarası", "Dizin yolu" }),
                Mc(4, "Sunucunun tüm ağ arayüzlerini dinlemesi için hangi adres kullanılır?", new[] { "127.0.0.1", "0.0.0.0", "255.255.255.255", "localhost" }),
                Mc(5, "Dizi içinde ardışık arama (linear search) algoritmasının zaman karmaşıklığı nedir?", new[] { "O(1)", "O(log n)", "O(n)", "O(n²)" }),
                Mc(6, "Sıralı bir dizide ikili arama (binary search) karmaşıklığı nedir?", new[] { "O(n)", "O(log n)", "O(n log n)", "O(1)" }),
                Mc(7, "C dilinde bir değişkenin bellek adresini veren operatör hangisidir?", new[] { "*", "&", "->", "%" }, 5),
                Classic(8, "WebSocket ile klasik HTTP isteği arasındaki farkı iki cümleyle açıkla.", 15),
                Classic(9, "Bir sunucunun belirlenen sınav süresi bittikten sonra gelen cevapları reddetmesi neden gereklidir?", 15),
                Classic(10, "O(n) ve O(n²) karmaşıklığa sahip iki algoritmayı kısaca karşılaştır. Girdi 10 kat büyürse çalışma süreleri yaklaşık kaç kat artar?", 20)
            }
        });
    }
}
