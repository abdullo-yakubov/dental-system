namespace DentalLocal.Client.Theming;

/// <summary>
/// Иконки в стиле Lucide (lucide.dev, ISC license), встроенные как raw SVG-строки —
/// MudBlazor принимает иконку именно так (см. <see cref="MudBlazor.Icons.Material"/> для сравнения формата),
/// поэтому подключение не требует npm-пакета или отдельного шрифта.
/// viewBox 24x24, stroke-based, совместимы с MudIcon/MudIconButton/MudNavLink.
/// </summary>
public static class LucideIcons
{
    private const string Wrap = "<g fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{0}</g>";

    private static string I(string inner) => string.Format(Wrap, inner);

    public static readonly string Dashboard = I("<rect x=\"3\" y=\"3\" width=\"7\" height=\"9\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"5\" rx=\"1\"/><rect x=\"14\" y=\"12\" width=\"7\" height=\"9\" rx=\"1\"/><rect x=\"3\" y=\"16\" width=\"7\" height=\"5\" rx=\"1\"/>");
    public static readonly string Patients = I("<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M22 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>");
    public static readonly string Calendar = I("<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18\"/>");
    // Круг с "S"-образным долларом (circle-dollar-sign) — читается как "деньги/баланс", в отличие от
    // прежнего перечёркнутого доллара, который визуально путался со значком "запрещено".
    public static readonly string Debtors = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M16 8h-6a2 2 0 1 0 0 4h4a2 2 0 1 1 0 4H8\"/><path d=\"M12 18V6\"/>");
    // Package (коробка с раскрытыми гранями) — стандартная пиктограмма склада/инвентаря.
    public static readonly string Inventory = I("<path d=\"m7.5 4.27 9 5.15\"/><path d=\"M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z\"/><path d=\"M3.29 7 12 12l8.71-5\"/><path d=\"M12 22V12\"/>");
    // Симметричный силуэт зуба (коронка + раздвоенный корень), координаты зеркальны относительно x=12 —
    // у Lucide официальной иконки "зуб" нет (проверено), поэтому силуэт нарисован вручную квадратичными
    // кривыми Безье — они предсказуемее кубических и меньше риск кривого рендера при 20px.
    public static readonly string Procedures = I("<path d=\"M7 4Q12 1 17 4Q20 5 19 9Q18.5 12 17 15Q16.5 18.5 14.5 21Q13 19 12 15Q11 19 9.5 21Q7.5 18.5 7 15Q5.5 12 5 9Q4 5 7 4Z\"/>");
    // TrendingUp (растущий график) — более узнаваемая метафора "аналитики/роста", чем сырые оси координат.
    public static readonly string Analytics = I("<path d=\"m22 7-8.5 8.5-5-5L2 17\"/><path d=\"M16 7h6v6\"/>");
    public static readonly string More = I("<circle cx=\"12\" cy=\"12\" r=\"1\"/><circle cx=\"19\" cy=\"12\" r=\"1\"/><circle cx=\"5\" cy=\"12\" r=\"1\"/>");
    // Гамбургер — открывает/закрывает боковое меню на узких экранах (см. адаптивную вёрстку в MainLayout.razor).
    public static readonly string Menu = I("<path d=\"M4 6h16M4 12h16M4 18h16\"/>");
    public static readonly string Search = I("<circle cx=\"11\" cy=\"11\" r=\"8\"/><path d=\"m21 21-4.3-4.3\"/>");
    public static readonly string Settings = I("<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9c.36.24.8.38 1.24.38H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1Z\"/>");

    public static readonly string Sun = I("<circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4\"/>");
    public static readonly string Moon = I("<path d=\"M21 12.8A9 9 0 1 1 11.2 3 7 7 0 0 0 21 12.8Z\"/>");
    // stroke="none" на точках обязателен: без него они наследуют stroke-width="2" от общей обёртки
    // (см. Wrap) поверх заливки — точки превращаются в жирные "кляксы" с лишним контуром, из-за чего
    // иконка визуально толще/"грязнее" остальных чисто контурных иконок в этом наборе.
    public static readonly string Palette = I("<circle cx=\"13.5\" cy=\"6.5\" r=\".5\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"17.5\" cy=\"10.5\" r=\".5\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"8.5\" cy=\"7.5\" r=\".5\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"6.5\" cy=\"12.5\" r=\".5\" fill=\"currentColor\" stroke=\"none\"/><path d=\"M12 2a10 10 0 1 0 0 20c1 0 2-.5 2-2 0-.6-.3-1-.6-1.4-.3-.4-.6-.8-.6-1.3 0-1 .8-1.8 1.8-1.8H17a3 3 0 0 0 3-3c0-5.5-4-9.5-8-10.5\"/>");
    public static readonly string Logout = I("<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4\"/><path d=\"m16 17 5-5-5-5\"/><path d=\"M21 12H9\"/>");
    public static readonly string Clock = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 6v6l4 2\"/>");

    public static readonly string Plus = I("<path d=\"M12 5v14M5 12h14\"/>");
    public static readonly string Edit = I("<path d=\"M17 3a2.85 2.85 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z\"/>");
    public static readonly string Trash = I("<path d=\"M3 6h18\"/><path d=\"M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2\"/><path d=\"M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6\"/><path d=\"M10 11v6M14 11v6\"/>");
    // Alias: содержимое совпадает с официальным Lucide "trash-2" (Trash выше уже включает внутренние
    // полоски — это она и есть), добавлен под именем, которое использует другой код в проекте.
    public static readonly string Trash2 = Trash;
    public static readonly string Check = I("<path d=\"M20 6 9 17l-5-5\"/>");
    public static readonly string CheckCircle = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m9 12 2 2 4-4\"/>");
    public static readonly string Block = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m4.9 4.9 14.2 14.2\"/>");
    public static readonly string ChevronLeft = I("<path d=\"m15 18-6-6 6-6\"/>");
    public static readonly string ChevronRight = I("<path d=\"m9 18 6-6-6-6\"/>");
    public static readonly string Inbox = I("<path d=\"M22 12h-6l-2 3h-4l-2-3H2\"/><path d=\"M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11Z\"/>");
    public static readonly string BarChart = I("<path d=\"M3 3v18h18\"/><rect x=\"7\" y=\"12\" width=\"3\" height=\"6\"/><rect x=\"12\" y=\"8\" width=\"3\" height=\"10\"/><rect x=\"17\" y=\"5\" width=\"3\" height=\"13\"/>");
    public static readonly string PieChart = I("<path d=\"M21.2 15.3A10 10 0 1 1 8.7 2.8\"/><path d=\"M22 12A10 10 0 0 0 12 2v10Z\"/>");
    public static readonly string TrendingUp = I("<path d=\"m22 7-8.5 8.5-5-5L2 17\"/><path d=\"M16 7h6v6\"/>");
    public static readonly string Download = I("<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"/><path d=\"M7 10l5 5 5-5\"/><path d=\"M12 15V3\"/>");
    public static readonly string DollarSign = I("<path d=\"M12 1v22\"/><path d=\"M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6\"/>");
    public static readonly string CalendarCheck = I("<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18\"/><path d=\"m9 16 2 2 4-4\"/>");
    public static readonly string CalendarX = I("<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18\"/><path d=\"m9.5 14.5 5 5M14.5 14.5l-5 5\"/>");
    public static readonly string UserPlus = I("<path d=\"M2 21v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v2\"/><circle cx=\"8\" cy=\"7\" r=\"4\"/><path d=\"M19 8v6M22 11h-6\"/>");
    public static readonly string UserSearch = I("<circle cx=\"10\" cy=\"7\" r=\"4\"/><path d=\"M10.3 15H7a4 4 0 0 0-4 4v2\"/><circle cx=\"17\" cy=\"17\" r=\"3\"/><path d=\"m21 21-1.9-1.9\"/>");
    public static readonly string Hourglass = I("<path d=\"M5 22h14M5 2h14\"/><path d=\"M6.5 2C6.5 8 12 8.5 12 12c0 3.5-5.5 4-5.5 10M17.5 2c0 6-5.5 6.5-5.5 10 0 3.5 5.5 4 5.5 10\"/>");

    // ── Расширенный набор: остальные разделы приложения (карточки пациента, настройки, диалоги) ──
    public static readonly string ArrowLeft = I("<path d=\"m12 19-7-7 7-7\"/><path d=\"M19 12H5\"/>");
    public static readonly string Paperclip = I("<path d=\"m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48\"/>");
    public static readonly string Backspace = I("<path d=\"M20 5H9l-7 7 7 7h11a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2Z\"/><path d=\"m12 9 6 6\"/><path d=\"m18 9-6 6\"/>");
    public static readonly string CloudUpload = I("<path d=\"M12 13v8\"/><path d=\"m8 17 4-4 4 4\"/><path d=\"M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3\"/>");
    public static readonly string IdCard = I("<path d=\"M16 10h2\"/><path d=\"M16 14h2\"/><path d=\"M6.17 15a3 3 0 0 1 5.66 0\"/><circle cx=\"9\" cy=\"11\" r=\"2\"/><rect x=\"2\" y=\"5\" width=\"20\" height=\"14\" rx=\"2\"/>");
    public static readonly string Cake = I("<path d=\"M20 21v-8a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8\"/><path d=\"M4 16s.5-1 2-1 2.5 2 4 2 2.5-2 4-2 2.5 2 4 2 2-1 2-1\"/><path d=\"M2 21h20\"/><path d=\"M7 8v3\"/><path d=\"M12 8v3\"/><path d=\"M17 8v3\"/><path d=\"M7 4h.01\"/><path d=\"M12 4h.01\"/><path d=\"M17 4h.01\"/>");
    public static readonly string Phone = I("<path d=\"M13.832 16.568a1 1 0 0 0 1.213-.303l.355-.465A2 2 0 0 1 17 15h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2A18 18 0 0 1 2 4a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v3a2 2 0 0 1-.8 1.6l-.468.351a1 1 0 0 0-.292 1.233 14 14 0 0 0 6.392 6.384\"/>");
    public static readonly string PhoneCall = I("<path d=\"M13 2a9 9 0 0 1 9 9\"/><path d=\"M13 6a5 5 0 0 1 5 5\"/><path d=\"M13.832 16.568a1 1 0 0 0 1.213-.303l.355-.465A2 2 0 0 1 17 15h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2A18 18 0 0 1 2 4a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v3a2 2 0 0 1-.8 1.6l-.468.351a1 1 0 0 0-.292 1.233 14 14 0 0 0 6.392 6.384\"/>");
    public static readonly string XCircle = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m15 9-6 6\"/><path d=\"m9 9 6 6\"/>");
    public static readonly string Tag = I("<path d=\"M12.586 2.586A2 2 0 0 0 11.172 2H4a2 2 0 0 0-2 2v7.172a2 2 0 0 0 .586 1.414l8.704 8.704a2.426 2.426 0 0 0 3.42 0l6.58-6.58a2.426 2.426 0 0 0 0-3.42z\"/><circle cx=\"7.5\" cy=\"7.5\" r=\".5\" fill=\"currentColor\" stroke=\"none\"/>");
    public static readonly string MessageCircle = I("<path d=\"M7.9 20A9 9 0 1 0 4 16.1L2 22Z\"/>");
    public static readonly string ListChecks = I("<path d=\"M11 6h9\"/><path d=\"M11 12h9\"/><path d=\"M11 18h9\"/><path d=\"m3 6 2 2 4-4\"/><path d=\"m3 12 2 2 4-4\"/><path d=\"m3 18 2 2 4-4\"/>");
    public static readonly string Circle = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/>");
    public static readonly string X = I("<path d=\"M18 6 6 18\"/><path d=\"m6 6 12 12\"/>");
    public static readonly string ArrowLeftRight = I("<path d=\"m16 3 4 4-4 4\"/><path d=\"M20 7H4\"/><path d=\"m8 21-4-4 4-4\"/><path d=\"M4 17h16\"/>");
    public static readonly string FileText = I("<path d=\"M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z\"/><path d=\"M14 2v4a2 2 0 0 0 2 2h4\"/><path d=\"M10 9H8\"/><path d=\"M16 13H8\"/><path d=\"M16 17H8\"/>");
    public static readonly string File = I("<path d=\"M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z\"/><path d=\"M14 2v4a2 2 0 0 0 2 2h4\"/>");
    public static readonly string Percent = I("<line x1=\"19\" y1=\"5\" x2=\"5\" y2=\"19\"/><circle cx=\"6.5\" cy=\"6.5\" r=\"2.5\"/><circle cx=\"17.5\" cy=\"17.5\" r=\"2.5\"/>");
    public static readonly string AlertCircle = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"12\" y1=\"8\" x2=\"12\" y2=\"12\"/><line x1=\"12\" y1=\"16\" x2=\"12.01\" y2=\"16\"/>");
    public static readonly string AlertTriangle = I("<path d=\"m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z\"/><path d=\"M12 9v4\"/><path d=\"M12 17h.01\"/>");
    public static readonly string ClipboardCheck = I("<rect x=\"8\" y=\"2\" width=\"8\" height=\"4\" rx=\"1\" ry=\"1\"/><path d=\"M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2\"/><path d=\"m9 14 2 2 4-4\"/>");
    public static readonly string ShieldCheck = I("<path d=\"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z\"/><path d=\"m9 12 2 2 4-4\"/>");
    public static readonly string History = I("<path d=\"M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8\"/><path d=\"M3 3v5h5\"/><path d=\"M12 7v5l4 2\"/>");
    public static readonly string Info = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 16v-4\"/><path d=\"M12 8h.01\"/>");
    public static readonly string Package = I("<path d=\"m7.5 4.27 9 5.15\"/><path d=\"M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z\"/><path d=\"M3.29 7 12 12l8.71-5\"/><path d=\"M12 22V12\"/>");
    public static readonly string ExternalLink = I("<path d=\"M15 3h6v6\"/><path d=\"M10 14 21 3\"/><path d=\"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/>");
    public static readonly string CreditCard = I("<rect width=\"20\" height=\"14\" x=\"2\" y=\"5\" rx=\"2\"/><line x1=\"2\" y1=\"10\" x2=\"22\" y2=\"10\"/>");
    public static readonly string Wallet = I("<path d=\"M21 12V7H5a2 2 0 0 1 0-4h14v4\"/><path d=\"M3 5v14a2 2 0 0 0 2 2h16v-5\"/><path d=\"M18 12a2 2 0 0 0 0 4h4v-4Z\"/>");
    public static readonly string UserX = I("<path d=\"M2 21v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 3.2 1.6\"/><circle cx=\"8\" cy=\"7\" r=\"4\"/><line x1=\"17\" y1=\"8\" x2=\"22\" y2=\"13\"/><line x1=\"22\" y1=\"8\" x2=\"17\" y2=\"13\"/>");
    public static readonly string Video = I("<path d=\"m16 13 5.223 3.482a.5.5 0 0 0 .777-.416V7.933a.5.5 0 0 0-.752-.432L16 10.5\"/><rect x=\"2\" y=\"6\" width=\"14\" height=\"12\" rx=\"2\"/>");
    public static readonly string Play = I("<path d=\"M6 3 20 12 6 21 6 3z\" fill=\"currentColor\" stroke=\"none\"/>");
    public static readonly string ListPlus = I("<path d=\"M11 12H3\"/><path d=\"M16 6H3\"/><path d=\"M16 18H3\"/><path d=\"M18 9v6\"/><path d=\"M21 12h-6\"/>");
    public static readonly string Printer = I("<path d=\"M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2\"/><path d=\"M6 9V3a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v6\"/><rect x=\"6\" y=\"14\" width=\"12\" height=\"8\"/>");
    public static readonly string Receipt = I("<path d=\"M4 2v20l2-1 2 1 2-1 2 1 2-1 2 1 2-1 2 1V2l-2 1-2-1-2 1-2-1-2 1-2-1-2 1Z\"/><path d=\"M8 7h8\"/><path d=\"M8 11h8\"/><path d=\"M8 15h5\"/>");
    public static readonly string RefreshCw = I("<path d=\"M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8\"/><path d=\"M21 3v5h-5\"/><path d=\"M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16\"/><path d=\"M8 16H3v5\"/>");
    public static readonly string Save = I("<path d=\"M15.2 3a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4V19a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z\"/><path d=\"M17 21v-7a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v7\"/><path d=\"M7 3v4a1 1 0 0 0 1 1h7\"/>");
    public static readonly string RotateCcw = I("<path d=\"M3 12a9 9 0 1 0 2.64-6.36L3 8\"/><path d=\"M3 3v5h5\"/>");
    public static readonly string ArrowUpDown = I("<path d=\"m21 16-4 4-4-4\"/><path d=\"M17 20V4\"/><path d=\"m3 8 4-4 4 4\"/><path d=\"M7 4v16\"/>");
    public static readonly string SlidersHorizontal = I("<line x1=\"21\" y1=\"4\" x2=\"14\" y2=\"4\"/><line x1=\"10\" y1=\"4\" x2=\"3\" y2=\"4\"/><line x1=\"21\" y1=\"12\" x2=\"12\" y2=\"12\"/><line x1=\"8\" y1=\"12\" x2=\"3\" y2=\"12\"/><line x1=\"21\" y1=\"20\" x2=\"16\" y2=\"20\"/><line x1=\"12\" y1=\"20\" x2=\"3\" y2=\"20\"/><line x1=\"14\" y1=\"2\" x2=\"14\" y2=\"6\"/><line x1=\"8\" y1=\"10\" x2=\"8\" y2=\"14\"/><line x1=\"16\" y1=\"18\" x2=\"16\" y2=\"22\"/>");
    public static readonly string Archive = I("<rect x=\"2\" y=\"4\" width=\"20\" height=\"5\" rx=\"2\"/><path d=\"M4 9v9a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9\"/><path d=\"M10 13h4\"/>");
    public static readonly string ArchiveRestore = I("<rect x=\"2\" y=\"4\" width=\"20\" height=\"5\" rx=\"2\"/><path d=\"M4 9v9a2 2 0 0 0 2 2h3\"/><path d=\"M20 9v9a2 2 0 0 1-2 2h-3\"/><path d=\"m9 15 3-3 3 3\"/><path d=\"M12 12v9\"/>");
    public static readonly string Undo2 = I("<path d=\"M9 14 4 9l5-5\"/><path d=\"M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5v0a5.5 5.5 0 0 1-5.5 5.5H11\"/>");
    public static readonly string Upload = I("<path d=\"M12 3v12\"/><path d=\"m17 8-5-5-5 5\"/><path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"/>");
    public static readonly string Lock = I("<rect x=\"3\" y=\"11\" width=\"18\" height=\"11\" rx=\"2\"/><path d=\"M7 11V7a5 5 0 0 1 10 0v4\"/>");
    public static readonly string Globe = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M2 12h20\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10Z\"/>");
}
