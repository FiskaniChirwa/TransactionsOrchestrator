using System.Text.Json;
using Document.Models.TemplateData;
using Document.Models.Enums;
using Document.Core.Services;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Document.Infrastructure.Pdf.Templates;

public class TransactionStatementTemplate : IPdfTemplate
{
    private readonly ILogger<TransactionStatementTemplate> _logger;

    public DocumentType SupportedType => DocumentType.TransactionStatement;

    public TransactionStatementTemplate(ILogger<TransactionStatementTemplate> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<Stream> RenderAsync(Dictionary<string, object> data)
    {
        _logger.LogInformation("Rendering Transaction Statement PDF");

        // Convert dictionary to strongly-typed model
        var json = JsonSerializer.Serialize(data);
        var model = JsonSerializer.Deserialize<TransactionStatementData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize transaction statement data");

        return await Task.Run(() => GeneratePdf(model));
    }

    public List<string> GetRequiredFields()
    {
        return new List<string>
        {
            "customerId",
            "customerName",
            "accounts"
        };
    }

    public List<string> GetOptionalFields()
    {
        return new List<string>
        {
            "totalTransactions",
            "dateRange"
        };
    }

    public string GetDescription()
    {
        return "Generates a comprehensive transaction statement with account details and transaction history";
    }

    private Stream GeneratePdf(TransactionStatementData data)
    {
        var stream = new MemoryStream();

        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(ComposeFooter);
            });
        })
        .GeneratePdf(stream);

        stream.Position = 0;

        _logger.LogInformation("Transaction Statement PDF generated successfully");

        return stream;
    }

    private void ComposeHeader(IContainer container, TransactionStatementData data)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(1).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("TRANSACTION STATEMENT")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    col.Item().Text($"Customer: {data.CustomerName} (#{data.CustomerId})")
                        .FontSize(12)
                        .SemiBold();

                    col.Item().Text($"Period: {data.DateRange?.ToString() ?? "All time"}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Generated: ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
                });
            });

            column.Item().PaddingTop(5);
        });
    }

    private void ComposeContent(IContainer container, TransactionStatementData data)
    {
        container.Column(column =>
        {
            if (!data.Accounts.Any())
            {
                column.Item().Text("No accounts or transactions found for this period.")
                    .FontColor(Colors.Grey.Darken1)
                    .Italic();
                return;
            }

            foreach (var account in data.Accounts)
            {
                column.Item().Element(c => ComposeAccount(c, account));
                column.Item().PaddingBottom(15);
            }

            column.Item().Element(c => ComposeSummary(c, data));
        });
    }

    private void ComposeAccount(IContainer container, AccountData account)
    {
        container.Column(column =>
        {
            column.Item().Background(Colors.Blue.Lighten4).Padding(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Account: {account.AccountType}")
                        .FontSize(12)
                        .SemiBold();

                    col.Item().Text($"Account Number: {account.AccountNumber}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text($"Balance: {account.Currency} {account.CurrentBalance:N2}")
                        .FontSize(10)
                        .SemiBold();

                    col.Item().Text($"Available: {account.Currency} {account.AvailableBalance:N2}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);
                });
            });

            if (!account.Transactions.Any())
            {
                column.Item().Padding(10).Text("No transactions")
                    .FontColor(Colors.Grey.Darken1)
                    .Italic();
            }
            else
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(60);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Date").SemiBold();
                        header.Cell().Element(CellStyle).Text("Description").SemiBold();
                        header.Cell().Element(CellStyle).Text("Merchant").SemiBold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Amount").SemiBold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Balance").SemiBold();
                        header.Cell().Element(CellStyle).Text("Status").SemiBold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container
                                .Background(Colors.Grey.Lighten2)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Darken1)
                                .Padding(5);
                        }
                    });

                    foreach (var transaction in account.Transactions.OrderBy(t => t.Date))
                    {
                        table.Cell().Element(RowCellStyle).Text(transaction.Date.ToString("yyyy-MM-dd"));
                        table.Cell().Element(RowCellStyle).Text(transaction.Description);
                        table.Cell().Element(RowCellStyle).Text(transaction.MerchantName);

                        table.Cell().Element(RowCellStyle).AlignRight().Text(text =>
                        {
                            var color = transaction.Amount >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2;
                            var prefix = transaction.Amount >= 0 ? "+" : "";
                            text.Span($"{prefix}{transaction.Currency} {transaction.Amount:N2}")
                                .FontColor(color)
                                .SemiBold();
                        });

                        table.Cell().Element(RowCellStyle).AlignRight()
                            .Text($"{transaction.Currency} {transaction.BalanceAfter:N2}");

                        table.Cell().Element(RowCellStyle).Text(transaction.Status);

                        static IContainer RowCellStyle(IContainer container)
                        {
                            return container
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    }
                });

                column.Item().PaddingTop(5).AlignRight()
                    .Text($"Total: {account.Transactions.Count} transaction(s)")
                    .FontSize(9)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private void ComposeSummary(IContainer container, TransactionStatementData data)
    {
        var allTransactions = data.Accounts.SelectMany(a => a.Transactions).ToList();

        if (!allTransactions.Any())
            return;

        var totalDebits = allTransactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
        var totalCredits = allTransactions.Where(t => t.Amount >= 0).Sum(t => t.Amount);
        var netAmount = totalCredits - totalDebits;
        var currency = allTransactions.FirstOrDefault()?.Currency ?? "ZAR";

        container.Background(Colors.Blue.Lighten5).Padding(10).Column(column =>
        {
            column.Item().Text("SUMMARY")
                .FontSize(14)
                .SemiBold()
                .FontColor(Colors.Blue.Darken2);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Total Transactions: {data.TotalTransactions}");
                row.RelativeItem().Text($"Total Debits: {currency} {totalDebits:N2}").FontColor(Colors.Red.Darken2);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Accounts: {data.Accounts.Count}");
                row.RelativeItem().Text($"Total Credits: {currency} {totalCredits:N2}").FontColor(Colors.Green.Darken2);
            });

            column.Item().PaddingTop(5).AlignRight()
                .Text($"Net Amount: {currency} {netAmount:N2}")
                .FontSize(12)
                .SemiBold()
                .FontColor(netAmount >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ").FontSize(8);
            text.CurrentPageNumber().FontSize(8);
            text.Span(" of ").FontSize(8);
            text.TotalPages().FontSize(8);
        });
    }
}