﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = Globals.ROLE_ADMIN)]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly CTDbContext _context;
        private readonly WakeDownloader _wakeDownloader;

        public AdminController(CTDbContext context, WakeDownloader wakeDownloader)
        {
            _context = context;
            _wakeDownloader = wakeDownloader;
        }

        [HttpPost("UpdateAllPlaylists")]
        public ActionResult UpdateAllPlaylists()
        {
            _wakeDownloader.UpdateAllPlaylists();
            return Ok();
        }

        [HttpPost("UpdatePlaylist")]
        public ActionResult UpdatePlaylist(string playlistId)
        {
            _wakeDownloader.UpdatePlaylist(playlistId);
            return Ok();
        }

        [HttpPost("PeriodicCheck")]
        public ActionResult PeriodicCheck()
        {
            _wakeDownloader.PeriodicCheck();
            return Ok();
        }

        [HttpGet("GetLogs")]
        public async Task<IActionResult> GetLogs(DateTime from, DateTime to)
        {
            var logs = await _context.Logs.Where(l => l.CreatedAt >= from && l.CreatedAt <= to).Select(l => new {
                Id = l.Id,
                CreatedAt = l.CreatedAt,
                UserId = l.UserId,
                OfferingId = l.OfferingId,
                MediaId = l.MediaId,
                EventType = l.EventType,
                Json = l.Json
            }).ToListAsync();
            var path = Path.GetTempFileName();
            using (var writer = new StreamWriter(path))
            {
                using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.CurrentCulture))
                {
                    csv.WriteRecords(logs);
                }
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "text/csv", Path.GetFileNameWithoutExtension(path) + ".csv");
        }
    }
}