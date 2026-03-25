using Laserfiche.DocumentServices;
using Laserfiche.RepositoryAccess;
using Laserfiche.RepositoryAccess.Common;
using LaserFicheOcrConsole.models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaserFicheOcrConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string serverName = config["Laserfiche:ServerName"];
            string repoName = config["Laserfiche:RepoName"];
            string username = config["Laserfiche:Username"];
            string password = config["Laserfiche:Password"];
            string filepath = config["Laserfiche:exportedFilePath"]?.Trim();
            OcrClient client = new OcrClient();

            try
            {
                //if (args.Length == 0)
                //{
                //    Console.WriteLine("FAIL: No entryId provided");
                //    return;
                //}

                //int entryId = int.Parse(args[0]);

                int entryId = 81;

                RepositoryRegistration repository = new RepositoryRegistration(serverName, repoName);

                using (Session session = new Session())
                {
                    session.LogIn(username, password, repository);

                    DocumentExporter documentExporter = new DocumentExporter();

                    DocumentInfo doc = new DocumentInfo(entryId, session);


                    IDocumentContents documentContents = doc as IDocumentContents;

                    PageSet pageSet = new PageSet();

                    for (int i = 1; i <= doc.PageCount; i++)
                    {
                        pageSet.AddPage(i);
                    }

                    string filePath = $@"{filepath}\{Guid.NewGuid()}.pdf";

                    documentExporter.ExportPdf(documentContents, pageSet, PdfExportOptions.None, filePath);

                    ApiResponse extractedText = await client.ExtractText(filePath);

                    WriteToDocumentTxt(entryId, session, extractedText.markdown_content);


                    File.WriteAllText($@"C:\extractedText\{Guid.NewGuid()}.txt", extractedText.markdown_content);

                    File.Delete(filePath);

                    var result = new
                    {
                        success = true,
                        data = extractedText.markdown_content
                    };

                    Console.WriteLine(JsonSerializer.Serialize(result));
                }


            }
            catch (Exception ex)
            {
                string filePath = $@"C:\logs\{Guid.NewGuid()}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

                File.WriteAllText(filePath, ex.ToString());

                Console.WriteLine("FAIL: " + ex.Message);
            }

        }



        private static void WriteToDocumentTxt(int entryId, Session session, string extractedOCRText)
        {
            using (EntryInfo entry = Entry.GetEntryInfo(entryId, session))
            {
                if (!(entry is DocumentInfo doc))
                    throw new InvalidOperationException($"Entry {entryId} is not a document.");

                doc.Lock(LockType.Exclusive);
                try
                {
                    doc.Refresh(true);

                    if (doc.PageCount < 1)
                        throw new InvalidOperationException("Document has no Laserfiche pages.");

                    PageInfo p1 = doc.GetPageInfo(1);
                    p1.WriteTextPagePart(extractedOCRText);

                    doc.Save();
                }
                finally
                {
                    doc.Unlock();
                }
            }
        }
    }
}
