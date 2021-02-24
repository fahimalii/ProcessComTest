using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProcessComTest.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessComTest.Controllers
{
    [Serializable]  // mandatory
    class Message
    {
        public string Color;
    }

    public class HomeController : Controller
    {
        private const string TestFileName = "info.txt";
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _filePath;
        private readonly IMemoryCacheTest _memoryCacheTest;

        public HomeController(ILogger<HomeController> logger, IMemoryCache memoryCache, IHostEnvironment hostEnvironment, IMemoryCacheTest memoryCacheTest)
        {
            _logger = logger;
            _cache = memoryCache;
            _filePath =  Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "test", TestFileName);
            _memoryCacheTest = memoryCacheTest;
        }

        public IActionResult Index()
        {
            _cache.TryGetValue(CacheKeys.Entry, out string data);
            var isDataFromCache = true;

            
            if (string.IsNullOrWhiteSpace(data))
            {
                Thread.Sleep(2000);
                data = System.IO.File.ReadAllText(_filePath);
                isDataFromCache = false;
                _memoryCacheTest.SetItemInCacheTest(_filePath);
            }

            ViewBag.Data = data;
            ViewBag.IsDataFromCache = isDataFromCache;

            ViewBag.ProcessID = Process.GetCurrentProcess().Id;
            ViewBag.ProcessList = Process.GetProcessesByName("w3wp").ToList();
            ViewBag.ProcessListCount = ViewBag.ProcessList.Count;
            ViewBag.ProcessName = Process.GetCurrentProcess().ProcessName;

            return View();
        }

        [HttpPost]
        public IActionResult Index(string colorName)
        {
            FileInfo file = new FileInfo(_filePath);
            System.IO.File.WriteAllText(_filePath, string.Empty);
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(colorName);
                    writer.Flush();
                }
            }

            return RedirectToAction("Index");
        }
        

        public IActionResult Index2()
        {
            ViewBag.BackgroundColor = System.IO.File.ReadAllText(_filePath);
            ViewBag.ProcessID = Process.GetCurrentProcess().Id;
            //ViewBag.ProcessList = Process.GetProcessesByName("iisexpress").ToList();
            ViewBag.ProcessList = Process.GetProcessesByName("w3wp").ToList();
            ViewBag.ProcessListCount = ViewBag.ProcessList.Count;
            ViewBag.ProcessName = Process.GetCurrentProcess().ProcessName;

            var memoryColor = ReadColorFromMemory();

            if(!string.IsNullOrWhiteSpace(memoryColor))
            {
                ViewBag.BackgroundColor = memoryColor;
            }

            return View();
        }

        [HttpPost]
        public IActionResult Index2(string colorName)
        {
            const int MMF_MAX_SIZE = 1024;  // allocated memory for this memory mapped file (bytes)
            const int MMF_VIEW_SIZE = 1024; // how many bytes of the allocated memory can this process access

            // creates the memory mapped file which allows 'Reading' and 'Writing'
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", MMF_MAX_SIZE, MemoryMappedFileAccess.ReadWrite);

            // creates a stream for this process, which allows it to write data from offset 0 to 1024 (whole memory)
            MemoryMappedViewStream mmvStream = mmf.CreateViewStream(0, MMF_VIEW_SIZE);

            // this is what we want to write to the memory mapped file
            Message message = new Message()
            {
                Color = colorName
            };

            // Serialize the variable message and write it to the memory mapped file
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(mmvStream, message);

            // sets the current position back to the beginning of the stream
            mmvStream.Seek(0, System.IO.SeekOrigin.Begin);

            return RedirectToAction("Index");
        }

        private string ReadColorFromMemory()
        {
            const int MMF_MAX_SIZE = 1024;  // allocated memory for this memory mapped file (bytes)
            const int MMF_VIEW_SIZE = 1024; // how many bytes of the allocated memory can this process access

            // creates the memory mapped file
            try
            {
                MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("mmf1");

                MemoryMappedViewStream mmvStream = mmf.CreateViewStream(0, MMF_VIEW_SIZE); // stream used to read data

                BinaryFormatter formatter = new BinaryFormatter();

                // needed for deserialization
                byte[] buffer = new byte[MMF_VIEW_SIZE];

                Message message1;

                // reads every second what's in the shared memory
                while (mmvStream.CanRead)
                {
                    // stores everything into this buffer
                    mmvStream.Read(buffer, 0, MMF_VIEW_SIZE);

                    // deserializes the buffer & prints the message
                    message1 = (Message)formatter.Deserialize(new MemoryStream(buffer));

                    return message1?.Color;
                }
            }
            catch (FileNotFoundException)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 20, Location = ResponseCacheLocation.Any, NoStore = false)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
