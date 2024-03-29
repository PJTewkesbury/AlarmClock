﻿using AlarmClock;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlarmClockWeb.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public DateTime AlarmTime { get; set; }

        [BindProperty]
        public int AlarmDuration{ get; set; }

        [BindProperty]
        public int SnoozeDuration { get; set; }

        [BindProperty]
        public string RadioStationUrl { get; set; }

        [BindProperty]
        public string WifiSSID { get; set; }

        [BindProperty]
        public string WifiPassword{ get; set; }

        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            AlarmTime = DateTime.Now.Date.AddHours(AlarmClock.AlarmClock.alarmClockState.Hour).AddMinutes(AlarmClock.AlarmClock.alarmClockState.Minute);
            AlarmDuration = AlarmClock.AlarmClock.alarmClockState.DurationTime;
            SnoozeDuration = AlarmClock.AlarmClock.alarmClockState.SnoozeTime;
            RadioStationUrl = "";
            WifiSSID = "BlueSKY87654";
            WifiPassword = "MDCQXSXH.5";
        }

        public void OnPost()
        {
            // Save Changes to file.
            AlarmClock.AlarmClock.alarmClockState.Hour = AlarmTime.Hour;
            AlarmClock.AlarmClock.alarmClockState.Minute = AlarmTime.Minute;
            AlarmClock.AlarmClock.alarmClockState.DurationTime = AlarmDuration;
            AlarmClock.AlarmClock.alarmClockState.SnoozeTime = SnoozeDuration;
            AlarmClock.AlarmClock.alarmClockState.Save();
        }
    }
}
