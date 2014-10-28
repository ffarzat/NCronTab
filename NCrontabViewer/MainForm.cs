﻿#region License, Terms and Author(s)
//
// NCrontab - Crontab for .NET
// Copyright (c) 2008 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace NCrontabViewer
{
    #region Imports

    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using NCrontab;

    #endregion

    public partial class MainForm : Form
    {
        private DateTime _lastChangeTime;
        private bool _dirty;
        private CrontabSchedule _crontab;
        private DateTime _startTime;
        private int _totalOccurrenceCount;

        public MainForm()
        {
            InitializeComponent();
        }

        private void CronBox_Changed(object sender, EventArgs args)
        {
            _lastChangeTime = DateTime.Now;
            _dirty = true;
            _crontab = null;
        }

        private void Timer_Tick(object sender, EventArgs args)
        {
            var changeLapse = DateTime.Now - _lastChangeTime;

            if (!_dirty || changeLapse <= TimeSpan.FromMilliseconds(500)) 
                return;

            _dirty = false;
            DoCrontabbing();
        }

        private void DoCrontabbing()
        {
            _resultBox.Clear();
            _errorProvider.SetError(_cronBox, null);
            _statusBarPanel.Text = "Ready";
            _moreButton.Enabled = false;

            if (_crontab == null)
            {
                try
                {
                    var expression = _cronBox.Text.Trim();

                    if (expression.Length == 0)
                        return;

                    _crontab = CrontabSchedule.Parse(expression);
                   
                    _totalOccurrenceCount = 0;
                    
                    _startTime = DateTime.ParseExact(_startTimePicker.Text, 
                        _startTimePicker.CustomFormat, CultureInfo.InvariantCulture, 
                        DateTimeStyles.AssumeLocal).AddMinutes(-1);
                }
                catch (CrontabException e)
                {
                    _errorProvider.SetError(_cronBox, e.Message);

                    var traceBuilder = new StringBuilder();

                    Exception traceException = e;
                    Exception lastException;

                    do
                    {
                        traceBuilder.Append(traceException.Message);
                        traceBuilder.Append("\r\n");
                        lastException = traceException;
                        traceException = traceException.GetBaseException();
                    }
                    while (lastException != traceException);

                    _resultBox.Text = traceBuilder.ToString();
                    return;
                }

            }

            var endTime = DateTime.ParseExact(_endTimePicker.Text, 
                _endTimePicker.CustomFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal);

            var sb = new StringBuilder();

            var count = 0;
            const int maxCount = 500;
            var info = DateTimeFormatInfo.CurrentInfo;
            var dayWidth = info.AbbreviatedDayNames.Max(s => s.Length);
            var monthWidth = info.AbbreviatedMonthNames.Max(s => s.Length);
            var timeFormat = string.Format("{{0,-{0}:ddd}} {{0:dd}}, {{0,-{1}:MMM}} {{0:yyyy HH:mm}}", dayWidth, monthWidth);
            var lastTimeString = new string('?', string.Format(timeFormat, DateTime.MinValue).Length);

            foreach (var occurrence in _crontab.GetNextOccurrences(_startTime, endTime))
            {
                if (count + 1 > maxCount)
                    break;

                _startTime = occurrence;
                _totalOccurrenceCount++;
                count++;

                var timeString = string.Format(timeFormat, occurrence);

                sb.Append(timeString);
                sb.Append(" | ");

                var index = Diff(lastTimeString, timeString, 0, dayWidth, sb);
                sb.Append(' ');
                index = Diff(lastTimeString, timeString, index + 1, 2, sb);
                sb.Append(", ");
                index = Diff(lastTimeString, timeString, index + 2, monthWidth, sb);
                sb.Append(' ');
                index = Diff(lastTimeString, timeString, index + 1, 4, sb);
                sb.Append(' ');
                index = Diff(lastTimeString, timeString, index + 1, 2, sb);
                sb.Append(':');
                Diff(lastTimeString, timeString, index + 1, 2, sb);

                lastTimeString = timeString;

                sb.Append("\r\n");
            }

            _moreButton.Enabled = count == maxCount;

            _statusBarPanel.Text = string.Format("Last count = {0}, Total = {1}",
                count.ToString("N0"), _totalOccurrenceCount.ToString("N0"));

            _resultBox.Text = sb.ToString();
            _resultBox.Select(0, 0);
            _resultBox.ScrollToCaret();
        }

        private static int Diff(string oldString, string newString, int index, int length, StringBuilder builder)
        {
            if (string.CompareOrdinal(oldString, index, newString, index, length) == 0)
                builder.Append('-', length);
            else
                builder.Append(newString, index, length);

            return index + length;
        }

        private void More_Click(object sender, EventArgs e)
        {
            DoCrontabbing();
        }
    }
}
