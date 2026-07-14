using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DentalLocal.Infrastructure.Services;

public class PdfService : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateAgreementPdf(Patient patient)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header()
                    .Text("ДОГОВОР НА ОКАЗАНИЕ СТОМАТОЛОГИЧЕСКИХ УСЛУГ")
                    .SemiBold().FontSize(16).AlignCenter();

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text($"г. Ташкент                                                                     Дата: {DateTime.Today:dd.MM.yyyy}г.");

                    column.Item().Text("Клиника «DentalLocal» в лице Главного врача (Исполнитель), с одной стороны, и гражданин(ка):")
                        .LineHeight(1.3f);

                    column.Item().Text($"{patient.FullName}, {patient.BirthDate:dd.MM.yyyy} года рождения, проживающий(ая) по адресу: {patient.Address ?? "не указан"} (Заказчик), с другой стороны, заключили настоящий Договор о нижеследующем:")
                        .Bold().LineHeight(1.3f);

                    column.Item().PaddingTop(10).Text("1. ПРЕДМЕТ ДОГОВОРА").Bold().FontSize(11);
                    column.Item().Text("Исполнитель обязуется оказать Заказчику стоматологические услуги, а Заказчик обязуется оплатить оказанные услуги в соответствии с утвержденным прейскурантом клиники.")
                        .LineHeight(1.2f);

                    column.Item().PaddingTop(10).Text("2. УСЛОВИЯ И ПОРЯДОК ОПЛАТЫ").Bold().FontSize(11);
                    column.Item().Text("Оплата стоматологических услуг производится непосредственно после приема у лечащего врача в наличной либо безналичной форме (картой/переводом) в полном объеме.")
                        .LineHeight(1.2f);

                    column.Item().PaddingTop(10).Text("3. ПРАВА И ОБЯЗАННОСТИ СТОРОН").Bold().FontSize(11);
                    column.Item().Text("Заказчик обязан соблюдать правила личной гигиены полости рта и выполнять рекомендации лечащего врача. Исполнитель гарантирует качество лечения при условии строгого выполнения назначений врача.")
                        .LineHeight(1.2f);

                    column.Item().PaddingTop(30).Text("ПОДПИСИ СТОРОН:").Bold().FontSize(11);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Исполнитель:").Bold();
                            c.Item().Text("ООО «DentalLocal»");
                            c.Item().Text("М.П. ___________________");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Заказчик:").Bold();
                            c.Item().Text(patient.FullName);
                            c.Item().Text("Подпись: _________________");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Договор составлен в 2-х экземплярах. Страница ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateConsentPdf(Patient patient)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header()
                    .Text("ДОБРОВОЛЬНОЕ ИНФОРМИРОВАННОЕ СОГЛАСИЕ\nНА СТОМАТОЛОГИЧЕСКОЕ Вмешательство")
                    .SemiBold().FontSize(14).AlignCenter();

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text($"Я, нижеподписавшийся(ая) {patient.FullName}, {patient.BirthDate:dd.MM.yyyy} г.р.,")
                        .Bold();

                    column.Item().Text("Подтверждаю, что добровольно соглашаюсь на проведение мне стоматологического обследования и необходимого лечения в клинике «DentalLocal».")
                        .LineHeight(1.3f);

                    column.Item().Text("Врач подробно разъяснил мне план лечения, возможные альтернативные варианты, а также риски и осложнения, которые могут возникнуть в процессе лечения или после его окончания (такие как болевые ощущения, отек мягких тканей, временное онемение и т.д.).")
                        .LineHeight(1.3f);

                    column.Item().Text("Я предупрежден(а) о необходимости строгого выполнения рекомендаций врача, приема назначенных медикаментов и явки на контрольные осмотры.")
                        .LineHeight(1.3f);

                    column.Item().PaddingTop(10).Text("Состояние моего здоровья (аллергии, хронические болезни):").Bold();
                    column.Item().Text($"Аллергии: {patient.Allergies ?? "нет"}");
                    column.Item().Text($"Хронические болезни: {patient.ChronicDiseases ?? "нет"}");

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Подпись врача: ________________");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Подпись пациента: ________________");
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GeneratePrescriptionPdf(Visit visit)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A5.Landscape());
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Клиника «DentalLocal»").Bold().FontSize(12);
                        c.Item().Text("г. Ташкент");
                    });
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Рецепт № {visit.Id.ToString()[..8].ToUpper()}").Bold().FontSize(12);
                        c.Item().Text($"Дата: {visit.VisitDate:dd.MM.yyyy}г.");
                    });
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(8);

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().Text(t =>
                    {
                        t.Span("Пациент: ").Bold();
                        t.Span(visit.Patient?.FullName ?? "Пациент не указан");
                    });

                    column.Item().Text(t =>
                    {
                        t.Span("Лечащий врач: ").Bold();
                        t.Span(visit.DoctorName);
                    });

                    column.Item().PaddingTop(5).Text("НАЗНАЧЕНИЯ И РЕКОМЕНДАЦИИ:").Bold().FontSize(11);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).MinHeight(80)
                        .Text(text =>
                        {
                            text.Span(visit.Prescriptions ?? "Нет назначений.").LineHeight(1.3f).Italic();
                        });

                    column.Item().PaddingTop(15).AlignRight().Text("Подпись врача (печать): _____________________");
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateInvoicePdf(Visit visit)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("КВИТАНЦИЯ ОБ ОПЛАТЕ УСЛУГ").SemiBold().FontSize(16);
                        c.Item().Text($"Клиника «DentalLocal»");
                    });
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Визит № {visit.Id.ToString()[..8].ToUpper()}").Bold();
                        c.Item().Text($"Дата: {visit.VisitDate:dd.MM.yyyy} {visit.VisitTime:HH:mm}");
                    });
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Пациент: ").Bold();
                            t.Span(visit.Patient?.FullName ?? "Не указан");
                        });
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Врач: ").Bold();
                            t.Span(visit.DoctorName);
                        });
                    });

                    column.Item().PaddingTop(10).Text("ОКАЗАННЫЕ УСЛУГИ / ЛЕЧЕНИЕ:").Bold();

                    // Table
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Procedure
                            columns.RelativeColumn(1); // Tooth
                            columns.RelativeColumn(1.5f); // Price
                            columns.RelativeColumn(1); // Quantity
                            columns.RelativeColumn(1.5f); // Total
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Наименование услуги").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Зуб").Bold().AlignCenter();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Цена (сум)").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Кол-во").Bold().AlignCenter();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Сумма (сум)").Bold().AlignRight();
                        });

                        // Rows
                        foreach (var item in visit.Items)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Procedure?.Name ?? "Услуга");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ToothNumber?.ToString() ?? "—").AlignCenter();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Price.ToString("N0")).AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Quantity.ToString()).AlignCenter();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text((item.Price * item.Quantity).ToString("N0")).AlignRight();
                        }
                    });

                    // Summary
                    column.Item().AlignRight().Width(250).PaddingTop(10).Column(sumCol =>
                    {
                        sumCol.Spacing(5);
                        sumCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Общая стоимость:");
                            r.RelativeItem().AlignRight().Text($"{visit.TotalCost.ToString("N0")} сум").Bold();
                        });
                        sumCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Оплачено:");
                            r.RelativeItem().AlignRight().Text($"{visit.TotalPaid.ToString("N0")} сум").Bold();
                        });
                        sumCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Остаток долга:");
                            r.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span($"{visit.RemainingAmount.ToString("N0")} сум").Bold().FontColor(visit.RemainingAmount > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                            });
                        });
                    });

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Text("Кассир (подпись): ___________________");
                        row.RelativeItem().AlignRight().Text("Пациент (подпись): ___________________");
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
