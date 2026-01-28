using iTextSharp.text;
using iTextSharp.text.pdf;

namespace BlazorDematReports.Services.UIServices
{
    /// <summary>
    /// Servizio per la generazione di documenti PDF
    /// </summary>
    public interface IPdfExportService
    {
        /// <summary>
        /// Genera un documento PDF dal contenuto testuale fornito.
        /// </summary>
        /// <param name="content">Contenuto testuale da convertire in PDF.</param>
        /// <param name="title">Titolo del documento PDF (predefinito: "Document").</param>
        /// <returns>Array di byte contenente il documento PDF generato.</returns>
        Task<byte[]> GeneratePdfFromTextAsync(string content, string title = "Document");
    }

    /// <summary>
    /// Implementazione del servizio per l'esportazione PDF
    /// </summary>
    public class PdfExportService : IPdfExportService
    {
        /// <inheritdoc />
        public async Task<byte[]> GeneratePdfFromTextAsync(string content, string title = "Document")
        {
            return await Task.Run(() =>
            {
                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Aggiungi titolo
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var titleParagraph = new Paragraph(title, titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(titleParagraph);

                // Aggiungi contenuto
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var lines = content.Split('\n');

                foreach (var line in lines)
                {
                    var paragraph = new Paragraph(line, bodyFont)
                    {
                        SpacingAfter = 5
                    };

                    // Formattazione speciale per titoli sezioni
                    if (line.Trim().StartsWith("===") || line.Trim().All(c => c == '='))
                    {
                        paragraph.Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                        paragraph.SpacingBefore = 10;
                        paragraph.SpacingAfter = 10;
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^\d+\.\s+[A-Z]"))
                    {
                        paragraph.Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                        paragraph.SpacingBefore = 8;
                        paragraph.SpacingAfter = 8;
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^\d+\.\d+\s+"))
                    {
                        paragraph.Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                        paragraph.SpacingBefore = 6;
                    }

                    document.Add(paragraph);
                }

                document.Close();
                return stream.ToArray();
            });
        }
    }
}