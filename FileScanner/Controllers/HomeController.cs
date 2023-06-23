using FileScanner.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MimeTypes.Core;
using System.Diagnostics;
using System.Web;

namespace FileScanner.Controllers
{
    public class HomeController : Controller
    {
        private static List<FileItem>? items;
        public HomeController()
        {
           
        }

        public async Task<ActionResult<IEnumerable<FileItem>>> Index()
        {
            List<string> files = new List<string>();
            string currentDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo? parentDirectory = Directory.GetParent(currentDirectory)!.Parent;
            try
            {
                files = await ScanDirectory(parentDirectory!.FullName);
                items = await GetInformation(files);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Ошибка доступа: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сканировании: {ex.Message}");
            }
            var groupedItems = items!.GroupBy(x => x.MimeType)
                        .ToDictionary(g => g.Key, g => g.ToList());

            var totalCount = items!.Count;

            var mimeTypeStatistics = groupedItems
                .Select(group => new
                {
                    MimeType = group.Key,
                    Count = group.Value.Count,
                    Percentage = Math.Round((double)group.Value.Count / totalCount * 100, 2),
                    AverageSize = Math.Round(group.Value.Average(item => item.Size), 2)
                })
                .ToList();


            ViewBag.MimeTypeStatistics = mimeTypeStatistics;

            return View(groupedItems);
        }


        

        private async Task<List<FileItem>> GetInformation(List<string> paths)
        {
            List<FileItem> fileItems = new List<FileItem>();
            foreach (string path in paths)
            {

                if (Directory.Exists(path))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    string directoryName = directoryInfo.Name;
                    long directorySize = await CalculateDirectorySize(directoryInfo);
                    string mimeType = "directory";

                    fileItems.Add(new FileItem
                    {
                        Name = directoryName, 
                        Size = directorySize, 
                        MimeType = mimeType 
                    });
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(path);
                    string fileName = fileInfo.Name;
                    long fileSize = fileInfo.Length;
                    string mimeType = GetMimeTypeFromExtension(fileInfo.Extension);

                    fileItems.Add(new FileItem{
                        Name = fileName, 
                        Size = fileSize, 
                        MimeType = mimeType
                    });
                }
            }
            return fileItems;
        }




        private static async Task<long> CalculateDirectorySize(DirectoryInfo directoryInfo)
        {
            long size = 0;

            FileInfo[] files = directoryInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                size += file.Length;
            }

            DirectoryInfo[] subdirectories = directoryInfo.GetDirectories();
            foreach (DirectoryInfo subdirectory in subdirectories)
            {
                size += await CalculateDirectorySize(subdirectory);
            }

            return size;
        }


        private static string GetMimeTypeFromExtension(string fileExtension)
        {
            string mimeType = MimeTypeMap.GetMimeType(fileExtension);

            if (string.IsNullOrEmpty(mimeType))
            {
                mimeType = "application/octet-stream";
            }

            return mimeType;
        }

        private async Task<List<string>> ScanDirectory(string rootDirectory)
        {
            List<string> scannedItems  = new List<string>();

            try
            {
                DirectoryInfo directory = new DirectoryInfo(rootDirectory);

                scannedItems.Add(directory.FullName);

                foreach(var subDirectory in directory.GetDirectories())
                {
                    List<string> subdirectoryItems = await ScanDirectory(subDirectory.FullName);
                    scannedItems.AddRange(subdirectoryItems);
                }

                foreach (var file in directory.GetFiles())
                {
                    scannedItems.Add(file.FullName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сканировании директории: {ex.Message}");
            }
            return scannedItems;
        }





        public IActionResult DownloadPage()
        {
            var groupedItems = items!.GroupBy(x => x.MimeType)
                        .ToDictionary(g => g.Key, g => g.ToList());

            var totalCount = items!.Count;

            var mimeTypeStatistics = groupedItems
                .Select(group => new
                {
                    MimeType = group.Key,
                    Count = group.Value.Count,
                    Percentage = Math.Round((double)group.Value.Count / totalCount * 100, 2),
                    AverageSize = Math.Round(group.Value.Average(item => item.Size), 2)
                })
                .ToList();

            string content = @"

                <!DOCTYPE html>
                <html>
                <head>
                    <meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
                    <link rel='stylesheet' type='text/css' href='~/css/style.css' />
                    <title>Тестовое задание</title>
                </head>
                <body>
                    <h3>Статистика по MimeType</h3>
                    <table>
                        <tr>
                            <th>MimeType</th>
                            <th>Количество</th>
                            <th>Процентное соотношение</th>
                            <th>Средний размер (в байтах)</th>
                        </tr>";
            foreach (var stat in mimeTypeStatistics)
            {
                content += $@"<tr><td>{stat.MimeType}</td><td>{@stat.Count}</td> <td>{stat.Percentage} %</td> <td>{@stat.AverageSize}</td> </tr>";
            }
            content += @"
                    </table>
                    <div class='files'>
                        <button id='toggleButton'>Показать все файлы</button>
                    </div>
                    <div id='infoList' style='display: none;'>";
            var groups = items.GroupBy(x => x.MimeType)
                        .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var group in groupedItems)
            {
                content += $"<h3 class='mime'>{group.Key}</h3>";
                foreach (var item in group.Value)
                {
                    content += $@"<div class='item'>
                                    <p>Название: {item.Name}</p>
                                    <p>MimeType: {item.MimeType}</p>
                                    <p>Размер: {@item.Size} байт</p>
                                </div>
                    ";
                }
            }
            content += @"
                    </div>
                    

                    <script>
                        document.addEventListener('DOMContentLoaded', function () {
                            var toggleButton = document.getElementById('toggleButton');
                            var infoList = document.getElementById('infoList');

                            toggleButton.addEventListener('click', function () {
                                if (infoList.style.display === 'none') {
                                    infoList.style.display = 'block';
                                } else {
                                    infoList.style.display = 'none';
                                }
                            });
                        });
                    </script>

                   <style>
                    #toggleButton {
                    padding: 10px 20px;
                    background-color: #337ab7;
                    color: #fff;
                    border: none;
                    border-radius: 4px;
                    cursor: pointer;
                    text-decoration:none;
                    padding-top:2vh;
                    }

                    #infoList {
                        margin-top: 10px;
                    }

                        #infoList ul {
                            list-style: none;
                            padding: 0;
                        }

                        #infoList li {
                            margin-bottom: 5px;
                        }

                    .item {
                        margin-bottom: 10px;
                        border: 1px solid #ccc;
                        padding: 10px;
                        background-color: #f5f5f5;
                    }

                    .mime {
                        font-size: 18px;
                        font-weight: bold;
                        color: #333;
                        margin-bottom: 10px;
                    }

                    table {
                        border-collapse: collapse;
                        width: 100%;
                    }

                    th, td {
                        padding: 8px;
                        text-align: left;
                        border-bottom: 1px solid #ddd;
                    }

                    th {
                        background-color: #f2f2f2;
                    }

                    .files
                    {
                        padding-top:3vh
                    }
                   </style>
                </body>
                </html>
                ";

            string fileName = "filescanner_page.html";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            // Сохраняем HTML страницу в файл
            System.IO.File.WriteAllText(filePath, content);

            // Возвращаем файл для скачивания
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "text/html", fileName);
        }
    }
}