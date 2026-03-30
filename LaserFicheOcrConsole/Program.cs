
using Laserfiche.DocumentServices;
using Laserfiche.RepositoryAccess;
using Laserfiche.RepositoryAccess.Common;
using LaserFicheOcrConsole.models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
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
            JsonResponse jsonResponse = new JsonResponse();

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

            if (!Directory.Exists(@"C:\logs"))
            {

                Directory.CreateDirectory(@"C:\\logs");

            }
            if (!Directory.Exists(@"C:\temp"))
            {

                Directory.CreateDirectory(@"C:\\temp");

            }
            if (!Directory.Exists(@"C:\extractedText"))
            {

                Directory.CreateDirectory(@"C:\\extractedText");
            }

            try
            {
                //if (args.Length == 0)
                //{
                //    Console.WriteLine("FAIL: No entryId provided");
                //    return;
                //}

                //int entryId = int.Parse(args[0]);

                int entryId = 0;

                Console.WriteLine("Please enter the entry ID: ");
                string input = Console.ReadLine();
                if (!int.TryParse(input, out entryId))
                {
                    Console.WriteLine("FAIL: Invalid entry ID");
                    Console.WriteLine("\n\nPress enter to exit console...");

                    Console.ReadLine();
                    return;
                }

                RepositoryRegistration repository = new RepositoryRegistration(serverName, repoName);

                using (Session session = new Session())
                {
                    session.LogIn(username, password, repository);

                    DocumentExporter documentExporter = new DocumentExporter();

                    EntryInfo entry = Entry.GetEntryInfo(entryId, session);

                    if (entry == null)
                    {
                        jsonResponse.data = "FAIL: Entry not found";
                        jsonResponse.success = false;
                        Console.WriteLine("Success: " + jsonResponse.success);
                        Console.WriteLine(jsonResponse.data);
                        Console.WriteLine("\n\nPress enter to exit console...");

                        Console.ReadLine();

                        return;
                    }

                    if (entry.EntryType != EntryType.Document)
                    {
                        jsonResponse.data = "FAIL: Entry is not a document";
                        jsonResponse.success = false;
                        Console.WriteLine("Success: " + jsonResponse.success);
                        Console.WriteLine(jsonResponse.data);
                        Console.WriteLine("\n\nPress enter to exit console...");

                        Console.ReadLine();

                        return;
                    }


                    DocumentInfo doc = new DocumentInfo(entryId, session);

                    


                    IDocumentContents documentContents = doc as IDocumentContents;

                    if (documentContents == null)
                    {
                        jsonResponse.data = "FAIL: Document has no content";
                        jsonResponse.success = false;
                        Console.WriteLine("Success: " + jsonResponse.success);
                        Console.WriteLine(jsonResponse.data);
                        Console.WriteLine("\n\nPress enter to exit console...");

                        Console.ReadLine();

                        return;
                    }

                    PageSet pageSet = new PageSet();

                    //if (doc.PageCount == 0)
                    //{
                    //    jsonResponse.data = "FAIL: Document has no pages";
                    //    jsonResponse.success = false;
                    //    Console.WriteLine("Success: " + jsonResponse.success);
                    //    Console.WriteLine(jsonResponse.data);

                    //    return;
                    //}

                    for (int i = 1; i <= doc.PageCount; i++)
                    {
                        pageSet.AddPage(i);
                    }

                    string ElecDocFilePath = $@"{filepath}\{Guid.NewGuid()}.pdf";

                    string ImageFilePath = $@"{filepath}\{Guid.NewGuid()}.pdf";

                    Console.WriteLine("Fetching Data...");

                    documentExporter.ExportPdf(documentContents, pageSet, PdfExportOptions.None, ImageFilePath);

                    documentExporter.ExportElecDoc(documentContents, ElecDocFilePath);

                    string mergedPDF = $@"{filepath}\merged.pdf";


                    MergePDFs(mergedPDF, new string[] { ElecDocFilePath, ImageFilePath });

                    Console.WriteLine("Ocring Data...");


                    ApiResponse extractedText = await client.ExtractText(mergedPDF);



                    WriteToDocumentTxt(entryId, session, extractedText.markdown_content);


                    string extractedTextFilePath = $@"C:\extractedText\{Guid.NewGuid()}.txt";

                    File.WriteAllText(extractedTextFilePath, extractedText.markdown_content);



                    //File.Delete(filePath); 



                    jsonResponse.data = extractedText.markdown_content;
                    jsonResponse.success = true;

                    Console.WriteLine("\n\nOK!");

                    Console.ForegroundColor = ConsoleColor.Green;


                    Console.WriteLine("Success: " + jsonResponse.success);

                    Console.WriteLine("data: " + jsonResponse.data);

                    Console.ForegroundColor= ConsoleColor.White;
                    Console.WriteLine("\n \na txt file has been generated in the following path:");
                    Console.WriteLine(extractedTextFilePath);

                    Console.WriteLine("\n\nPress enter to exit console...");
                    Console.ReadLine();

                }


            }
            catch (Exception ex)
            {

        

                string filePath = $@"C:\logs\{Guid.NewGuid()}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

                File.WriteAllText(filePath, ex.ToString());

                jsonResponse.success = false;
                Console.WriteLine("FAIL: " + ex.Message);

                Console.WriteLine("\n\nPress enter to exit console...");


                Console.ReadLine();
            }

        }

        static void MergePDFs(string targetPath, params string[] sourceFiles)
        {
            // Create the output document
            using (PdfDocument outputDocument = new PdfDocument())
            {
                foreach (string file in sourceFiles)
                {
                    // Open the source document in Import mode
                    using (PdfDocument inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import))
                    {
                        // Iterate through pages and add to output
                        int count = inputDocument.PageCount;
                        for (int idx = 0; idx < count; idx++)
                        {
                            PdfPage page = inputDocument.Pages[idx];
                            outputDocument.AddPage(page);
                        }
                    }
                }
                // Save the finished document
                outputDocument.Save(targetPath);
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